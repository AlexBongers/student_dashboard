using Microsoft.EntityFrameworkCore;
using WebDashboard.Data;
using WebDashboard.Services;

var builder = WebApplication.CreateBuilder(args);

// Render.com provides a PORT environment variable; bind to it.
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Database path: use DATA_DIR env var (e.g. Render disk mount) or fall back to app directory.
var dataDir = Environment.GetEnvironmentVariable("DATA_DIR") ?? builder.Environment.ContentRootPath;
Directory.CreateDirectory(dataDir); // Ensure the directory exists
var dbPath = Path.Combine(dataDir, "students.db");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddScoped<StudentService>();
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

var app = builder.Build();

// Ensure DB is created on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();

// Fallback to index.html for SPA-style routing
app.MapFallbackToFile("index.html");

app.Run();
