using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(int userId, int? projectId, int? taskId, UploadDocumentRequest request, Stream fileStream);
    Task<List<DocumentSummary>> GetAccessibleDocumentsAsync(int userId, DocumentFilters filters);
    Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId);
    Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId);
    Task<bool> ShareDocumentAsync(int documentId, int ownerUserId, int recipientUserId);
    Task<List<Document>> GetSharedWithUserAsync(int userId);
    Task<List<AuditEvent>> GetAuditTrailAsync(int requestingUserId, int? documentId = null);
    Task<int> GetRecentDocumentCountAsync(int userId);
}

public class DocumentService : IDocumentService
{
    private const long MaxFileSizeBytes = 25 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".doc",
        ".docx",
        ".xls",
        ".xlsx",
        ".ppt",
        ".pptx",
        ".txt",
        ".png",
        ".jpg",
        ".jpeg"
    };

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _storageService;

    public DocumentService(ApplicationDbContext context, IFileStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
    }

    public async Task<Document> UploadDocumentAsync(int userId, int? projectId, int? taskId, UploadDocumentRequest request, Stream fileStream)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Document title is required.");

        if (string.IsNullOrWhiteSpace(request.Category))
            throw new ArgumentException("Document category is required.");

        if (fileStream == null)
            throw new ArgumentException("A document file is required.");

        if (fileStream.Length == 0)
            throw new ArgumentException("The uploaded file is empty.");

        if (fileStream.Length > MaxFileSizeBytes)
            throw new InvalidOperationException("The document exceeds the 25 MB upload limit.");

        var fileName = Path.GetFileName(request.FileName);
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported file type.");

        if (projectId.HasValue)
        {
            var project = await _context.Projects
                .Include(p => p.ProjectMembers)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId.Value);

            if (project == null)
                throw new InvalidOperationException("Project not found.");

            var isProjectManager = project.ProjectManagerId == userId;
            var isProjectMember = project.ProjectMembers.Any(pm => pm.UserId == userId);
            if (!isProjectManager && !isProjectMember)
                throw new UnauthorizedAccessException("You do not have access to upload documents to this project.");
        }

        var safeName = Guid.NewGuid().ToString("N");
        var logicalPath = $"{userId}/{projectId ?? 0}/{safeName}{extension}";
        var storedPath = await _storageService.UploadAsync(fileStream, fileName, request.ContentType ?? "application/octet-stream", logicalPath);

        var document = new Document
        {
            Title = request.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Category = request.Category.Trim(),
            FileName = fileName,
            StoredFilePath = storedPath,
            MimeType = string.IsNullOrWhiteSpace(request.ContentType) ? "application/octet-stream" : request.ContentType,
            FileSizeBytes = fileStream.Length,
            UploadedByUserId = userId,
            ProjectId = projectId,
            TaskId = taskId,
            UploadedDateUtc = DateTime.UtcNow,
            UpdatedDateUtc = DateTime.UtcNow,
            Tags = string.Join(",", request.Tags ?? new List<string>())
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync();

        await LogAuditAsync(userId, "upload", document.DocumentId, $"Uploaded document '{document.Title}'");

        return document;
    }

    public async Task<List<DocumentSummary>> GetAccessibleDocumentsAsync(int userId, DocumentFilters filters)
    {
        var query = _context.Documents
            .AsNoTracking()
            .Where(d => !d.IsDeleted)
            .Where(d => d.UploadedByUserId == userId || d.ProjectId != null &&
                _context.ProjectMembers.Any(pm => pm.ProjectId == d.ProjectId && pm.UserId == userId) ||
                d.Shares.Any(s => s.UserId == userId && s.IsActive))
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.Category))
        {
            query = query.Where(d => d.Category == filters.Category);
        }

        if (filters.ProjectId.HasValue)
        {
            query = query.Where(d => d.ProjectId == filters.ProjectId.Value);
        }

        if (filters.FromUtc.HasValue)
        {
            query = query.Where(d => d.UploadedDateUtc >= filters.FromUtc.Value);
        }

        if (filters.ToUtc.HasValue)
        {
            query = query.Where(d => d.UploadedDateUtc <= filters.ToUtc.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.SearchText))
        {
            var search = filters.SearchText.Trim();
            query = query.Where(d =>
                d.Title.Contains(search) ||
                (d.Description != null && d.Description.Contains(search)) ||
                (d.Tags != null && d.Tags.Contains(search)) ||
                d.UploadedByUser.DisplayName.Contains(search) ||
                (d.Project != null && d.Project.Name.Contains(search)));
        }

        query = filters.SortBy?.ToLowerInvariant() switch
        {
            "title" => query.OrderBy(d => d.Title),
            "category" => query.OrderBy(d => d.Category),
            "filesize" => query.OrderBy(d => d.FileSizeBytes),
            _ => query.OrderByDescending(d => d.UploadedDateUtc)
        };

        var docs = await query.ToListAsync();
        return docs.Select(d => new DocumentSummary
        {
            DocumentId = d.DocumentId,
            Title = d.Title,
            Category = d.Category,
            FileSizeBytes = d.FileSizeBytes,
            UploadedDateUtc = d.UploadedDateUtc,
            ProjectId = d.ProjectId,
            ProjectName = d.Project != null ? d.Project.Name : null,
            UploaderDisplayName = d.UploadedByUser.DisplayName,
            Tags = d.Tags
        }).ToList();
    }

    public async Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Project)
                .ThenInclude(p => p.ProjectMembers)
            .Include(d => d.Shares)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null)
            return null;

        var isOwner = document.UploadedByUserId == requestingUserId;
        var isProjectManager = document.Project != null && document.Project.ProjectManagerId == requestingUserId;
        var isProjectMember = document.Project != null && document.Project.ProjectMembers.Any(pm => pm.UserId == requestingUserId);
        var isSharedRecipient = document.Shares.Any(s => s.UserId == requestingUserId && s.IsActive);

        if (!isOwner && !isProjectManager && !isProjectMember && !isSharedRecipient)
            return null;

        return document;
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId)
    {
        var document = await _context.Documents
            .Include(d => d.Project)
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null)
            return false;

        var isOwner = document.UploadedByUserId == requestingUserId;
        var isProjectManager = document.Project != null && document.Project.ProjectManagerId == requestingUserId;

        if (!isOwner && !isProjectManager)
            return false;

        document.IsDeleted = true;
        document.UpdatedDateUtc = DateTime.UtcNow;

        await _storageService.DeleteAsync(document.StoredFilePath);
        await _context.SaveChangesAsync();
        await LogAuditAsync(requestingUserId, "delete", document.DocumentId, $"Deleted document '{document.Title}'");

        return true;
    }

    public async Task<bool> ShareDocumentAsync(int documentId, int ownerUserId, int recipientUserId)
    {
        var document = await _context.Documents
            .FirstOrDefaultAsync(d => d.DocumentId == documentId && !d.IsDeleted);

        if (document == null || document.UploadedByUserId != ownerUserId)
            return false;

        var existingShare = await _context.DocumentShares
            .FirstOrDefaultAsync(ds => ds.DocumentId == documentId && ds.UserId == recipientUserId && ds.IsActive);

        if (existingShare != null)
            return false;

        var share = new DocumentShare
        {
            DocumentId = documentId,
            UserId = recipientUserId,
            GrantedByUserId = ownerUserId,
            SharedDateUtc = DateTime.UtcNow,
            IsActive = true
        };

        _context.DocumentShares.Add(share);
        await _context.SaveChangesAsync();

        await LogAuditAsync(ownerUserId, "share", documentId, $"Shared document '{document.Title}' with user {recipientUserId}");
        return true;
    }

    public async Task<List<Document>> GetSharedWithUserAsync(int userId)
    {
        return await _context.Documents
            .Include(d => d.UploadedByUser)
            .Where(d => d.Shares.Any(s => s.UserId == userId && s.IsActive) && !d.IsDeleted)
            .ToListAsync();
    }

    public async Task<List<AuditEvent>> GetAuditTrailAsync(int requestingUserId, int? documentId = null)
    {
        var isAdmin = await _context.Users.AnyAsync(u => u.UserId == requestingUserId && u.Role == UserRole.Administrator);
        if (!isAdmin)
            return new List<AuditEvent>();

        var query = _context.AuditEvents
            .Include(a => a.User)
            .AsQueryable();

        if (documentId.HasValue)
        {
            query = query.Where(a => a.DocumentId == documentId.Value);
        }

        return await query.OrderByDescending(a => a.ActionTimestampUtc).ToListAsync();
    }

    public async Task<int> GetRecentDocumentCountAsync(int userId)
    {
        var cutoff = DateTime.UtcNow.AddDays(-7);
        return await _context.Documents
            .CountAsync(d => d.UploadedByUserId == userId && d.UploadedDateUtc >= cutoff && !d.IsDeleted);
    }

    private async Task LogAuditAsync(int userId, string actionType, int documentId, string details)
    {
        _context.AuditEvents.Add(new AuditEvent
        {
            UserId = userId,
            DocumentId = documentId,
            ActionType = actionType,
            ActionTimestampUtc = DateTime.UtcNow,
            Details = details
        });

        await _context.SaveChangesAsync();
    }
}

public class UploadDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class DocumentFilters
{
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? SearchText { get; set; }
    public string? SortBy { get; set; }
}

public class DocumentSummary
{
    public int DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedDateUtc { get; set; }
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string UploaderDisplayName { get; set; } = string.Empty;
    public string? Tags { get; set; }
}
