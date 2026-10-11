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
        // to the normal Shell-based app without any animation or loading UI.
        await Task.Delay(900);

        if (Application.Current is null)
            return;

        Application.Current.MainPage = new AppShell();

        await Shell.Current.GoToAsync(nameof(MyCareerPage));
    }
}
