namespace StudyMap;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		// Show the full-screen branded StudyMap image immediately after the
		// Android system launch splash, then transition to the normal app shell.
		MainPage = new SplashPage();
	}
}
