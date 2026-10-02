using System.Collections.ObjectModel;
using System.Text.Json;
using StudyMap.Models;

namespace StudyMap;

public partial class CareerExplorerPage : ContentPage
{
	private readonly List<Career> _allCareers = [];
	private bool _isLoaded;
	private string _searchText = string.Empty;
	private string _selectedCategory = "All";

	public ObservableCollection<Career> FilteredCareers { get; } = [];

	public ObservableCollection<Career> PopularCareers { get; } = [];

	public CareerExplorerPage()
	{
		InitializeComponent();
		BindingContext = this;
		ApplyCategoryStyles();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (_isLoaded)
		{
			return;
		}

		_isLoaded = true;
		await LoadCareersAsync();
	}

	private async Task LoadCareersAsync()
	{
		try
		{
			await using var stream = await FileSystem.OpenAppPackageFileAsync("careers.json");
			var careers = await JsonSerializer.DeserializeAsync<List<Career>>(
				stream,
				new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				});

			_allCareers.Clear();
			_allCareers.AddRange(careers ?? []);
			ApplyFilters();
		}
		catch (Exception)
		{
			await DisplayAlert("Career Explorer", "Unable to load careers right now.", "OK");
		}
	}

	private void OnSearchCompleted(object? sender, EventArgs e)
	{
		_searchText = SearchEntry?.Text?.Trim() ?? string.Empty;
		ApplyFilters();
	}

	private void OnCategoryClicked(object? sender, EventArgs e)
	{
		if (sender is not Button button)
		{
			return;
		}

		_selectedCategory = button.Text;
		ApplyCategoryStyles();
		ApplyFilters();
	}
private async void OnCareerTapped(object? sender, TappedEventArgs e)
{
    if (e.Parameter is not Career career)
    {
        return;
    }

    var route =
        $"{nameof(CareerDetailPage)}" +
        $"?careerId={Uri.EscapeDataString(career.Id)}";

    await Shell.Current.GoToAsync(route);
}


	
private async void OnHomeTapped(object? sender, TappedEventArgs e)
{
    try
    {
        await Shell.Current.GoToAsync(nameof(MyCareerPage));
    }
    catch (Exception ex)
    {
        await DisplayAlert(
            "Navigation Error",
            ex.ToString(),
            "OK");
    }
}
	private async void OnMyCareerTapped(object? sender, TappedEventArgs e)
	{
		await Shell.Current.GoToAsync(nameof(MyCareerPage));
	}
	private void ApplyFilters()
{
    var filtered = _allCareers
        .Where(career =>
            (_selectedCategory == "All" ||
             career.Category.Equals(
                 _selectedCategory,
                 StringComparison.OrdinalIgnoreCase))
            &&
            (string.IsNullOrWhiteSpace(_searchText) ||
             career.Name.Contains(
                 _searchText,
                 StringComparison.OrdinalIgnoreCase) ||
             career.Description.Contains(
                 _searchText,
                 StringComparison.OrdinalIgnoreCase) ||
             career.Category.Contains(
                 _searchText,
                 StringComparison.OrdinalIgnoreCase) ||
             career.SalaryRangeIndia.Contains(
                 _searchText,
                 StringComparison.OrdinalIgnoreCase) ||
             career.FutureDemand.Contains(
                 _searchText,
                 StringComparison.OrdinalIgnoreCase) ||
             career.EducationPath.Contains(
                 _searchText,
                 StringComparison.OrdinalIgnoreCase) ||
             career.KeySkills.Any(skill =>
                 skill.Contains(
                     _searchText,
                     StringComparison.OrdinalIgnoreCase)) ||
             career.EntranceExams.Any(exam =>
                 exam.Contains(
                     _searchText,
                     StringComparison.OrdinalIgnoreCase)) ||
					 career.SearchAliases.Any(alias => alias.Contains(_searchText, StringComparison.OrdinalIgnoreCase)) ||
             career.Workplaces.Any(workplace =>
                 workplace.Contains(
                     _searchText,
                     StringComparison.OrdinalIgnoreCase))))
        .OrderBy(career => career.Name)
        .ToList();

    ResetCollection(FilteredCareers, filtered);

    var popular = filtered
        .Where(career => career.IsPopular)
        .Take(6)
        .ToList();

    if (!popular.Any())
    {
        popular = filtered
            .Take(6)
            .ToList();
    }

    ResetCollection(PopularCareers, popular);
}

	private void ResetCollection(ObservableCollection<Career> collection, IReadOnlyCollection<Career> items)
	{
		collection.Clear();

		foreach (var item in items)
		{
			collection.Add(item);
		}
	}

	private void ApplyCategoryStyles()
	{
		SetCategoryButtonState(AllCategoryButton, _selectedCategory == "All");
		SetCategoryButtonState(ScienceCategoryButton, _selectedCategory == "Science");
		SetCategoryButtonState(CommerceCategoryButton, _selectedCategory == "Commerce");
		SetCategoryButtonState(ArtsCategoryButton, _selectedCategory == "Arts");
	}

	private static void SetCategoryButtonState(Button button, bool isSelected)
	{
		button.BackgroundColor = isSelected ? Color.FromArgb("#17203B") : Colors.White;
		button.TextColor = isSelected ? Colors.White : Color.FromArgb("#6F7691");
	}
}