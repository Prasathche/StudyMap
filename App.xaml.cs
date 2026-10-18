namespace StudyMap;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        // Start directly with the normal Shell after the native Android
        // splash screen. Career Explorer is the app's home page.
        MainPage = new AppShell();
    }
}
