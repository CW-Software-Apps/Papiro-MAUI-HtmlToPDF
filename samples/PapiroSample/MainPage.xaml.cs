using CwSoftware.Papiro;
using PapiroSample.Services;

namespace PapiroSample;

public partial class MainPage : ContentPage
{
    private readonly IHtmlToPdfService _pdfService;
    private readonly TemplateService _templateService;
    private readonly List<TemplateInfo> _templates;

    public MainPage(IHtmlToPdfService pdfService, TemplateService templateService)
    {
        InitializeComponent();
        _pdfService = pdfService;
        _templateService = templateService;
        _templates = TemplateService.GetAvailableTemplates().ToList();

        // Bind template picker
        TemplatePicker.ItemsSource = _templates.Select(t => t.DisplayName).ToList();
        TemplatePicker.SelectedIndex = 0;
    }

    private async void OnGenerateClicked(object sender, EventArgs e)
    {
        try
        {
            // Show loading state
            GenerateButton.IsEnabled = false;
            LoadingOverlay.IsVisible = true;

            StatusLabel.Text = "Ready to create"; // Reset main status
            StatusLabel.TextColor = Colors.Gray;

            // Parse amount
            if (!decimal.TryParse(TotalAmountEntry.Text?.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var totalAmount))
            {
                totalAmount = 0;
            }

            // Get logo as Base64
            LoadingLabel.Text = "Loading logo...";
            var logoBase64 = await HtmlTemplateHelper.EmbeddedResourceToBase64Async("papiro_logo.png");

            // Load template from file
            LoadingLabel.Text = "Processing template...";
            var selectedTemplate = _templates[TemplatePicker.SelectedIndex];
            var template = await _templateService.LoadTemplateAsync(selectedTemplate.FileName);

            // Replace tags with form values
            var html = HtmlTemplateHelper.ReplaceTags(template, new
            {
                LogoBase64 = logoBase64,
                CompanyName = HtmlTemplateHelper.HtmlEncode(CompanyNameEntry.Text),
                ClientName = HtmlTemplateHelper.HtmlEncode(ClientNameEntry.Text),
                ReportNumber = HtmlTemplateHelper.HtmlEncode(ReportNumberEntry.Text),
                ReportDate = HtmlTemplateHelper.FormatDate(ReportDatePicker.Date ?? DateTime.Now),
                Description = HtmlTemplateHelper.HtmlEncode(DescriptionEditor.Text),
                TotalAmount = HtmlTemplateHelper.FormatCurrency(totalAmount),
                Website = "https://www.cwsoftware.com.br",
                GeneratedAt = HtmlTemplateHelper.FormatDate(DateTime.Now, "dd/MM/yyyy HH:mm:ss")
            });

            // Generate PDF
            LoadingLabel.Text = "Generating PDF...";
            // Add a small delay so user can see the beautiful loading screen :)
            await Task.Delay(500);

            var fileName = $"report_{ReportNumberEntry.Text?.Replace("/", "-") ?? "doc"}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var result = await _pdfService.ConvertAndSaveAsync(html, fileName);

            if (result.IsSuccess)
            {
                StatusLabel.Text = "✅ PDF generated successfully!";
                StatusLabel.TextColor = Colors.Green;

                // Share the PDF
                // Open the PDF directly
                await Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    Title = $"Report {ReportNumberEntry.Text}",
                    File = new ReadOnlyFile(result.FilePath!)
                });
            }
            else
            {
                StatusLabel.Text = $"❌ Error: {result.ErrorMessage}";
                StatusLabel.TextColor = Colors.Red;
            }
        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"❌ Error: {ex.Message}";
            StatusLabel.TextColor = Colors.Red;
        }
        finally
        {
            GenerateButton.IsEnabled = true;
            LoadingOverlay.IsVisible = false;
        }
    }
}
