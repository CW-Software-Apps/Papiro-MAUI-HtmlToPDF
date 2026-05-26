namespace CwSoftware.Papiro;

public partial class HtmlToPdfService : IHtmlToPdfService
{
    public async Task<HtmlToPdfResult> ConvertAndSaveAsync(string htmlContent, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return HtmlToPdfResult.Failure("HTML content cannot be empty.");

        fileName ??= $"doc_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

        string outputDir = Path.Combine(FileSystem.CacheDirectory, "generated_pdfs");
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        string outputPath = Path.Combine(outputDir, fileName);

        try
        {
            // Timeout scales with content size: base 60s + 1s per KB (max 5 minutes)
            int contentKb = htmlContent.Length / 1024;
            int timeoutSeconds = Math.Min(300, 60 + contentKb);

            var conversionTask = ConvertVal(htmlContent, outputPath);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

            var completedTask = await Task.WhenAny(conversionTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                return HtmlToPdfResult.Failure($"PDF generation timed out after {timeoutSeconds} seconds. This might be caused by large images, complex loop in scripts, or resource loading issues.");
            }

            return await conversionTask;
        }
        catch (Exception ex)
        {
            return HtmlToPdfResult.Failure($"Conversion failed: {ex.Message}");
        }
    }

    // Partial method to be implemented by platforms
    private partial Task<HtmlToPdfResult> ConvertVal(string html, string filePath);
}
