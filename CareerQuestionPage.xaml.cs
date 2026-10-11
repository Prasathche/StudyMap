using StudyMap.Models;
using StudyMap.Services;

namespace StudyMap;

[QueryProperty(nameof(Index), "index")]
public partial class CareerQuestionPage : ContentPage
{
    private readonly CareerRecommendationService _recommendationService = new();
    private readonly List<string> _selectedOptionIds = [];
    private CareerQuestion? _question;
    private int _index = 1;

    public CareerQuestionPage()
    {
        InitializeComponent();
    }

    public string Index
    {
        set
        {
            if (int.TryParse(value, out var parsed))
            {
                _index = parsed;
                MainThread.BeginInvokeOnMainThread(LoadQuestion);
            }
        }
    }

    private void LoadQuestion()
    {
        var total = _recommendationService.Questions.Count;
        _question = _recommendationService.Questions.FirstOrDefault(q => q.Number == _index);
        if (_question is null)
            return;

        QuestionCounterLabel.Text = $"Question {_index} of {total}";
        ProgressBarControl.Progress = (double)_index / total;
        QuestionTextLabel.Text = _question.Text;
        HintLabel.Text = _question.AllowMultiple ? "Select all that apply" : "Select one option";
        NextButton.Text = _index == total ? "See My Results" : "Next";

        _selectedOptionIds.Clear();
        var existingAnswer = CareerDiscoverySessionService.Instance.GetAnswer(_index);
        if (existingAnswer is not null)
            _selectedOptionIds.AddRange(existingAnswer.SelectedOptionIds);

        BuildOptionCards();
        UpdateNextButtonState();
    }

    private void BuildOptionCards()
    {
        OptionsLayout.Children.Clear();
        if (_question is null)
            return;

        foreach (var option in _question.Options)
            OptionsLayout.Children.Add(CreateOptionCard(option));
    }

    private Border CreateOptionCard(CareerQuestionOption option)
    {
        var isSelected = _selectedOptionIds.Contains(option.Id);

        var iconLabel = new Label
        {
            Text = option.Icon,
            FontSize = 22,
            VerticalOptions = LayoutOptions.Center
        };

        var textLabel = new Label
        {
            Text = option.Text,
            FontSize = 15,
            FontFamily = "OpenSansSemibold",
            TextColor = isSelected ? Colors.White : Color.FromArgb("#17203B"),
            VerticalOptions = LayoutOptions.Center,
            LineBreakMode = LineBreakMode.WordWrap,
            HorizontalOptions = LayoutOptions.Fill
        };

        var infoButton = new Button
        {
            Text = "i",
            FontFamily = "OpenSansSemibold",
            FontSize = 14,
            TextColor = isSelected ? Colors.White : Color.FromArgb("#7C57F5"),
            BackgroundColor = Colors.Transparent,
            Padding = 0,
            WidthRequest = 30,
            HeightRequest = 30,
            CornerRadius = 15,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.End
        };

        infoButton.Clicked += async (_, _) => await ShowOptionInfoAsync(option);

        var checkLabel = new Label
        {
            Text = "✓",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            IsVisible = isSelected,
            VerticalOptions = LayoutOptions.Center
        };

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10,
            VerticalOptions = LayoutOptions.Center
        };

        row.Add(iconLabel, 0);
        row.Add(textLabel, 1);
        row.Add(infoButton, 2);
        row.Add(checkLabel, 3);

        var card = new Border
        {
            Padding = new Thickness(16, 12),
            StrokeThickness = isSelected ? 0 : 1,
            Stroke = Color.FromArgb("#EEF0F6"),
            BackgroundColor = isSelected ? Color.FromArgb("#7C57F5") : Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Content = row
        };

        card.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => OnOptionTapped(option))
        });

        return card;
    }

    private async Task ShowOptionInfoAsync(CareerQuestionOption option)
    {
        var info = string.IsNullOrWhiteSpace(option.Info)
            ? $"This option helps describe an interest, strength, preference, or career area. Selecting it helps StudyMap suggest relevant careers to explore."
            : option.Info;

        await DisplayAlert(option.Text, info, "Got it");
    }

    private void OnOptionTapped(CareerQuestionOption option)
    {
        if (_question is null)
            return;

        var isNoneOption = option.Id.EndsWith("-none", StringComparison.OrdinalIgnoreCase);

        if (_question.AllowMultiple)
        {
            if (isNoneOption)
            {
                // "None of the above" is mutually exclusive with all other choices.
                _selectedOptionIds.Clear();
                _selectedOptionIds.Add(option.Id);
            }
            else
            {
                // Selecting any real preference removes the neutral option.
                _selectedOptionIds.RemoveAll(id => id.EndsWith("-none", StringComparison.OrdinalIgnoreCase));

                if (!_selectedOptionIds.Remove(option.Id))
                    _selectedOptionIds.Add(option.Id);
            }
        }
        else
        {
            _selectedOptionIds.Clear();
            _selectedOptionIds.Add(option.Id);
        }

        BuildOptionCards();
        UpdateNextButtonState();
    }

    private void UpdateNextButtonState()
    {
        var hasSelection = _selectedOptionIds.Count > 0;
        NextButton.IsEnabled = hasSelection;
        NextButton.Opacity = hasSelection ? 1 : 0.5;
    }

    private async void OnNextClicked(object? sender, EventArgs e)
    {
        if (_question is null || _selectedOptionIds.Count == 0)
            return;

        var selectedTexts = _question.Options
            .Where(o => _selectedOptionIds.Contains(o.Id))
            .Select(o => o.Text)
            .ToList();

        CareerDiscoverySessionService.Instance.SetAnswer(
            _index,
            [.. _selectedOptionIds],
            selectedTexts);

        var total = _recommendationService.Questions.Count;
        if (_index < total)
        {
            await Shell.Current.GoToAsync($"{nameof(CareerQuestionPage)}?index={_index + 1}");
        }
        else
        {
            await Shell.Current.GoToAsync(nameof(CareerResultsPage));
        }
    }

    private async void OnBackTapped(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnHomeTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MyCareerPage));
    }
}
