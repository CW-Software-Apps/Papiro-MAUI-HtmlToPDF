namespace CwSoftware.Papiro;

public partial class HtmlToPdfService : IHtmlToPdfService
{
    private const string OutputSubdirectory = "generated_pdfs";

    // PDFs gerados anteriores que não foram limpos pelo app (crash, forçar fechar, etc.) ficam
    // acumulando no cache do dispositivo indefinidamente. Uma limpeza best-effort a cada chamada
    // evita que isso vire um problema de espaço em disco com o uso contínuo do app.
    private static readonly TimeSpan MaxCacheAge = TimeSpan.FromHours(24);

    public async Task<HtmlToPdfResult> ConvertAndSaveAsync(string htmlContent, string? fileName = null)
    {
        if (string.IsNullOrWhiteSpace(htmlContent))
            return HtmlToPdfResult.Failure("HTML content cannot be empty.");

        fileName = SanitizeFileName(fileName) ?? $"doc_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

        string outputDir = Path.Combine(FileSystem.CacheDirectory, OutputSubdirectory);
        if (!Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);

        CleanupOldFiles(outputDir);

        string outputPath = Path.Combine(outputDir, fileName);

        // Timeout scales with content size: base 60s + 1s per KB (max 5 minutes)
        int contentKb = htmlContent.Length / 1024;
        int timeoutSeconds = Math.Min(300, 60 + contentKb);

        using var cts = new CancellationTokenSource();

        try
        {
            var conversionTask = ConvertVal(htmlContent, outputPath, cts.Token);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeoutSeconds));

            var completedTask = await Task.WhenAny(conversionTask, timeoutTask);

            if (completedTask == timeoutTask)
            {
                // Sinaliza para a implementação da plataforma abandonar e liberar a WebView em
                // andamento, em vez de deixá-la rodando "zumbi" em segundo plano até terminar
                // sozinha (ou nunca terminar, no caso do bug que motivou este timeout existir).
                cts.Cancel();
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
    private partial Task<HtmlToPdfResult> ConvertVal(string html, string filePath, CancellationToken cancellationToken);

    /// <summary>
    /// Garante que o nome do arquivo informado pelo chamador não escape do diretório de saída
    /// (path traversal, ex.: "../../etc/passwd") nem contenha caracteres inválidos para o sistema
    /// de arquivos. Retorna null se, após sanitizado, sobrar um nome vazio — nesse caso o chamador
    /// usa o nome gerado automaticamente.
    /// </summary>
    private static string? SanitizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var safeName = Path.GetFileName(fileName.Trim());

        foreach (var invalidChar in Path.GetInvalidFileNameChars())
            safeName = safeName.Replace(invalidChar, '_');

        if (string.IsNullOrWhiteSpace(safeName))
            return null;

        if (!safeName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            safeName += ".pdf";

        return safeName;
    }

    /// <summary>
    /// Remove, em melhor esforço, PDFs antigos deixados no cache por gerações anteriores que não
    /// foram limpas pelo app chamador. Nunca lança — qualquer falha (arquivo em uso, permissão,
    /// etc.) é apenas ignorada, já que isso é só housekeeping e não pode atrapalhar a conversão
    /// atual.
    /// </summary>
    private static void CleanupOldFiles(string outputDir)
    {
        try
        {
            var cutoff = DateTime.UtcNow - MaxCacheAge;
            foreach (var file in Directory.EnumerateFiles(outputDir, "*.pdf"))
            {
                try
                {
                    if (File.GetLastWriteTimeUtc(file) < cutoff)
                        File.Delete(file);
                }
                catch
                {
                    // Ignora arquivo individual com problema (em uso, sem permissão, etc.) e
                    // continua limpando os demais.
                }
            }
        }
        catch
        {
            // Diretório inacessível ou outro problema de I/O — não é crítico, a conversão segue.
        }
    }
}
