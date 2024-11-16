using BoxDownloaderMAUI.Services;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;

namespace BoxDownloaderMAUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var up = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		var path = Path.Combine(up, ".boxdlmaui");
		var browserFetcher = new BrowserFetcher(){ CacheDir = path };
		browserFetcher.DownloadAsync().GetAwaiter().GetResult();
		
		var browser = (Puppeteer.LaunchAsync(new LaunchOptions()
		{
			ExecutablePath = browserFetcher.GetInstalledBrowsers().First().GetExecutablePath(),
			Headless = true,
			Timeout = 0
		})).GetAwaiter().GetResult();
		
		builder.Services.AddSingleton(browser);
		builder.Services.AddSingleton<HttpClient>();
		builder.Services.AddSingleton<DownloadService>();
		
		return builder.Build();
	}
}
