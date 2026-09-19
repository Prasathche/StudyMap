using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using StudyMap.Models;
using StudyMap.Services;

namespace StudyMap.ViewModels;

/// <summary>
/// ViewModel for the Career Details screen.
/// Supports save/favorite toggling, roadmap navigation, and related career display.
/// </summary>
public class CareerDetailsViewModel : INotifyPropertyChanged
{
    // ── Dependencies ────────────────────────────────────────────────
    private readonly CareersService _careersService;
    private readonly FavoritesService _favoritesService;
    private readonly RecentlyViewedService _recentlyViewedService;

    // ── Backing fields ───────────────────────────────────────────────
    private Career? _career;
    private bool _isLoading;
    private bool _isSaved;
    private bool _isFavorite;
    private string _saveButtonText = "Save Career";
    private string _favoriteButtonText = "Add to Favorites";
    private string _statusMessage = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    // ── Collections ──────────────────────────────────────────────────
    public ObservableCollection<string> EntranceExams { get; } = [];
    public ObservableCollection<string> KeySkills { get; } = [];
    public ObservableCollection<string> RoadmapSteps { get; } = [];
    public ObservableCollection<string> SalaryInsights { get; } = [];
    public ObservableCollection<string> TopColleges { get; } = [];
    public ObservableCollection<string> Workplaces { get; } = [];
    public ObservableCollection<string> RecommendedSubjects { get; } = [];
    public ObservableCollection<string> RelatedCareers { get; } = [];
    public ObservableCollection<Career> RelatedCareerObjects { get; } = [];

    // ── Commands ─────────────────────────────────────────────────────
    public ICommand ViewRoadmapCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand SaveCareerCommand { get; }
    public ICommand NavigateToRelatedCareerCommand { get; }

    // ── Bindable properties ──────────────────────────────────────────
    public Career? Career
    {
        get => _career;
        private set
        {
            _career = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(Category));
            OnPropertyChanged(nameof(Icon));
            OnPropertyChanged(nameof(SalaryRangeIndia));
            OnPropertyChanged(nameof(FutureDemand));
            OnPropertyChanged(nameof(SalaryDisplay));
            OnPropertyChanged(nameof(DemandDisplay));
            OnPropertyChanged(nameof(StreamDisplay));
            OnPropertyChanged(nameof(EducationPath));
            OnPropertyChanged(nameof(AccentColor));
            OnPropertyChanged(nameof(Duration));
            OnPropertyChanged(nameof(FutureScope));
        }
    }

    public string SalaryDisplay =>
        !string.IsNullOrWhiteSpace(_career?.SalaryRangeIndia)
            ? _career.SalaryRangeIndia
            : ExtractSalaryFromInsights(_career);

    public string DemandDisplay =>
        !string.IsNullOrWhiteSpace(_career?.FutureDemand)
            ? _career.FutureDemand
            : ExtractDemandFromInsights(_career);

    public string StreamDisplay =>
        !string.IsNullOrWhiteSpace(_career?.Category)
            ? _career.Category
            : "Not specified";

    private static string ExtractSalaryFromInsights(Career? career)
    {
        var insight = career?.SalaryInsights?.FirstOrDefault(x =>
            x.Contains("INR", StringComparison.OrdinalIgnoreCase) &&
            x.Contains("LPA", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(insight))
            return "Not specified";

        var start = insight.IndexOf("INR", StringComparison.OrdinalIgnoreCase);
        var end = insight.IndexOf("LPA", start, StringComparison.OrdinalIgnoreCase);

        if (start < 0 || end <= start)
            return "Not specified";

        var value = insight[start..(end + 3)];
        var comma = value.IndexOf(',');
        return (comma > 0 ? value[..comma] : value).Trim();
    }

    private static string ExtractDemandFromInsights(Career? career)
    {
        var insight = career?.SalaryInsights?.LastOrDefault(x =>
            x.Contains("Demand", StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(insight))
            return "Not specified";

        const string prefix = "Demand is ";
        var start = insight.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            return insight.Trim();

        start += prefix.Length;
        var end = insight.IndexOf(" across", start, StringComparison.OrdinalIgnoreCase);
        if (end < 0)
            end = insight.Length;

        return insight[start..end].TrimEnd('.', ' ');
    }

    public string Name => _career?.Name ?? string.Empty;
    public string Description => _career?.Description ?? string.Empty;
    public string Category => _career?.Category ?? string.Empty;
    public string Icon => _career?.Icon ?? "🧭";
    public string SalaryRangeIndia => _career?.SalaryRangeIndia ?? string.Empty;
    public string FutureDemand => _career?.FutureDemand ?? string.Empty;
    public string EducationPath => _career?.EducationPath ?? string.Empty;
    public string AccentColor => _career?.AccentColor ?? "#F6A64F";
    public string Duration => _career?.Duration ?? string.Empty;
    public string FutureScope => _career?.FutureScope ?? string.Empty;

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotLoading));
        }
    }

    public bool IsNotLoading => !_isLoading;

    public bool IsSaved
    {
        get => _isSaved;
        private set
        {
            if (_isSaved == value) return;
            _isSaved = value;
            SaveButtonText = value ? "✓ Saved!" : "Save Career";
            OnPropertyChanged();
        }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        private set
        {
            if (_isFavorite == value) return;
            _isFavorite = value;
            FavoriteButtonText = value ? "❤ Remove Favorite" : "Add to Favorites";
            OnPropertyChanged();
        }
    }

    public string SaveButtonText
    {
        get => _saveButtonText;
        private set { if (_saveButtonText != value) { _saveButtonText = value; OnPropertyChanged(); } }
    }

    public string FavoriteButtonText
    {
        get => _favoriteButtonText;
        private set { if (_favoriteButtonText != value) { _favoriteButtonText = value; OnPropertyChanged(); } }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } }
    }

    // ── Constructor ──────────────────────────────────────────────────
    public CareerDetailsViewModel()
    {
        _careersService        = new CareersService();
        _favoritesService      = new FavoritesService();
        _recentlyViewedService = new RecentlyViewedService();

        ViewRoadmapCommand             = new Command(async () => await OnViewRoadmap());
        ToggleFavoriteCommand          = new Command(OnToggleFavorite);
        SaveCareerCommand              = new Command(OnSaveCareer);
        NavigateToRelatedCareerCommand = new Command<string>(async id => await OnNavigateToRelatedCareer(id));
    }

    // ── Public API ───────────────────────────────────────────────────
    public async Task LoadCareerAsync(string careerId)
    {
        if (string.IsNullOrWhiteSpace(careerId)) return;

        try
        {
            IsLoading = true;
            var career = await _careersService.GetCareerByIdAsync(careerId);
            if (career is null) return;

            // Record as recently viewed
            _recentlyViewedService.Record(careerId);

           _career = career;
Career = career;

IsFavorite = _favoritesService.IsFavorite(careerId);

            // Populate observable lists
            Reset(EntranceExams,        career.EntranceExams);
            Reset(KeySkills,            career.KeySkills);
            Reset(RoadmapSteps,         career.RoadmapSteps);
            Reset(SalaryInsights,       career.SalaryInsights);
            Reset(TopColleges,          career.TopColleges);
            Reset(Workplaces,           career.Workplaces);
            Reset(RecommendedSubjects,  career.RecommendedSubjects);
            Reset(RelatedCareers,       career.RelatedCareers);

            await LoadRelatedCareerObjectsAsync(career);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CareerDetailsViewModel] {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Command handlers ─────────────────────────────────────────────
    private async Task OnViewRoadmap()
    {
        if (_career is null) return;
        await Shell.Current.GoToAsync(
            $"{nameof(StudyMap.CareerRoadmapPage)}");
    }

    private void OnToggleFavorite()
{
    if (_career is null)
        return;

    var isNow = _favoritesService.Toggle(_career.Id);

    IsFavorite = isNow;

    OnPropertyChanged(nameof(IsFavorite));
    OnPropertyChanged(nameof(FavoriteButtonText));

    ShowStatus(
        isNow
            ? "❤ Added to Favorites"
            : "♡ Removed from Favorites");
}

    private void OnSaveCareer()
    {
        if (_career is null) return;
        IsSaved = true;
        ShowStatus("✓ Career saved successfully!");

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(2500);
            IsSaved = false;
            StatusMessage = string.Empty;
        });
    }

    private async Task OnNavigateToRelatedCareer(string? careerId)
    {
        if (string.IsNullOrWhiteSpace(careerId)) return;
        await Shell.Current.GoToAsync(
            $"{nameof(StudyMap.CareerDetailPage)}?careerId={Uri.EscapeDataString(careerId)}");
    }

    // ── Private helpers ──────────────────────────────────────────────
    private async Task LoadRelatedCareerObjectsAsync(Career source)
    {
        RelatedCareerObjects.Clear();
        if (source.RelatedCareers.Count == 0)
        {
            // Auto-generate related from same category
            var all = await _careersService.GetCareersByCategoryAsync(source.Category);
            var related = all
                .Where(c => c.Id != source.Id)
                .Take(4)
                .ToList();
            foreach (var c in related)
                RelatedCareerObjects.Add(c);
        }
        else
        {
            foreach (var name in source.RelatedCareers.Take(4))
            {
                var all = await _careersService.GetAllCareersAsync();
                var c = all.FirstOrDefault(x =>
                    x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (c is not null) RelatedCareerObjects.Add(c);
            }
        }
    }

    private void ShowStatus(string message)
    {
        StatusMessage = message;
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(2000);
            if (StatusMessage == message)
                StatusMessage = string.Empty;
        });
    }

    private static void Reset(ObservableCollection<string> col, IEnumerable<string> items)
    {
        col.Clear();
        foreach (var i in items)
            if (!string.IsNullOrWhiteSpace(i))
                col.Add(i);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
