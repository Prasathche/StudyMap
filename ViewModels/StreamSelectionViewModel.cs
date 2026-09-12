using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using StudyMap.Models;
using StudyMap.Services;

namespace StudyMap.ViewModels;

public class StreamSelectionViewModel : INotifyPropertyChanged
{
    private readonly CareersService _careersService;
    private bool _isLoading;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<Models.Stream> Streams { get; } = [];

    public ICommand SelectStreamCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public StreamSelectionViewModel()
    {
        _careersService = new CareersService();
        SelectStreamCommand = new Command<string>(async (name) => await OnSelectStream(name));
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        IsLoading = true;
        try
        {
            await BuildStreamsWithCountsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task BuildStreamsWithCountsAsync()
    {
        var allCareers = await _careersService.GetAllCareersAsync();

        int scienceCount  = allCareers.Count(c => c.Category.Equals("Science",  StringComparison.OrdinalIgnoreCase));
        int commerceCount = allCareers.Count(c => c.Category.Equals("Commerce", StringComparison.OrdinalIgnoreCase));
        int artsCount     = allCareers.Count(c => c.Category.Equals("Arts",     StringComparison.OrdinalIgnoreCase));

        var scienceFeatured  = allCareers.Where(c => c.Category.Equals("Science",  StringComparison.OrdinalIgnoreCase) && c.IsPopular).Take(3).Select(c => c.Name).ToList();
        var commerceFeatured = allCareers.Where(c => c.Category.Equals("Commerce", StringComparison.OrdinalIgnoreCase) && c.IsPopular).Take(3).Select(c => c.Name).ToList();
        var artsFeatured     = allCareers.Where(c => c.Category.Equals("Arts",     StringComparison.OrdinalIgnoreCase) && c.IsPopular).Take(3).Select(c => c.Name).ToList();

        Streams.Clear();

                Streams.Add(new Models.Stream
        {
            Id                 = "science",
            Name               = "Science",
            Description        = "Explore technology, healthcare, engineering & research careers.",
            TagLine            = "For curious minds who love to experiment",
            Icon               = "🔬",
            AccentColor        = "#2979FF",
            GradientStartColor = "#E3F2FD",
            GradientEndColor   = "#BBDEFB",
            CareerCount        = scienceCount,
            FeaturedCareers    = scienceFeatured
        });

                Streams.Add(new Models.Stream
        {
            Id                 = "commerce",
            Name               = "Commerce",
            Description        = "Discover careers in business, finance, accounting & management.",
            TagLine            = "For leaders who understand markets & money",
            Icon               = "📈",
            AccentColor        = "#00897B",
            GradientStartColor = "#E8F5E9",
            GradientEndColor   = "#C8E6C9",
            CareerCount        = commerceCount,
            FeaturedCareers    = commerceFeatured
        });

                Streams.Add(new Models.Stream
        {
            Id                 = "arts",
            Name               = "Arts",
            Description        = "Uncover careers in design, law, media, social sciences & humanities.",
            TagLine            = "For creative souls who shape culture",
            Icon               = "🎨",
            AccentColor        = "#7B1FA2",
            GradientStartColor = "#F3E5F5",
            GradientEndColor   = "#E1BEE7",
            CareerCount        = artsCount,
            FeaturedCareers    = artsFeatured
        });
    }

    private async Task OnSelectStream(string streamName)
    {
        if (string.IsNullOrWhiteSpace(streamName))
            return;

        await Shell.Current.GoToAsync(
            $"{nameof(StudyMap.StreamCareerListPage)}?streamName={Uri.EscapeDataString(streamName)}");
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}