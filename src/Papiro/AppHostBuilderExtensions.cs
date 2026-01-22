using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Hosting;

namespace CwSoftware.Papiro;

public static class AppHostBuilderExtensions
{
    /// <summary>
    /// Registers the Papiro (HTML to PDF) service.
    /// </summary>
    public static MauiAppBuilder UsePapiro(this MauiAppBuilder builder)
    {
        builder.Services.AddSingleton<IHtmlToPdfService, HtmlToPdfService>();
        return builder;
    }
}
