using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Get port from environment or default to 3000
var port = Environment.GetEnvironmentVariable("PORT") ?? "3000";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

var app = builder.Build();

// Serve static files from frontend folder
var frontendPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "frontend");

// If frontend folder doesn't exist in parent, try current directory
if (!Directory.Exists(frontendPath))
{
    frontendPath = Path.Combine(Directory.GetCurrentDirectory(), "frontend");
}

if (Directory.Exists(frontendPath))
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
    // Fallback if frontend folder not found
    app.MapGet("/", () => "Frontend folder not found. Checked: " + frontendPath);
}

app.Run();
