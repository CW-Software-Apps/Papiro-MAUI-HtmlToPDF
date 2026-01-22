namespace CwSoftware.Papiro;

public interface IHtmlToPdfService
{
    /// <summary>
    /// Converts the provided HTML string to a PDF file and saves it to a temporary location.
    /// </summary>
    /// <param name="htmlContent">The complete HTML content to render.</param>
    /// <param name="fileName">Optional filename. If not provided, a random name will be generated.</param>
    /// <returns>IsSuccess: Boolean indicating success. FilePath: Full path to the generated PDF. ErrorMessage: Error details if failed.</returns>
    Task<HtmlToPdfResult> ConvertAndSaveAsync(string htmlContent, string? fileName = null);
}

public class HtmlToPdfResult
{
    public bool IsSuccess { get; set; }
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }

    public static HtmlToPdfResult Success(string path) => new() { IsSuccess = true, FilePath = path };
    public static HtmlToPdfResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}
