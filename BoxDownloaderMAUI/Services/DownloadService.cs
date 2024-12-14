using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BoxDownloaderMAUI.Helpers;
using BoxDownloaderMAUI.Models;
using PuppeteerSharp;
using IBrowser = PuppeteerSharp.IBrowser;

namespace BoxDownloaderMAUI.Services;

public class DownloadService(HttpClient client, DownloaderSettings settings)
{
    private IBrowser browser;
    
    public async Task<int> DownloadBoxLinksAsync(string[] urls)
    {
        using Ping pinger = new();
        PingReply reply = await pinger.SendPingAsync("google.com");

        if (reply is not { Status: IPStatus.Success })
        {
            return 0;
        }

        if (urls == null && urls.Length == 0)
            return 0;

        browser = await Puppeteer.LaunchAsync(new LaunchOptions()
        {
            ExecutablePath = settings.BrowserPath,
            Headless = true,
            Timeout = 0
        });
        
        // List<(string, string)> downloadLinks = [];
        // foreach (string url in urls)
        // {
        //     (string, string) downloadLink = await ScrapeBoxDocumentAsync(url);
        //     downloadLinks.Add(downloadLink);
        //     await Task.Delay(500);
        // }
        
        // Task[] downloadFileTasks = downloadLinks
        //     .Where(x => x != (string.Empty, string.Empty))
        //     .Select(t => DownloadFileAsync(t.Item2, t.Item1))
        //     .ToArray();
        // await Task.WhenAll(downloadFileTasks);

        await foreach (var item in ScrapeBoxDocumentsAsync(urls))
        {
            await DownloadFileAsync(item.Item2, item.Item1);
        }
        
        await browser.CloseAsync();
        
        return 1;
    }

    private async IAsyncEnumerable<(string, string)> ScrapeBoxDocumentsAsync(IEnumerable<string> urls)
    {
        foreach (string url in urls)
        {
            (string title, string link) = await ScrapeBoxDocumentAsync(url);
            yield return (title, link);
        }
    }

    private async Task<(string, string)> ScrapeBoxDocumentAsync(string url)
    {
        try
        {
            var page = await browser.NewPageAsync();
            await page.GoToAsync(url, new NavigationOptions()
            {
                Timeout = 0,
                WaitUntil = [WaitUntilNavigation.Networkidle0]
            });

            string? title = await page.GetTitleAsync();
            title = title.Split('|').First().Trim();
            
            JsonElement? networkRequestsStr = await page.EvaluateExpressionAsync("JSON.stringify(window.performance.getEntries())");
            List<JsonElement> networkRequests = JsonSerializer.Deserialize<List<JsonElement>>(networkRequestsStr.GetValueOrDefault().GetString());
            JsonElement? target = networkRequests.Find(x =>
            {
                var name = x.GetProperty("name").GetString();
                bool isPreview = (name.Contains("public.boxcloud.com/api/2.0/files") || name.Contains("dl.boxcloud.com/api/2.0/files")) && name.Contains("content?preview=true");
                bool isInternalFiles = name.Contains("internal_files") && name.Contains("pdf");
                return isPreview || isInternalFiles;
            });
            string downloadLink = target?.GetProperty("name").GetString() ?? string.Empty;
            return (title, downloadLink);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return (string.Empty, string.Empty);
        }
    }

    private async Task DownloadFileAsync(string downloadLink, string title)
    {
        string destinationPath = Path.Combine(settings.DownloadPath, $"{title}-{DateTime.Now.Ticks}.pdf");
        
        client.DefaultRequestHeaders.AcceptEncoding.Clear();
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));

        // NOTE: accept all languages
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("*"));

        // NOTE: accept all media types
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

        HttpResponseMessage response = await client.GetAsync(downloadLink);
        using MemoryStream ms = new();
        await response.Content.CopyToAsync(ms);
        byte[] fileBytes = await Decompressor.GZip(ms);
        await File.WriteAllBytesAsync(destinationPath, fileBytes);
    }
}