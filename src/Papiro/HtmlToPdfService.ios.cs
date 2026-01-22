#if IOS
using Foundation;
using UIKit;
using WebKit;
using CoreGraphics;

namespace CwSoftware.Papiro;

public partial class HtmlToPdfService
{
    private partial async Task<HtmlToPdfResult> ConvertVal(string html, string filePath)
    {
        var tcs = new TaskCompletionSource<HtmlToPdfResult>();

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                var webView = new WKWebView(new CGRect(0, 0, 595, 842), new WKWebViewConfiguration());
                var delegateHandler = new WebViewNavigationDelegate(async () =>
                {
                    try
                    {
                        var renderer = new UIPrintPageRenderer();
                        var formatter = webView.ViewPrintFormatter;
                        
                        renderer.AddPrintFormatter(formatter, 0);

                        // A4 paper size (595 x 842 points)
                        var paperRect = new CGRect(0, 0, 595, 842);
                        var printableRect = new CGRect(0, 0, 595, 842); // Adjust margins here if needed

                        renderer.SetValueForKey(NSValue.FromCGRect(paperRect), new NSString("paperRect"));
                        renderer.SetValueForKey(NSValue.FromCGRect(printableRect), new NSString("printableRect"));

                        var data = new NSMutableData();
                        UIGraphics.BeginPDFContext(data, paperRect, null);
                        
                        // PrepareForDrawing is not needed or not available in C# binding
                        
                        var pages = renderer.NumberOfPages;
                        var bounds = UIGraphics.PDFContextBounds;

                        for (int i = 0; i < pages; i++)
                        {
                            UIGraphics.BeginPDFPage();
                            renderer.DrawPage(i, bounds);
                        }

                        UIGraphics.EndPDFContext();

                        data.Save(NSUrl.FromFilename(filePath), true);
                        tcs.TrySetResult(HtmlToPdfResult.Success(filePath));
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetResult(HtmlToPdfResult.Failure(ex.Message));
                    }
                });

                webView.NavigationDelegate = delegateHandler;
                webView.LoadHtmlString(html, new NSUrl("about:blank"));
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(HtmlToPdfResult.Failure(ex.Message));
            }
        });

        return await tcs.Task;
    }

    private class WebViewNavigationDelegate : WKNavigationDelegate
    {
        private readonly Func<Task> _onFinished;

        public WebViewNavigationDelegate(Func<Task> onFinished)
        {
            _onFinished = onFinished;
        }

        public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
        {
            _onFinished?.Invoke();
        }
        
        public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
        {
             // Handle error
        }
    }
}
#endif
