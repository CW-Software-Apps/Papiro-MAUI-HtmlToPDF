#if !ANDROID && !IOS
namespace CwSoftware.Papiro;

public partial class HtmlToPdfService
{
    private partial Task<HtmlToPdfResult> ConvertVal(string html, string filePath)
    {
        return Task.FromResult(HtmlToPdfResult.Failure("Platform not supported for Native PDF generation."));
    }
}
#endif
