using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using PuppeteerSharp.Media;
using Zenvoyce.Application.Abstractions.Services;

namespace Zenvoyce.Infrastructure.Services;

/// <summary>
/// Render HTML+CSS thành PDF bằng PuppeteerSharp (headless Chromium).
/// Khởi tạo browser một lần và tái sử dụng giữa các request.
/// </summary>
public sealed class PuppeteerPdfRenderer : IInvoicePdfRenderer, IAsyncDisposable
{
    private readonly ILogger<PuppeteerPdfRenderer> _logger;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private IBrowser? _browser;
    private bool _disposed;

    public PuppeteerPdfRenderer(ILogger<PuppeteerPdfRenderer> logger)
    {
        _logger = logger;
    }

    public async Task<byte[]> RenderPdfAsync(string html, string? css, CancellationToken cancellationToken)
    {
        var browser = await GetBrowserAsync(cancellationToken).ConfigureAwait(false);

        await using var page = await browser.NewPageAsync().ConfigureAwait(false);

        var fullHtml = BuildFullHtml(html, css);
        await page.SetContentAsync(fullHtml, new NavigationOptions
        {
            WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
            Timeout = 30000
        }).ConfigureAwait(false);

        var pdfBytes = await page.PdfDataAsync(new PdfOptions
        {
            Format = PaperFormat.A4,
            PrintBackground = true,
            MarginOptions = new MarginOptions
            {
                Top = "12mm",
                Bottom = "12mm",
                Left = "10mm",
                Right = "10mm"
            }
        }).ConfigureAwait(false);

        return pdfBytes;
    }

    private async Task<IBrowser> GetBrowserAsync(CancellationToken cancellationToken)
    {
        if (_browser is { IsClosed: false })
        {
            return _browser;
        }

        await _initLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_browser is { IsClosed: false })
            {
                return _browser;
            }

            _logger.LogInformation("Khởi tạo headless Chromium cho PDF rendering...");
            var executablePath = ResolveBrowserExecutablePath();
            if (string.IsNullOrWhiteSpace(executablePath))
            {
                var browserFetcher = new BrowserFetcher();
                await browserFetcher.DownloadAsync().ConfigureAwait(false);
            }
            else
            {
                _logger.LogInformation("Sử dụng trình duyệt hệ thống: {ExecutablePath}", executablePath);
            }

            _browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                ExecutablePath = executablePath,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--disable-dev-shm-usage" }
            }).ConfigureAwait(false);

            return _browser;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static string BuildFullHtml(string body, string? css)
    {
        var styleTag = string.IsNullOrWhiteSpace(css) ? string.Empty : $"<style>{css}</style>";
        return $"""
        <!DOCTYPE html>
        <html lang="vi">
        <head>
        <meta charset="utf-8" />
        <title>Invoice</title>
        {styleTag}
        </head>
        <body>
        {body}
        </body>
        </html>
        """;
    }

    private static string? ResolveBrowserExecutablePath()
    {
        var envPath = Environment.GetEnvironmentVariable("PUPPETEER_EXECUTABLE_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        var candidates = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? new[]
            {
                "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
                "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
                "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
                "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe"
            }
            : new[]
            {
                "/usr/bin/microsoft-edge",
                "/usr/bin/google-chrome",
                "/usr/bin/chromium",
                "/usr/bin/chromium-browser",
                "/opt/google/chrome/chrome"
            };

        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_browser is { IsClosed: false })
        {
            await _browser.CloseAsync().ConfigureAwait(false);
            await _browser.DisposeAsync().ConfigureAwait(false);
        }

        _initLock.Dispose();
    }
}
