# Papiro - Native HTML to PDF for .NET MAUI

![Papiro Logo](PapiroLogo.png)

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![NuGet](https://img.shields.io/nuget/v/CwSoftware.Papiro.svg)](https://www.nuget.org/packages/CwSoftware.Papiro)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![Platforms](https://img.shields.io/badge/Platforms-Android%20%7C%20iOS-lightgrey)](https://github.com/CW-Software-Apps/Papiro-MAUI-HtmlToPDF)

A **lightweight, free, and fully native** library for converting HTML to PDF in .NET MAUI applications. No external dependencies, no expensive licenses, no embedded browsers.

---

## 🤔 Why Papiro?

Creating reports in mobile apps shouldn't be complicated. Sometimes you just need a **simple, beautiful PDF** — an invoice, a service order, a summary report.

**The Problem:**

- Complex PDF libraries require expensive licenses (iText, Syncfusion)
- Low-level PDF APIs are tedious and hard to style
- Embedded browser solutions add heavy dependencies
- Most MAUI solutions are overkill for simple reports

**The Solution:**
HTML is the perfect template language. Everyone knows it. It's easy to design, easy to preview in any browser, and supports rich styling with CSS.

**Papiro** bridges this gap: write your report in HTML, use simple `{{tag}}` placeholders for dynamic data, and generate a native PDF in seconds. No licenses, no bloat, no complexity.

---

## ✨ Features

| Feature | Description |
|---------|-------------|
| 🆓 **100% Free** | Uses native platform APIs. No iText, no Chromium, no Syncfusion licenses. |
| ⚡ **Lightweight** | No heavy dependencies. Just clean, native code. |
| 📄 **A4 Pagination** | Automatically splits long content into multiple pages. |
| 🎨 **High Quality** | Uses 3x scale factor (~220 DPI) for crisp text and images. |
| 📱 **Native Rendering** | Uses the same rendering engine as the platform's WebView. |
| 🔄 **Async/Await** | Modern async API with detailed result object. |
| 🏷️ **Template Support** | Simple `{{tag}}` substitution with included helper class. |
| ⏱️ **Timeout Protection** | Built-in 30-second timeout prevents infinite hangs. |

---

## 📦 Installation

Add the NuGet package to your .NET MAUI project:

```xml
<PackageReference Include="CwSoftware.Papiro" Version="1.0.0" />
```

Or add as a project reference:

```xml
<ProjectReference Include="..\Papiro\src\Papiro\Papiro.csproj" />
```

---

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

### 2. Generate a PDF

Inject `IHtmlToPdfService` and convert HTML:

```csharp
using CwSoftware.Papiro;

public class ReportService
{
    private readonly IHtmlToPdfService _pdfService;

    public ReportService(IHtmlToPdfService pdfService)
    {
        _pdfService = pdfService;
    }

    public async Task<string?> GenerateReportAsync()
    {
        string html = @"
            <html>
            <head>
                <style>
                    body { font-family: Arial, sans-serif; padding: 40px; }
                    h1 { color: #3498db; border-bottom: 2px solid #3498db; }
                    table { width: 100%; border-collapse: collapse; margin-top: 20px; }
                    th, td { border: 1px solid #ddd; padding: 12px; text-align: left; }
                    th { background: linear-gradient(135deg, #667eea, #764ba2); color: white; }
                    .total { font-size: 24px; font-weight: bold; color: #27ae60; }
                </style>
            </head>
            <body>
                <h1>📊 Sales Report</h1>
                <table>
                    <tr><th>Product</th><th>Qty</th><th>Total</th></tr>
                    <tr><td>Widget Pro</td><td>100</td><td>$1,500.00</td></tr>
                    <tr><td>Widget Basic</td><td>250</td><td>$2,500.00</td></tr>
                </table>
                <p class='total'>Grand Total: $4,000.00</p>
            </body>
            </html>";

        var result = await _pdfService.ConvertAndSaveAsync(html, "sales-report.pdf");

        if (result.IsSuccess)
        {
            // Open or share the PDF
            await Launcher.Default.OpenAsync(new OpenFileRequest
            {
                Title = "Sales Report",
                File = new ReadOnlyFile(result.FilePath!)
            });
            return result.FilePath;
        }
        
        Console.WriteLine($"Error: {result.ErrorMessage}");
        return null;
    }
}
```

---

## 🏷️ Template System with Tag Substitution

Papiro includes `HtmlTemplateHelper` for easy `{{tag}}` substitution:

```csharp
using CwSoftware.Papiro;

// Define your HTML template with {{TagName}} placeholders
string template = @"
<html>
<body>
    <h1>{{CompanyName}}</h1>
    <p>Client: {{ClientName}}</p>
    <p>Date: {{ReportDate}}</p>
    <p>Total: {{TotalAmount}}</p>
</body>
</html>";

// Replace tags using anonymous object
string html = HtmlTemplateHelper.ReplaceTags(template, new 
{
    CompanyName = "ACME Corporation",
    ClientName = "John Doe",
    ReportDate = DateTime.Now.ToString("dd/MM/yyyy"),
    TotalAmount = "R$ 15.750,00"
});

// Or use a Dictionary
var values = new Dictionary<string, string?>
{
    ["CompanyName"] = "ACME Corporation",
    ["ClientName"] = "John Doe"
};
string html = HtmlTemplateHelper.ReplaceTags(template, values);
```

### Using External HTML Template Files

For better maintainability, store your templates as `.html` files in your app's resources:

```text
Resources/Raw/Templates/
├── professional_report.html
├── minimalist_report.html
└── invoice_template.html
```

Then load and process them at runtime:

```csharp
// Load template from Resources/Raw
using var stream = await FileSystem.OpenAppPackageFileAsync("Templates/invoice_template.html");
using var reader = new StreamReader(stream);
string template = await reader.ReadToEndAsync();

// Replace tags
string html = HtmlTemplateHelper.ReplaceTags(template, new 
{
    CompanyName = "ACME Inc.",
    ClientName = clientName,
    InvoiceDate = DateTime.Now.ToString("dd/MM/yyyy")
});

// Generate PDF
var result = await _pdfService.ConvertAndSaveAsync(html, "invoice.pdf");
```

### Embedding Images as Base64

```csharp
// From file
string logoBase64 = await HtmlTemplateHelper.ImageToBase64Async("/path/to/logo.png");

// From embedded resource (MauiAsset)
string logoBase64 = await HtmlTemplateHelper.EmbeddedResourceToBase64Async("logo.png");

// Use in template
string template = @"<img src='{{LogoBase64}}' alt='Logo' />";
string html = HtmlTemplateHelper.ReplaceTags(template, new { LogoBase64 = logoBase64 });
```

---

## 📱 Sample Application

Check out the complete sample app in [`samples/PapiroSample`](samples/PapiroSample/) that demonstrates:

- ✅ **Form input** with 6 editable fields
- ✅ **Tag substitution** with `{{TagName}}` placeholders
- ✅ **HTML templates as files** in `Resources/Raw/Templates/`
- ✅ **Image embedding** with Base64 conversion
- ✅ **Multiple templates** (Professional & Minimalist)
- ✅ **Full-screen loading overlay** with progress messages
- ✅ **PDF opening** via native launcher

### Running the Sample

1. Open `Papiro.sln` in Visual Studio.
2. In the **Solution Explorer**, right-click on the `PapiroSample` project.
3. Select **Set as Startup Project**.
4. Select **net10.0-android** or your target device from the debug dropdown.
5. Press F5 to build and run.

Or via command line:

```bash
cd samples/PapiroSample
dotnet build -f net10.0-android
dotnet build -f net10.0-ios
```

---

## 📁 Project Structure

```text
Papiro/
├── src/Papiro/                    # 📦 Library
│   ├── Papiro.csproj
│   ├── IHtmlToPdfService.cs       # Service interface
│   ├── HtmlToPdfService.cs        # Core implementation (+ timeout)
│   ├── HtmlToPdfService.android.cs
│   ├── HtmlToPdfService.ios.cs
│   ├── AppHostBuilderExtensions.cs
│   └── HtmlTemplateHelper.cs      # Tag substitution helper
│
├── samples/PapiroSample/          # 📱 Sample App
│   ├── Services/TemplateService.cs
│   ├── Resources/Raw/Templates/
│   │   ├── professional_report.html
│   │   └── minimalist_report.html
│   └── ...
│
├── README.md
└── PapiroLogo.png
```

---

## 📖 API Reference

### `IHtmlToPdfService`

```csharp
public interface IHtmlToPdfService
{
    /// <summary>
    /// Converts HTML to PDF and saves to a temporary file.
    /// Includes a 30-second timeout to prevent hangs.
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

### `HtmlTemplateHelper`

| Method | Description |
|--------|-------------|
| `ReplaceTags(template, object)` | Replace `{{tags}}` with object properties |
| `ReplaceTags(template, Dictionary)` | Replace `{{tags}}` with dictionary values |
| `ImageToBase64Async(path)` | Convert image file to Base64 data URI |
| `EmbeddedResourceToBase64Async(name)` | Convert embedded resource to Base64 |
| `HtmlEncode(text)` | Escape HTML special characters |
| `FormatCurrency(value)` | Format as currency (pt-BR) |
| `FormatDate(date, format)` | Format DateTime |

---

## 📱 Platform Support

| Platform | Minimum Version | Implementation |
|----------|-----------------|----------------|
| **Android** | API 21 (Lollipop) | `WebView` + `PdfDocument` with Canvas pagination |
| **iOS** | iOS 11+ | `UIPrintPageRenderer` + `UIMarkupTextPrintFormatter` |

---

## 🔧 Technical Details

### Android Implementation

- Uses native `Android.Webkit.WebView` to render HTML
- Creates PDF using `Android.Graphics.Pdf.PdfDocument`
- Custom `WebViewClient` to detect page load completion and errors
- Manual Canvas pagination for multi-page documents
- A4 page size: 595 × 842 points (scaled 3x for quality)
- 30-second timeout to prevent infinite hangs

### iOS Implementation

- Uses `UIMarkupTextPrintFormatter` to parse HTML
- Renders to PDF via `UIPrintPageRenderer`
- Native `NSData` to byte stream conversion
- Automatic page breaking handled by iOS

---

## ⚠️ Limitations

1. **CSS Support** – Limited to what the platform's WebView supports
2. **JavaScript** – JavaScript is enabled but execution timing may vary
3. **External Resources** – Images/fonts should be inlined (base64) for reliability
4. **Windows/macOS** – Currently only Android and iOS are supported

---

## 💡 Tips for Best Results

```html
<!-- Inline CSS for reliable styling -->
<style>
    * { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
    body { margin: 0; padding: 20px; }
    @page { size: A4; margin: 0; }
</style>

<!-- Use base64 for images -->
<img src="data:image/png;base64,iVBORw0KGgo..." />

<!-- Force page breaks -->
<div style="page-break-before: always;"></div>
```

---

## 📄 License

MIT License - Free for commercial and personal use.

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

---

Made with ❤️ by [CW Software](https://github.com/CW-Software-Apps)
