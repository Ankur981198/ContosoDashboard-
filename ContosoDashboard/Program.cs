using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using System.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add authentication state provider for Blazor
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Mock Authentication (Cookie-based for training purposes)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/logout";
        options.AccessDeniedPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

// Add authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("TeamLead", policy => policy.RequireRole("TeamLead", "ProjectManager", "Administrator"));
    options.AddPolicy("ProjectManager", policy => policy.RequireRole("ProjectManager", "Administrator"));
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
});

// Register application services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IDocumentService, DocumentService>();

// Add HttpContextAccessor for accessing user claims
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var database = context.Database;
        var recreateDevelopmentDatabase = app.Environment.IsDevelopment()
            && builder.Configuration.GetValue<bool>("Database:RecreateOnSchemaMismatch");

        if (!database.CanConnect())
        {
            database.EnsureCreated();
        }
        else if (recreateDevelopmentDatabase && !HasDocumentsTable(context))
        {
            database.EnsureDeleted();
            database.EnsureCreated();
        }
        else
        {
            database.EnsureCreated();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred creating the database.");
    }
}

static bool HasDocumentsTable(ApplicationDbContext context)
{
    var connection = context.Database.GetDbConnection();
    var wasClosed = connection.State == ConnectionState.Closed;

    if (wasClosed)
    {
        connection.Open();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'Documents'";
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
    finally
    {
        if (wasClosed)
        {
            connection.Close();
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Use HSTS even in development for training purposes
    app.UseHsts();
}

// Add security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    // Content Security Policy for Blazor Server
    context.Response.Headers["Content-Security-Policy"] = 
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; " +
        "font-src 'self' https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self' wss: ws:;";
    
    await next();
});

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Enable authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/documents/download/{documentId:int}", async (int documentId, HttpContext context, IDocumentService documentService, IFileStorageService fileStorageService) =>
{
    if (context.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
    if (!int.TryParse(userIdClaim?.Value, out var requestingUserId))
    {
        return Results.Unauthorized();
    }

    var document = await documentService.GetDocumentByIdAsync(documentId, requestingUserId);
    if (document == null)
    {
        return Results.NotFound();
    }

    try
    {
        await using var stream = await fileStorageService.DownloadAsync(document.StoredFilePath);
        return Results.File(stream, document.MimeType ?? "application/octet-stream", document.FileName, enableRangeProcessing: true);
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound();
    }
});

app.MapGet("/documents/download", async (HttpContext context, ApplicationDbContext dbContext, IDocumentService documentService, IFileStorageService fileStorageService) =>
{
    if (context.User?.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
    if (!int.TryParse(userIdClaim?.Value, out var requestingUserId))
    {
        return Results.Unauthorized();
    }

    var storedPath = context.Request.Query["path"].ToString();
    if (string.IsNullOrWhiteSpace(storedPath))
    {
        return Results.BadRequest();
    }

    var document = await dbContext.Documents
        .FirstOrDefaultAsync(d => d.StoredFilePath == storedPath && !d.IsDeleted);

    if (document == null)
    {
        return Results.NotFound();
    }

    if (await documentService.GetDocumentByIdAsync(document.DocumentId, requestingUserId) == null)
    {
        return Results.Forbid();
    }

    try
    {
        await using var stream = await fileStorageService.DownloadAsync(document.StoredFilePath);
        return Results.File(stream, document.MimeType ?? "application/octet-stream", document.FileName, enableRangeProcessing: true);
    }
    catch (FileNotFoundException)
    {
        return Results.NotFound();
    }
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
