# Document Service Contract

## Overview

This contract describes the service interfaces that implement the upload, authorization, access, sharing, and audit behavior for the document feature. It is intentionally aligned to the current repository’s service-oriented design and the offline-first architecture stated in the project constitution.

## Interface: IFileStorageService

```csharp
public interface IFileStorageService
{
    Task<string> UploadAsync(Stream fileStream, string originalFileName, string contentType, string logicalPath);
    Task DeleteAsync(string storedFilePath);
    Task<Stream> DownloadAsync(string storedFilePath);
    Task<string> GetUrlAsync(string storedFilePath, TimeSpan expiration);
}
```

### Contract behaviors
- `UploadAsync` stores the file in a safe location and returns the stored relative path for database persistence.
- `DeleteAsync` removes the stored file and fails clearly if it cannot be deleted.
- `DownloadAsync` returns a stream only after the caller has been authorized.
- `GetUrlAsync` is optional for future Azure migration and may resolve to a local path or virtual URL in the training environment.

## Interface: IDocumentService

```csharp
public interface IDocumentService
{
    Task<Document> UploadDocumentAsync(int userId, int? projectId, int? taskId, UploadDocumentRequest request, Stream fileStream);
    Task<List<DocumentSummary>> GetAccessibleDocumentsAsync(int userId, DocumentFilters filters);
    Task<Document?> GetDocumentByIdAsync(int documentId, int requestingUserId);
    Task<bool> UpdateMetadataAsync(int documentId, int requestingUserId, UpdateDocumentRequest request);
    Task<bool> ReplaceFileAsync(int documentId, int requestingUserId, Stream fileStream, string fileName, string contentType);
    Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId);
    Task<bool> ShareDocumentAsync(int documentId, int ownerUserId, int recipientUserId);
    Task<List<Document>> GetSharedWithUserAsync(int userId);
    Task<List<AuditEvent>> GetAuditTrailAsync(int requestingUserId, int? documentId = null);
}
```

### Authorization expectations
- Users may upload documents when authorized for their project context or when uploading personal documents.
- Project managers and document owners may delete documents within their project boundaries.
- Download, view, and share operations require explicit access checks before data is returned or mutated.
- Service methods must reject unauthorized access without leaking sensitive information.

## Data Transfer Shapes

### UploadDocumentRequest
```csharp
public class UploadDocumentRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public int? ProjectId { get; set; }
    public int? TaskId { get; set; }
}
```

### DocumentFilters
```csharp
public class DocumentFilters
{
    public string? Category { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public string? SearchText { get; set; }
    public string? SortBy { get; set; }
}
```

## Failure Semantics
- Validation failures return a clear error result and do not persist partial file storage.
- Authorization failures return a non-sensitive denial result and do not expose hidden document metadata.
- Storage errors should keep the database in a consistent state by generating the path before persistence and deleting partial files on rollback conditions.
- Audit events must record any upload, share, delete, or download action regardless of success or failure when meaningful.
