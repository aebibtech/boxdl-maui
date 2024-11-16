namespace BoxDownloaderMAUI;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		
		MainPage = new MainPage();
	}
	
	protected override Window CreateWindow(IActivationState? activationState)
	{
		var window = base.CreateWindow(activationState);
		UpdateWindow(window);

		return window;
	}

	void UpdateWindow(Window window)
	{
		window.MinimumWidth = 720;
		window.MinimumHeight = 560;

		window.Width = 720;
		window.Height = 560;
	}
}
