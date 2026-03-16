using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure the database.
// On Render.com, set the DATABASE_URL environment variable to the PostgreSQL connection string.
// Locally, falls back to SQLite.
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(databaseUrl))
{
    // Render.com provides PostgreSQL URL in the format:
    //   postgres://user:pass@host:port/dbname
    // Npgsql 6+ supports this format directly.
    // If the URL starts with "postgres://", rewrite to "postgresql://" for Npgsql compatibility.
    if (databaseUrl.StartsWith("postgres://"))
        databaseUrl = "postgresql://" + databaseUrl["postgres://".Length..];

    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseNpgsql(databaseUrl));
}
else
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=students.db";
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(connectionString));
}

builder.Services.AddScoped<StudentService>();

var app = builder.Build();

// Apply database migrations / ensure database exists at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
