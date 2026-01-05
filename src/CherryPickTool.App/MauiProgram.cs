using CherryPickTool.App.ViewModels;
using CherryPickTool.Core.Services;
using Microsoft.Extensions.Logging;

namespace CherryPickTool.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services
        builder.Services.AddSingleton<IGitService, GitService>();
        builder.Services.AddSingleton<IGitHubService, GitHubService>();
        builder.Services.AddSingleton<CherryPickOrchestrator>();

        // Register ViewModels
        builder.Services.AddTransient<MainViewModel>();

        // Register Pages and App
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<App>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
