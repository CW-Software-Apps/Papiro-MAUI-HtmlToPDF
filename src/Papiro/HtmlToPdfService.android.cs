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
    
    // Scale factor: 2x gives ~144 DPI — good quality, significantly faster for large reports
    private const int SCALE_FACTOR = 2;
    
    private const int PAGE_WIDTH = A4_WIDTH_POINTS * SCALE_FACTOR;  // 1190
    private const int PAGE_HEIGHT = A4_HEIGHT_POINTS * SCALE_FACTOR; // 1684

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
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        try
                        {
                            // Wait for JS/CSS rendering to complete
                            await Task.Delay(500);

                            float scale = view.Context?.Resources?.DisplayMetrics?.Density ?? 1.0f;
                            int contentWidth = PAGE_WIDTH;
                            int contentHeight = (int)(view.ContentHeight * scale);

                            if (contentHeight <= 0)
                                contentHeight = PAGE_HEIGHT;

                            // Relayout WebView to full content height for accurate drawing
                            view.Measure(
                                AndroidView.MeasureSpec.MakeMeasureSpec(contentWidth, MeasureSpecMode.Exactly),
                                AndroidView.MeasureSpec.MakeMeasureSpec(contentHeight, MeasureSpecMode.Exactly));
                            view.Layout(0, 0, contentWidth, contentHeight);

                            await Task.Delay(150); // Allow relayout to settle

                            double exactPages = (double)contentHeight / PAGE_HEIGHT;
                            double remainder = exactPages - Math.Floor(exactPages);
                            int pageCount = remainder < 0.02
                                ? (int)Math.Floor(exactPages)
                                : (int)Math.Ceiling(exactPages);
                            if (pageCount < 1) pageCount = 1;

                            // Render WebView to a single full-height bitmap, then slice into pages.
                            // This avoids calling view.Draw() once per page (O(n²) for large reports).
                            var fullBitmap = Android.Graphics.Bitmap.CreateBitmap(contentWidth, contentHeight, Android.Graphics.Bitmap.Config.Rgb565!);
                            var fullCanvas = new Android.Graphics.Canvas(fullBitmap!);
                            view.Draw(fullCanvas);

                            var pdfDocument = new PdfDocument();

                            for (int pageIndex = 0; pageIndex < pageCount; pageIndex++)
                            {
                                var pageInfo = new PdfDocument.PageInfo.Builder(PAGE_WIDTH, PAGE_HEIGHT, pageIndex + 1).Create();
                                var page = pdfDocument.StartPage(pageInfo);
                                if (page == null) continue;

                                int yOffset = pageIndex * PAGE_HEIGHT;
                                int sliceHeight = Math.Min(PAGE_HEIGHT, contentHeight - yOffset);

                                var canvas = page.Canvas;
                                if (canvas != null && sliceHeight > 0)
                                {
                                    // Draw only the slice of the full bitmap for this page
                                    var srcRect = new Android.Graphics.Rect(0, yOffset, contentWidth, yOffset + sliceHeight);
                                    var dstRect = new Android.Graphics.RectF(0, 0, PAGE_WIDTH, sliceHeight);
                                    canvas.DrawBitmap(fullBitmap!, srcRect, dstRect, null);
                                }
                                pdfDocument.FinishPage(page);
                            }

                            fullBitmap?.Recycle();

                            using (var stream = new FileStream(filePath, FileMode.Create))
                            {
                                pdfDocument.WriteTo(stream);
                            }

                            pdfDocument.Close();

                            // ✅ CRITICAL FIX: Destroy WebView to free native resources (Chromium)
                            // This prevents OOM and signal crashes on repeat usage
                            try
                            {
                                view.Destroy();
                            }
                            catch (Exception destroyEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error destroying WebView: {destroyEx.Message}");
                            }

                            tcs.TrySetResult(HtmlToPdfResult.Success(filePath));
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"PDF Generation Error: {ex.Message}");
                            tcs.TrySetResult(HtmlToPdfResult.Failure(ex.Message));
                        }
                    });
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
