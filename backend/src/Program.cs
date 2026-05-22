using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using src.Data;
using src.Services;
using src.Utils;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
    });

builder.Services.AddSingleton<JwtService>();

builder.Services.AddHttpClient<LinkedInApiService>(client =>
{
    client.BaseAddress = new Uri($"https://{Environment.GetEnvironmentVariable("LINKEDIN_API_HOST")}/");
    client.DefaultRequestHeaders.Add("x-rapidapi-key", Environment.GetEnvironmentVariable("LINKEDIN_API_KEY"));
    client.DefaultRequestHeaders.Add("x-rapidapi-host", Environment.GetEnvironmentVariable("LINKEDIN_API_HOST"));
});

builder.Services.AddHttpClient<GeminiService>(client =>
{
    var apiKey = Environment.GetEnvironmentVariable("LLM_GOOGLE_API_KEY");
    client.BaseAddress = new Uri($"https://generativelanguage.googleapis.com/");
    client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddHttpClient<OpenAIService>(client =>
{
    var apiKey = Environment.GetEnvironmentVariable("LLM_OPENAI_API_KEY");
    client.BaseAddress = new Uri("https://api.openai.com/");
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    client.Timeout = TimeSpan.FromSeconds(120);
});

builder.Services.AddSingleton<JobFetchOrchestrator>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<JobFetchOrchestrator>());

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                context.Token = context.Request.Cookies["bimbajobs_auth"];
                return Task.CompletedTask;
            }
        };
    });

var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var connectionString = string.IsNullOrEmpty(databaseUrl)
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : DatabaseUrlParser.ToConnectionString(databaseUrl);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString!).UseSnakeCaseNamingConvention());

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
