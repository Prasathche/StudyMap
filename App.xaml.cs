namespace StudyMap;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		// Show the responsive branded launch screen first. The Android system
		// splash remains intentionally minimal; SplashPage provides the full
		// Study Map branding before the normal Shell starts.
		MainPage = new SplashPage();
	}
}
