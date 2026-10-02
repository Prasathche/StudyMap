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

    // ── QueryProperty ────────────────────────────────────────────────
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

    // ── Question loading ─────────────────────────────────────────────
    private void LoadQuestion()
    {
        var total = _recommendationService.Questions.Count;
        _question = _recommendationService.Questions.FirstOrDefault(q => q.Number == _index);
        if (_question is null)
        {
            return;
        }

        QuestionCounterLabel.Text = $"Question {_index} of {total}";
        ProgressBarControl.Progress = (double)_index / total;
        QuestionTextLabel.Text = _question.Text;
        HintLabel.Text = _question.AllowMultiple ? "Select all that apply" : "Select one option";
        NextButton.Text = _index == total ? "See My Results" : "Next";

        _selectedOptionIds.Clear();
        var existingAnswer = CareerDiscoverySessionService.Instance.GetAnswer(_index);
        if (existingAnswer is not null)
        {
            _selectedOptionIds.AddRange(existingAnswer.SelectedOptionIds);
        }

        BuildOptionCards();
        UpdateNextButtonState();
    }

    private void BuildOptionCards()
    {
        OptionsLayout.Children.Clear();
        if (_question is null)
        {
            return;
        }

        foreach (var option in _question.Options)
        {
            OptionsLayout.Children.Add(CreateOptionCard(option));
        }
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

        var checkLabel = new Label
        {
            Text = "✓",
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            IsVisible = isSelected,
            VerticalOptions = LayoutOptions.Center
        };

        var row = new HorizontalStackLayout
        {
            Spacing = 14,
            Children = { iconLabel, textLabel, checkLabel }
        };

        var card = new Border
        {
            Padding = new Thickness(16, 14),
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

    private void OnOptionTapped(CareerQuestionOption option)
    {
        if (_question is null)
        {
            return;
        }

        if (_question.AllowMultiple)
        {
            if (!_selectedOptionIds.Remove(option.Id))
            {
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

    // ── Navigation ────────────────────────────────────────────────────
    private async void OnNextClicked(object? sender, EventArgs e)
    {
        if (_question is null || _selectedOptionIds.Count == 0)
        {
            return;
        }

        var selectedTexts = _question.Options
            .Where(o => _selectedOptionIds.Contains(o.Id))
            .Select(o => o.Text)
            .ToList();

        CareerDiscoverySessionService.Instance.SetAnswer(_index, [.. _selectedOptionIds], selectedTexts);

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
}
