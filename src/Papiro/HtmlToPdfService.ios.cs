#if IOS
using System.Collections.Concurrent;
using Foundation;
using UIKit;
using WebKit;
using CoreGraphics;

namespace CwSoftware.Papiro;

public partial class HtmlToPdfService
{
    // CRITICAL: WKWebView.NavigationDelegate é uma propriedade "weak" no WebKit nativo — ela NÃO
    // retém o delegate. Se webView/delegateHandler ficassem só como variáveis locais da lambda
    // (fire-and-forget via MainThread.BeginInvokeOnMainThread), o GC do .NET podia coletar os dois
    // antes do DidFinishNavigation disparar (principalmente em relatórios grandes, que demoram mais
    // para carregar e dão mais chance do GC rodar no meio do caminho). O resultado era um spinner
    // infinito seguido do timeout de 80s do HtmlToPdfService, com o WKWebView "zumbi" ainda vivo em
    // memória. Por isso guardamos referências fortes por conversão (não como campo único de
    // instância — o serviço é registrado como singleton via UsePapiro(), então duas conversões
    // concorrentes usando um único campo se pisariam) enquanto ela está em andamento, liberando só
    // depois que o TaskCompletionSource correspondente é resolvido.
    private static readonly ConcurrentDictionary<Guid, (WKWebView WebView, WebViewNavigationDelegate Delegate)> _activeConversions = new();

    private partial async Task<HtmlToPdfResult> ConvertVal(string html, string filePath, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<HtmlToPdfResult>();
        var conversionId = Guid.NewGuid();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    tcs.TrySetResult(HtmlToPdfResult.Failure("Conversion cancelled."));
                    return;
                }

                var webView = new WKWebView(new CGRect(0, 0, 595, 842), new WKWebViewConfiguration());

                // Anexa a WKWebView (fora da tela) à janela principal: sem estar numa hierarquia de
                // views real, alguns recursos de layout/CSS (webfonts, media queries, etc.) podem não
                // renderizar corretamente, mesmo com a navegação "terminando" com sucesso.
                var keyWindow = UIApplication.SharedApplication.ConnectedScenes
                    .OfType<UIWindowScene>()
                    .SelectMany(scene => scene.Windows)
                    .FirstOrDefault(w => w.IsKeyWindow)
                    ?? UIApplication.SharedApplication.Windows.FirstOrDefault();

                webView.Hidden = true;
                keyWindow?.AddSubview(webView);

                var delegateHandler = new WebViewNavigationDelegate(
                    onFinished: () => FinalizarConversao(conversionId, webView, filePath, tcs),
                    onFailed: message => Finalizar(conversionId, HtmlToPdfResult.Failure(message), webView, tcs));

                // Retém fortemente até a conversão terminar (sucesso, falha ou cancelamento) — ver
                // comentário no topo da classe sobre a propriedade "weak" NavigationDelegate.
                _activeConversions[conversionId] = (webView, delegateHandler);

                // Se o timeout do HtmlToPdfService disparar antes da navegação terminar, aborta a
                // WebView imediatamente em vez de deixá-la "zumbi" rodando em segundo plano.
                cancellationToken.Register(() =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        webView.StopLoading();
                        Finalizar(conversionId, HtmlToPdfResult.Failure("Conversion cancelled (timeout)."), webView, tcs);
                    });
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

    private void FinalizarConversao(Guid conversionId, WKWebView webView, string filePath, TaskCompletionSource<HtmlToPdfResult> tcs)
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

            var pages = renderer.NumberOfPages;
            var bounds = UIGraphics.PDFContextBounds;

            for (int i = 0; i < pages; i++)
            {
                UIGraphics.BeginPDFPage();
                renderer.DrawPage(i, bounds);
            }

            UIGraphics.EndPDFContext();

            data.Save(NSUrl.FromFilename(filePath), true);
            Finalizar(conversionId, HtmlToPdfResult.Success(filePath), webView, tcs);
        }
        catch (Exception ex)
        {
            Finalizar(conversionId, HtmlToPdfResult.Failure(ex.Message), webView, tcs);
        }
    }

    private static void Finalizar(Guid conversionId, HtmlToPdfResult result, WKWebView webView, TaskCompletionSource<HtmlToPdfResult> tcs)
    {
        // Libera as referências fortes e remove a WKWebView da hierarquia para não vazar memória
        // (WebKit é pesado; reter WKWebViews mortas entre gerações de relatório causa pressão de
        // memória em relatórios sucessivos). TrySetResult porque cancelamento e término normal
        // podem, em teoria, correr em paralelo — só o primeiro deve valer.
        if (!tcs.TrySetResult(result))
            return;

        webView.NavigationDelegate = null;
        webView.RemoveFromSuperview();
        _activeConversions.TryRemove(conversionId, out _);
    }

    private class WebViewNavigationDelegate : WKNavigationDelegate
    {
        private readonly Action _onFinished;
        private readonly Action<string> _onFailed;

        public WebViewNavigationDelegate(Action onFinished, Action<string> onFailed)
        {
            _onFinished = onFinished;
            _onFailed = onFailed;
        }

        public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
        {
            _onFinished();
        }

        public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
        {
            _onFailed($"WKWebView falhou ao renderizar o HTML: {error.LocalizedDescription}");
        }

        public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error)
        {
            _onFailed($"WKWebView falhou ao carregar o HTML: {error.LocalizedDescription}");
        }
    }
}
#endif
