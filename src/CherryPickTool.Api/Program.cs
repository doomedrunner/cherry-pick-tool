using CherryPickTool.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Register Core services
builder.Services.AddSingleton<IGitService, GitService>();
builder.Services.AddSingleton<IGitHubService, GitHubService>();
builder.Services.AddSingleton<CherryPickOrchestrator>();

var app = builder.Build();

app.UseCors("ReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();
