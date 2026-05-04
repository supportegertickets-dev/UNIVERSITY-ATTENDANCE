using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.FileProviders;
using System.Text;
using AttendanceAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add environment variables to configuration
builder.Configuration.AddEnvironmentVariables();

// MongoDB
var mongoUrl = Environment.GetEnvironmentVariable("MONGODB_URL") ?? 
               builder.Configuration["MongoDbSettings:ConnectionString"] ?? 
               "mongodb://localhost:27017";

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? 
                builder.Configuration["Jwt:Key"] ?? 
                "UniversityAttendanceSecretKey2024!!";

// Configure MongoDbSettings
builder.Services.Configure<MongoDbSettings>(options =>
{
    options.ConnectionString = mongoUrl;
    options.DatabaseName = builder.Configuration["MongoDbSettings:DatabaseName"] ?? "UniversityAttendanceDB";
});

builder.Services.AddSingleton<MongoDbService>();

// JWT Auth
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — allow frontend
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowAll");

// Serve static files from wwwroot (frontend)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Fallback to index.html for SPA routing
app.MapFallback(() => Results.File("wwwroot/index.html", "text/html"));

// Listen on Railway PORT environment variable (default 8080)
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Add($"http://0.0.0.0:{port}");
app.Run();