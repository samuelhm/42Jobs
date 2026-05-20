using Microsoft.EntityFrameworkCore;
using src.Data;

var builder = WebApplication.CreateBuilder(args);

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = string.IsNullOrEmpty(databaseUrl)
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : ConvertDatabaseUrl(databaseUrl);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

// Endpoint de prueba: muestra si la BD responde
app.MapGet("/db-test", async (AppDbContext db) =>
{
    var now = await db.Database.SqlQueryRaw<DateTime>(
        db.Database.IsNpgsql()
            ? "SELECT NOW()"
            : "SELECT GETDATE()"
    ).FirstOrDefaultAsync();
    return new { status = "ok", server_time = now };
});

app.Run();

static string ConvertDatabaseUrl(string url)
{
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':');
    var user = userInfo[0];
    var password = userInfo.Length > 1 ? userInfo[1] : "";
    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.TrimStart('/')};Username={user};Password={password}";
}
