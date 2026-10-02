using StudyMap.Models;
using StudyMap.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace StudyMap.ViewModels;

public class StreamCareersViewModel : INotifyPropertyChanged
{
    // ── Dependencies ──────────────────────────────────────────────
    private readonly CareersService _careersService;
    private readonly FavoritesService _favoritesService;
    private readonly RecentlyViewedService _recentlyViewedService;

    // ── Backing fields ────────────────────────────────────────────
    private string _selectedStreamName = string.Empty;
    private string _selectedStreamIcon = string.Empty;
    private string _selectedStreamDescription = string.Empty;
    private string _selectedStreamAccentColor = "#2979FF";
    private string _selectedStreamGradientStart = "#E3F2FD";
    private string _selectedStreamGradientEnd = "#BBDEFB";
    private bool _isLoading;
    private string _searchQuery = string.Empty;
    private string _activeFilter = "All";
    private bool _hasResults;
    private List<Career> _streamCareersRaw = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Observable collections ────────────────────────────────────
    public ObservableCollection<Career> StreamCareers { get; } = [];
    public ObservableCollection<Career> FavoriteCareers { get; } = [];
    public ObservableCollection<Career> RecentlyViewedCareers { get; } = [];
    public ObservableCollection<string> FilterChips { get; } = ["All", "Popular", "Favorite", "High Salary"];

    // ── Commands ──────────────────────────────────────────────────
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand FilterCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand ViewCareerDetailCommand { get; }

    // ── Bindable properties ───────────────────────────────────────
    public string SelectedStreamName
    {
        get => _selectedStreamName;
        set { if (_selectedStreamName != value) { _selectedStreamName = value; OnPropertyChanged(); } }
    }

    public string SelectedStreamIcon
    {
        get => _selectedStreamIcon;
        set { if (_selectedStreamIcon != value) { _selectedStreamIcon = value; OnPropertyChanged(); } }
    }

    public string SelectedStreamDescription
    {
        get => _selectedStreamDescription;
        set { if (_selectedStreamDescription != value) { _selectedStreamDescription = value; OnPropertyChanged(); } }
    }

    public string SelectedStreamAccentColor
    {
        get => _selectedStreamAccentColor;
        set { if (_selectedStreamAccentColor != value) { _selectedStreamAccentColor = value; OnPropertyChanged(); } }
    }

    public string SelectedStreamGradientStart
    {
        get => _selectedStreamGradientStart;
        set { if (_selectedStreamGradientStart != value) { _selectedStreamGradientStart = value; OnPropertyChanged(); } }
    }

    public string SelectedStreamGradientEnd
    {
        get => _selectedStreamGradientEnd;
        set { if (_selectedStreamGradientEnd != value) { _selectedStreamGradientEnd = value; OnPropertyChanged(); } }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { if (_isLoading != value) { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLoading)); } }
    }

    public bool IsNotLoading => !_isLoading;

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }
    }

    public string ActiveFilter
    {
        get => _activeFilter;
        set
        {
            if (_activeFilter != value)
            {
                _activeFilter = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }
    }

    public bool HasResults
    {
        get => _hasResults;
        set { if (_hasResults != value) { _hasResults = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasNoResults)); } }
    }

    public bool HasNoResults => !_hasResults;

    public int CareerCount => StreamCareers.Count;

    // ── Constructor ───────────────────────────────────────────────
    public StreamCareersViewModel()
    {
        _careersService        = new CareersService();
        _favoritesService      = new FavoritesService();
        _recentlyViewedService = new RecentlyViewedService();

        ToggleFavoriteCommand    = new Command<Career>(OnToggleFavorite);
        FilterCommand            = new Command<string>(chip => ActiveFilter = chip ?? "All");
        SearchCommand            = new Command<string>(q => SearchQuery = q ?? string.Empty);
        ViewCareerDetailCommand  = new Command<Career>(async c => await OnViewCareerDetail(c));
    }

    // ── Public API ────────────────────────────────────────────────
    public async Task LoadCareersForStreamAsync(string streamName)
    {
        if (string.IsNullOrWhiteSpace(streamName)) return;

        try
        {
            IsLoading = true;
            ApplyStreamMetadata(streamName);

            var all = await _careersService.GetAllCareersAsync();

            _streamCareersRaw = all
                .Where(c => c.Category.Equals(streamName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Inject live favorite state
            foreach (var c in _streamCareersRaw)
                c.IsFavorite = _favoritesService.IsFavorite(c.Id);

            _activeFilter  = "All";
            _searchQuery   = string.Empty;
            OnPropertyChanged(nameof(ActiveFilter));
            OnPropertyChanged(nameof(SearchQuery));

            ApplyFilters();
            await LoadSideCareersAsync(all);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[StreamCareersViewModel] {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void RefreshFavoriteStates()
    {
        foreach (var c in _streamCareersRaw)
            c.IsFavorite = _favoritesService.IsFavorite(c.Id);
        ApplyFilters();
    }

    // ── Private helpers ───────────────────────────────────────────
    private void ApplyStreamMetadata(string streamName)
    {
        SelectedStreamName = streamName;
        switch (streamName.ToLowerInvariant())
        {
            case "science":
                SelectedStreamIcon         = "🔬";
                SelectedStreamDescription  = "Technology, Healthcare & Engineering";
                SelectedStreamAccentColor  = "#2979FF";
                SelectedStreamGradientStart = "#E3F2FD";
                SelectedStreamGradientEnd   = "#BBDEFB";
                break;
            case "commerce":
                SelectedStreamIcon         = "📈";
                SelectedStreamDescription  = "Business, Finance & Economics";
                SelectedStreamAccentColor  = "#00897B";
                SelectedStreamGradientStart = "#E8F5E9";
                SelectedStreamGradientEnd   = "#C8E6C9";
                break;
            case "arts":
                SelectedStreamIcon         = "🎨";
                SelectedStreamDescription  = "Design, Media & Social Sciences";
                SelectedStreamAccentColor  = "#7B1FA2";
                SelectedStreamGradientStart = "#F3E5F5";
                SelectedStreamGradientEnd   = "#E1BEE7";
                break;
        }
    }

    private void ApplyFilters()
    {
        var query = _searchQuery.Trim();

        var filtered = _streamCareersRaw
            .Where(c =>
            {
                // Text search
                if (!string.IsNullOrWhiteSpace(query))
                {
                    var hit = c.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                           || c.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
                           || c.KeySkills.Any(s => s.Contains(query, StringComparison.OrdinalIgnoreCase));
                    if (!hit) return false;
                }
                // Chip filter
                return _activeFilter switch
                {
                    "Popular"     => c.IsPopular,
                    "Favorite"    => c.IsFavorite,
                    "High Salary" => c.FutureDemand.Contains("Very High", StringComparison.OrdinalIgnoreCase),
                    _             => true
                };
            })
            .OrderByDescending(c => c.IsPopular)
            .ThenBy(c => c.Name)
            .ToList();

        StreamCareers.Clear();
        foreach (var c in filtered)
            StreamCareers.Add(c);

        HasResults = StreamCareers.Count > 0;
        OnPropertyChanged(nameof(CareerCount));
    }

    private async Task LoadSideCareersAsync(List<Career> all)
    {
        // Favorites for this stream
        var favIds = _favoritesService.GetAll();
        FavoriteCareers.Clear();
        foreach (var c in all.Where(c => favIds.Contains(c.Id)))
            FavoriteCareers.Add(c);

        // Recently viewed
        var recentIds = _recentlyViewedService.GetAll();
        RecentlyViewedCareers.Clear();
        foreach (var id in recentIds)
        {
            var c = all.FirstOrDefault(x => x.Id == id);
            if (c is not null)
                RecentlyViewedCareers.Add(c);
        }

        await Task.CompletedTask;
    }

    private void OnToggleFavorite(Career? career)
    {
        if (career is null) return;
        var isNowFav = _favoritesService.Toggle(career.Id);
        career.IsFavorite = isNowFav;

        // Refresh chips that depend on favorite state
        if (_activeFilter == "Favorite") ApplyFilters();
    }

    private async Task OnViewCareerDetail(Career? career)
    {
        if (career is null) return;
        _recentlyViewedService.Record(career.Id);
        await Shell.Current.GoToAsync(
            $"{nameof(StudyMap.CareerDetailPage)}?careerId={Uri.EscapeDataString(career.Id)}");
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}