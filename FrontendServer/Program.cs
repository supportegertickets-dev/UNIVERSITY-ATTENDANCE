using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Get port from environment or default to 3000
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Serve static files from frontend folder
// Try multiple paths to find frontend folder
var frontendPath = "";
var possiblePaths = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), "..", "frontend"),  // ../frontend
    Path.Combine(Directory.GetCurrentDirectory(), "frontend"),         // ./frontend
    Path.Combine(AppContext.BaseDirectory, "..", "..", "frontend"),   // ../../frontend
    "/app/frontend",                                                    // Railway container path
    "./frontend"                                                        // Current directory
};

foreach (var path in possiblePaths)
{
    var fullPath = Path.GetFullPath(path);
    if (Directory.Exists(fullPath))
    {
        frontendPath = fullPath;
        break;
    }
}

if (!string.IsNullOrEmpty(frontendPath) && Directory.Exists(frontendPath))
{
    app.UseDefaultFiles(new DefaultFilesOptions 
    { 
        FileProvider = new PhysicalFileProvider(frontendPath),
        DefaultFileNames = new List<string> { "index.html" }
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(frontendPath),
        RequestPath = ""
    });
}
else
{
    // Fallback if frontend folder not found - return error with diagnostic info
    var debugInfo = $@"Frontend folder not found. 
Current directory: {Directory.GetCurrentDirectory()}
Base directory: {AppContext.BaseDirectory}
Searched paths: {string.Join(", ", possiblePaths)}";
    app.MapGet("/", () => Results.Text(debugInfo, statusCode: 500));
    app.MapFallback(() => Results.Text(debugInfo, statusCode: 500));
}

app.Run();
