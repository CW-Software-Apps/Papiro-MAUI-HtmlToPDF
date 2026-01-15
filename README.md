# Papiro - Native HTML to PDF for .NET MAUI

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![Platforms](https://img.shields.io/badge/Platforms-Android%20%7C%20iOS-lightgrey)](https://github.com/CW-Software-Apps/Papiro-MAUI-HtmlToPDF)

A **lightweight, free, and fully native** library for converting HTML to PDF in .NET MAUI applications. No external dependencies, no expensive licenses, no embedded browsers.

## ✨ Features

- 🆓 **100% Free** – Uses native platform APIs. No iText, no Chromium, no Syncfusion licenses needed.
- ⚡ **Lightweight** – No heavy dependencies. Just clean, native code.
- 📄 **A4 Pagination** – Automatically splits long content into multiple pages.
- 🎨 **High Quality** – Uses 3x scale factor (~220 DPI) for crisp text and images.
- 📱 **Native Rendering** – Uses the same rendering engine as the platform's WebView.
- 🔄 **Async/Await** – Modern async API with detailed result object.

## 📦 Installation

Add the project reference or NuGet package to your .NET MAUI project:

```xml
<PackageReference Include="CwSoftware.Papiro" Version="1.0.0" />
```

Or add as a project reference:

```xml
<ProjectReference Include="..\Maui.HtmlToPdf\Maui.HtmlToPdf.csproj" />
```

## 🚀 Quick Start

### 1. Register the Service

In your `MauiProgram.cs`:

```csharp
using CwSoftware.Papiro;

public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    
    builder
        .UseMauiApp<App>()
        .UsePapiro();  // ✅ Add this line
    
    return builder.Build();
}
```

### 2. Use the Service

Inject `IHtmlToPdfService` into your ViewModel or Page:

```csharp
using CwSoftware.Papiro;

public class ReportViewModel
{
    private readonly IHtmlToPdfService _pdfService;

    public ReportViewModel(IHtmlToPdfService pdfService)
    {
        _pdfService = pdfService;
    }

    public async Task GenerateReportAsync()
    {
        string html = @"
            <html>
            <head>
                <style>
                    body { font-family: Arial, sans-serif; padding: 20px; }
                    h1 { color: #2c3e50; }
                    table { width: 100%; border-collapse: collapse; }
                    th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                    th { background-color: #3498db; color: white; }
                </style>
            </head>
            <body>
                <h1>Sales Report</h1>
                <table>
                    <tr><th>Product</th><th>Quantity</th><th>Total</th></tr>
                    <tr><td>Widget A</td><td>100</td><td>$1,000</td></tr>
                    <tr><td>Widget B</td><td>50</td><td>$750</td></tr>
                </table>
            </body>
            </html>";

        var result = await _pdfService.ConvertAndSaveAsync(html, "sales-report.pdf");

        if (result.IsSuccess)
        {
            Console.WriteLine($"✅ PDF saved to: {result.FilePath}");
            // Share or open the PDF
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Sales Report",
                File = new ShareFile(result.FilePath!)
            });
        }
        else
        {
            Console.WriteLine($"❌ Error: {result.ErrorMessage}");
        }
    }
}
```

## 📖 API Reference

### `IHtmlToPdfService`

```csharp
public interface IHtmlToPdfService
{
    /// <summary>
    /// Converts HTML to PDF and saves to a temporary file.
    /// </summary>
    /// <param name="htmlContent">Complete HTML content to render.</param>
    /// <param name="fileName">Optional filename (auto-generated if not provided).</param>
    /// <returns>Result with success status, file path, or error message.</returns>
    Task<HtmlToPdfResult> ConvertAndSaveAsync(string htmlContent, string? fileName = null);
}
```

### `HtmlToPdfResult`

```csharp
public class HtmlToPdfResult
{
    public bool IsSuccess { get; set; }
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
}
```

## 📱 Platform Support

| Platform | Minimum Version | Implementation |
|----------|----------------|----------------|
| **Android** | API 21 (Lollipop) | `WebView` + `PdfDocument` with Canvas pagination |
| **iOS** | iOS 11+ | `UIPrintPageRenderer` + `UIMarkupTextPrintFormatter` |

## 🔧 Technical Details

### Android Implementation
- Uses native `Android.Webkit.WebView` to render HTML
- Creates PDF using `Android.Graphics.Pdf.PdfDocument`
- Custom `WebViewClient` to detect page load completion
- Manual Canvas pagination for multi-page documents
- A4 page size: 595 × 842 points (scaled 3x for quality)

### iOS Implementation
- Uses `UIMarkupTextPrintFormatter` to parse HTML
- Renders to PDF via `UIPrintPageRenderer`
- Native `NSData` to byte stream conversion
- Automatic page breaking handled by iOS

## ⚠️ Limitations

1. **CSS Support** – Limited to what the platform's WebView supports
2. **JavaScript** – JavaScript is enabled but execution timing may vary
3. **External Resources** – Images/fonts must be inlined (base64) or accessible via URL
4. **Windows/macOS** – Currently only Android and iOS are supported

## 💡 Tips for Best Results

```html
<!-- Inline CSS for reliable styling -->
<style>
    * { -webkit-print-color-adjust: exact; }
    body { margin: 0; padding: 20px; }
    @page { margin: 0; }
</style>

<!-- Use base64 for images -->
<img src="data:image/png;base64,iVBORw0KGgo..." />

<!-- Force page breaks -->
<div style="page-break-before: always;"></div>
```

## 📄 License

MIT License - Free for commercial and personal use.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

Made with ❤️ by [CW Software](https://github.com/CW-Software-Apps)
