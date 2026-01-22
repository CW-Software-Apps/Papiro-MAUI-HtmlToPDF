#if ANDROID
using Android.Content;
using Android.Graphics.Pdf;
using Android.OS;
using Android.Webkit;
using Android.Views;
using System.IO;
using File = System.IO.File;
using Microsoft.Maui.Platform;
using AndroidWebView = Android.Webkit.WebView;
using AndroidView = Android.Views.View;
using AndroidGraphics = Android.Graphics;

namespace CwSoftware.Papiro;

public partial class HtmlToPdfService
{
    // A4 dimensions at 72 DPI (standard PDF points)
    private const int A4_WIDTH_POINTS = 595;
    private const int A4_HEIGHT_POINTS = 842;
    
    // Scale factor for high quality rendering (300 DPI / 72 DPI ≈ 4.17)
    // Using 3x for balance between quality and performance
    private const int SCALE_FACTOR = 3;
    
    private const int PAGE_WIDTH = A4_WIDTH_POINTS * SCALE_FACTOR;  // ~1785
    private const int PAGE_HEIGHT = A4_HEIGHT_POINTS * SCALE_FACTOR; // ~2526

    private partial async Task<HtmlToPdfResult> ConvertVal(string html, string filePath)
    {
        var tcs = new TaskCompletionSource<HtmlToPdfResult>();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var context = Platform.CurrentActivity ?? Platform.AppContext;
                var webView = new AndroidWebView(context);
                
                webView.Settings.JavaScriptEnabled = true;
                webView.Settings.DomStorageEnabled = true;
                webView.Settings.LoadWithOverviewMode = true;
                webView.Settings.UseWideViewPort = true;
                
                // Set initial layout to A4 width (we'll measure height after content loads)
                webView.Layout(0, 0, PAGE_WIDTH, PAGE_HEIGHT);
                webView.Measure(
                    AndroidView.MeasureSpec.MakeMeasureSpec(PAGE_WIDTH, MeasureSpecMode.Exactly), 
                    AndroidView.MeasureSpec.MakeMeasureSpec(PAGE_HEIGHT, MeasureSpecMode.AtMost));

                webView.SetWebViewClient(new PdfWebViewClient(async (view) =>
                {
                    try
                    {
                        // Wait for rendering to complete
                        await Task.Delay(800);

                        // Get actual content height from WebView
                        // Scale is deprecated, using Density as approximation or 1.0 if not available
                        float scale = view.Context?.Resources?.DisplayMetrics?.Density ?? 1.0f;
                        int contentWidth = PAGE_WIDTH;
                        int contentHeight = (int)(view.ContentHeight * scale);
                        
                        if (contentHeight <= 0)
                        {
                            contentHeight = PAGE_HEIGHT; // Fallback to single page
                        }

                        // Relayout WebView to full content height for accurate drawing
                        view.Measure(
                            AndroidView.MeasureSpec.MakeMeasureSpec(contentWidth, MeasureSpecMode.Exactly),
                            AndroidView.MeasureSpec.MakeMeasureSpec(contentHeight, MeasureSpecMode.Exactly));
                        view.Layout(0, 0, contentWidth, contentHeight);
                        
                        await Task.Delay(200); // Allow relayout to settle

                        // Calculate number of pages needed with tolerance
                        // If content is within 2% of a page boundary, don't create an extra blank page
                        double exactPages = (double)contentHeight / PAGE_HEIGHT;
                        double remainder = exactPages - Math.Floor(exactPages);
                        int pageCount;
                        
                        if (remainder < 0.02) // Less than 2% overhang = round down
                        {
                            pageCount = (int)Math.Floor(exactPages);
                        }
                        else
                        {
                            pageCount = (int)Math.Ceiling(exactPages);
                        }
                        
                        if (pageCount < 1) pageCount = 1;


                        var pdfDocument = new PdfDocument();

                        for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                        {
                            var pageInfo = new PdfDocument.PageInfo.Builder(PAGE_WIDTH, PAGE_HEIGHT, pageIndex + 1).Create();
                            var page = pdfDocument.StartPage(pageInfo);
                            if (page == null) continue;

                            // Calculate vertical offset for this page
                            int yOffset = pageIndex * PAGE_HEIGHT;

                            // Translate canvas to show correct portion of content
                            var canvas = page.Canvas;
                            if (canvas != null)
                            {
                                canvas.Save();
                                canvas.Translate(0, -yOffset);
                                
                                // Draw the entire WebView (translated canvas will clip to visible portion)
                                view.Draw(canvas);
                                
                                canvas.Restore();
                            }
                            pdfDocument.FinishPage(page);
                        }

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            pdfDocument.WriteTo(stream);
                        }

                        pdfDocument.Close();
                        tcs.TrySetResult(HtmlToPdfResult.Success(filePath));
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetResult(HtmlToPdfResult.Failure(ex.Message));
                    }
                }, 
                (errorMsg) => 
                {
                    tcs.TrySetResult(HtmlToPdfResult.Failure(errorMsg));
                }));

                webView.LoadDataWithBaseURL(null, html, "text/html", "utf-8", null);
            }
            catch (Exception ex)
            {
                tcs.TrySetResult(HtmlToPdfResult.Failure(ex.Message));
            }
        });

        return await tcs.Task;
    }

    private class PdfWebViewClient : Android.Webkit.WebViewClient
    {
        private readonly Action<AndroidWebView> _onPageFinished;
        private readonly Action<string> _onError;

        public PdfWebViewClient(Action<AndroidWebView> onPageFinished, Action<string> onError) 
        {
            _onPageFinished = onPageFinished;
            _onError = onError;
        }

        public override void OnPageFinished(AndroidWebView? view, string? url)
        {
            base.OnPageFinished(view, url);
            if (view != null) 
            {
                _onPageFinished(view);
            }
        }

        public override void OnReceivedError(AndroidWebView? view, IWebResourceRequest? request, WebResourceError? error)
        {
            base.OnReceivedError(view, request, error);
            // Only fail if it's the main page usage
            if (request?.IsForMainFrame == true)
            {
                _onError?.Invoke($"WebView Error: {error?.Description} (Code: {error?.ErrorCode})");
            }
        }
    }
}
#endif
