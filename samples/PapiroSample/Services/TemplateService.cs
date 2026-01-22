namespace PapiroSample.Services;

/// <summary>
/// Service for loading and processing HTML templates from Resources.
/// </summary>
public class TemplateService
{
    private const string TemplatesFolder = "Templates";

    /// <summary>
    /// Available template names.
    /// </summary>
    public static class Templates
    {
        public const string Professional = "professional_report.html";
        public const string Minimalist = "minimalist_report.html";
    }

    /// <summary>
    /// Loads an HTML template from the Resources/Raw/Templates folder.
    /// </summary>
    /// <param name="templateName">Template filename (e.g., "professional_report.html").</param>
    /// <returns>The HTML template content.</returns>
    public async Task<string> LoadTemplateAsync(string templateName)
    {
        var resourcePath = $"{TemplatesFolder}/{templateName}";

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(resourcePath);
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
        catch (FileNotFoundException)
        {
            throw new FileNotFoundException($"Template not found: {resourcePath}");
        }
    }

    /// <summary>
    /// Gets a list of all available templates.
    /// </summary>
    public static IReadOnlyList<TemplateInfo> GetAvailableTemplates() =>
    [
        new("✨ Professional (Colorful)", Templates.Professional),
        new("📋 Minimalist (Clean)", Templates.Minimalist)
    ];
}

/// <summary>
/// Information about an available template.
/// </summary>
public record TemplateInfo(string DisplayName, string FileName);
