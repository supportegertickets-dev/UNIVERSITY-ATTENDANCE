using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://localhost:3000");

var app = builder.Build();

// Serve static files from frontend folder
var frontendPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "frontend");
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
    app.MapGet("/", () => "Frontend folder not found at: " + frontendPath);
}

app.Run();
