using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace CwSoftware.Papiro;

/// <summary>
/// Helper class for HTML template processing with tag substitution.
/// Replaces {{TagName}} placeholders with actual values.
/// </summary>
public static partial class HtmlTemplateHelper
{
    /// <summary>
    /// Regex pattern to match {{TagName}} placeholders.
    /// </summary>
    [GeneratedRegex(@"\{\{(\w+)\}\}", RegexOptions.Compiled)]
    private static partial Regex TagPattern();

    /// <summary>
    /// Replaces all {{TagName}} placeholders in the template with values from a dictionary.
    /// </summary>
    /// <param name="template">HTML template with {{TagName}} placeholders.</param>
    /// <param name="values">Dictionary mapping tag names to values.</param>
    /// <returns>HTML with placeholders replaced by actual values.</returns>
    public static string ReplaceTags(string template, IDictionary<string, string?> values)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return TagPattern().Replace(template, match =>
        {
            var tagName = match.Groups[1].Value;
            return values.TryGetValue(tagName, out var value)
                ? value ?? string.Empty
                : match.Value; // Keep original if not found
        });
    }

    /// <summary>
    /// Replaces all {{TagName}} placeholders in the template with values from an anonymous object.
    /// Property names become tag names.
    /// </summary>
    /// <param name="template">HTML template with {{TagName}} placeholders.</param>
    /// <param name="model">Object whose properties will be used as tag values.</param>
    /// <returns>HTML with placeholders replaced by actual values.</returns>
    public static string ReplaceTags(string template, object model)
    {
        if (string.IsNullOrEmpty(template) || model == null)
            return template;

        var properties = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var values = new Dictionary<string, string?>();

        foreach (var prop in properties)
        {
            var value = prop.GetValue(model);
            values[prop.Name] = value?.ToString();
        }

        return ReplaceTags(template, values);
    }

    /// <summary>
    /// Converts an image file to a Base64 data URI for embedding in HTML.
    /// </summary>
    /// <param name="imagePath">Path to the image file.</param>
    /// <param name="mimeType">MIME type (e.g., "image/png", "image/jpeg").</param>
    /// <returns>Base64 data URI string.</returns>
    public static async Task<string> ImageToBase64Async(string imagePath, string mimeType = "image/png")
    {
        if (!File.Exists(imagePath))
            return string.Empty;

        var bytes = await File.ReadAllBytesAsync(imagePath);
        return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
    }

    /// <summary>
    /// Converts a stream to a Base64 data URI for embedding in HTML.
    /// </summary>
    /// <param name="stream">Image stream.</param>
    /// <param name="mimeType">MIME type (e.g., "image/png", "image/jpeg").</param>
    /// <returns>Base64 data URI string.</returns>
    public static async Task<string> StreamToBase64Async(Stream stream, string mimeType = "image/png")
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        return $"data:{mimeType};base64,{Convert.ToBase64String(memoryStream.ToArray())}";
    }

    /// <summary>
    /// Loads an embedded resource as a Base64 data URI.
    /// </summary>
    /// <param name="resourcePath">Resource path (e.g., "papiro_logo.png").</param>
    /// <param name="mimeType">MIME type.</param>
    /// <returns>Base64 data URI string.</returns>
    public static async Task<string> EmbeddedResourceToBase64Async(string resourcePath, string mimeType = "image/png")
    {
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(resourcePath);
            return await StreamToBase64Async(stream, mimeType);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Escapes HTML special characters in a string.
    /// </summary>
    public static string HtmlEncode(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return System.Net.WebUtility.HtmlEncode(text);
    }

    /// <summary>
    /// Formats a decimal as currency.
    /// </summary>
    public static string FormatCurrency(decimal value, string culture = "pt-BR")
    {
        return value.ToString("C2", new System.Globalization.CultureInfo(culture));
    }

    /// <summary>
    /// Formats a DateTime.
    /// </summary>
    public static string FormatDate(DateTime date, string format = "dd/MM/yyyy")
    {
        return date.ToString(format);
    }
}
