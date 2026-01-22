using CwSoftware.Papiro;
using PapiroSample.Services;

namespace PapiroSample;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UsePapiro() // 📄 Enable Papiro HTML to PDF
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Register services
        builder.Services.AddSingleton<TemplateService>();
        builder.Services.AddTransient<MainPage>();

        return builder.Build();
    }
}
