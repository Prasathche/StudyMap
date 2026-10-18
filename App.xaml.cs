namespace StudyMap;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Start directly with the normal Shell after the native Android
        // splash screen. The custom SplashPage is no longer used.
        var shell = new AppShell();
        MainPage = shell;

        // MyCareerPage is the app's home/dashboard, so navigate to it as soon
        // as the Shell has been attached to the application window.
        Dispatcher.Dispatch(async () =>
        {
            await shell.GoToAsync(nameof(MyCareerPage));
        });
    }
}
