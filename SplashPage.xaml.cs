namespace StudyMap;

public partial class SplashPage : ContentPage
{
    private bool _started;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_started)
            return;

        _started = true;

        // Keep the branded launch screen visible briefly, then hand control
        // to the normal Shell-based app without animation or loading UI.
        await Task.Delay(900);

        if (Application.Current is null)
            return;

        var shell = new AppShell();
        Application.Current.MainPage = shell;

        await shell.GoToAsync(nameof(MyCareerPage));
    }
}
