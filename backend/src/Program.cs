using Microsoft.EntityFrameworkCore;
using src.Data;
using src.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = string.IsNullOrEmpty(databaseUrl)
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : DatabaseUrlParser.ToConnectionString(databaseUrl);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString!));

var app = builder.Build();

app.MapControllers();

app.Run();
