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

    public CareerQuestionPage() => InitializeComponent();

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
        if (_question is null) return;

        QuestionCounterLabel.Text = $"Question {_index} of {total}";
        ProgressBarControl.Progress = (double)_index / total;
        QuestionTextLabel.Text = _question.Text;
        HintLabel.Text = _index == 10 ? "Select up to 2 options" : (_question.AllowMultiple ? "Select all that apply" : "Select one option");
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
        if (_question is null) return;
        foreach (var option in _question.Options)
            OptionsLayout.Children.Add(CreateOptionCard(option));
    }

    private Border CreateOptionCard(CareerQuestionOption option)
    {
        var isSelected = _selectedOptionIds.Contains(option.Id);
        var iconLabel = new Label { Text = option.Icon, FontSize = 22, VerticalOptions = LayoutOptions.Center };
        var textLabel = new Label
        {
            Text = option.Text, FontSize = 15, FontFamily = "OpenSansSemibold",
            TextColor = isSelected ? Colors.White : Color.FromArgb("#17203B"),
            VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.WordWrap,
            HorizontalOptions = LayoutOptions.Fill
        };
        var infoButton = new Button
        {
            Text = "ⓘ", FontSize = 21, TextColor = isSelected ? Colors.White : Color.FromArgb("#7C57F5"),
            BackgroundColor = Colors.Transparent, Padding = 0, WidthRequest = 32, HeightRequest = 32,
            VerticalOptions = LayoutOptions.Center, HorizontalOptions = LayoutOptions.End
        };
        infoButton.Clicked += async (_, _) => await ShowOptionInfoAsync(option);
        var checkLabel = new Label
        {
            Text = "✓", FontSize = 16, FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White, IsVisible = isSelected, VerticalOptions = LayoutOptions.Center
        };
        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Auto)
            },
            ColumnSpacing = 10, VerticalOptions = LayoutOptions.Center
        };
        row.Add(iconLabel, 0); row.Add(textLabel, 1); row.Add(infoButton, 2); row.Add(checkLabel, 3);

        var card = new Border
        {
            Padding = new Thickness(16, 12), StrokeThickness = isSelected ? 0 : 1,
            Stroke = Color.FromArgb("#EEF0F6"), BackgroundColor = isSelected ? Color.FromArgb("#7C57F5") : Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 }, Content = row
        };
        card.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => OnOptionTapped(option)) });
        return card;
    }

    private async Task ShowOptionInfoAsync(CareerQuestionOption option) => await DisplayAlert(option.Text, GetOptionInfo(option), "Got it");

    private static string GetOptionInfo(CareerQuestionOption option)
    {
        return option.Id switch
        {
            "q1-math" => "Maths is about numbers, patterns and solving problems.",
            "q1-science" => "Science is about understanding how the world works by observing, testing and asking questions.",
            "q1-biology" => "Biology is about living things, plants, animals and the human body.",
            "q1-computers" => "Computers involve using technology, digital tools and software to create, solve problems and work with information.",
            "q1-business" => "Business and Economics are about money, buying and selling, companies, markets and making decisions.",
            "q1-languages" => "Languages involve reading, writing, speaking and understanding how people communicate.",
            "q1-art" => "Art and Design involve drawing, creating, choosing how things look and expressing ideas visually.",
            "q1-social" => "Social Studies is about people, communities, history and how society works.",
            "q2-puzzles" => "Solving puzzles means finding clues, spotting patterns and working out answers.",
            "q2-building" => "Building things means making, fixing or putting things together to create something useful.",
            "q2-helping" => "Helping people means caring for, supporting, guiding or encouraging others.",
            "q2-computers" => "Working with computers means using digital tools, software and technology to create or solve problems.",
            "q3-technology" => "Creating technology means using ideas, tools and digital systems to make something new or useful.",
            "q3-health" => "Learning about health means understanding the human body, wellbeing and ways to support people.",
            "q3-business" => "Business involves planning, making decisions, understanding customers and learning how money works.",
            "q4-technology" => "Creating technology means using ideas, tools and digital systems to make something new or useful.",
            "q4-body" => "Learning about the human body means understanding how the body's organs, systems and processes work.",
            "q4-business" => "Running a business involves planning, making decisions, managing resources and understanding customers.",
            "q4-design" => "Designing creative things means using ideas, shapes, colours and layouts to create something.",
            "q4-environment" => "Protecting the environment means caring for nature, reducing pollution, saving resources and looking after the planet.",
            "q4-research" => "Research means asking questions, collecting information, testing ideas and learning something new.",
            "q5-breakdown" => "Breaking a problem into smaller parts is a step-by-step way to understand and solve it.",
            "q5-ideas" => "Trying different ideas is a creative way to find a solution.",
            "q5-research" => "Research means finding more information so you can understand a problem better.",
            "q5-discuss" => "Discussing a problem with others can help you see different ideas and approaches.",
            "q5-numbers" => "Looking at numbers and calculations is useful when a problem involves quantities, patterns or data.",
            "q5-people" => "Thinking about how a problem affects people focuses on human needs and wellbeing.",
            "q6-computer" => "Making something using a computer can involve coding, digital design, research or other technology skills.",
            "q6-building" => "Building with your hands involves practical problem solving and creating something tangible.",
            "q6-design" => "Design and creative activities involve ideas, visual thinking and making things.",
            "q6-debate" => "Debating and presenting involve communication, reasoning and explaining ideas clearly.",
            "q6-sport" => "Sport involves physical activity, practice, teamwork and performance.",
            "q6-help" => "Helping other students involves cooperation, communication and supporting people.",
            "q7-how" => "Being curious about how or why something works is a strong learning and research signal.",
            "q7-solve" => "Solving a problem yourself shows enjoyment of analytical thinking and challenge.",
            "q7-creative" => "Finding a creative way to explain something shows creative thinking and communication.",
            "q7-explain" => "Explaining something to others combines understanding with communication.",
            "q7-experiment" => "Experimenting and researching involves testing ideas and discovering answers.",
            "q7-numbers" => "Using numbers or logic shows comfort with quantitative and analytical thinking.",
            "q7-help" => "Understanding how something can help someone shows a people-focused mindset.",
            "q7-make" => "Using learning to make something combines knowledge with practical problem solving.",
            "q8-maths" => "Others see you as someone who can help with Maths and difficult problems.",
            "q8-science" => "Others see you as someone who can help with Science or experiments.",
            "q8-technology" => "Others see you as someone who can help with computers, apps or technology.",
            "q8-research" => "Others rely on you to find information or research a topic.",
            "q8-writing" => "Others see you as someone who can help with writing, English or languages.",
            "q8-design" => "Others see you as someone who can help with drawing, design or presentations.",
            "q8-speaking" => "Others see you as someone who can explain ideas or speak confidently.",
            "q8-helping" => "Others trust you to help people understand or solve problems.",
            "q8-sports" => "Others may see you as someone who is good at sports or physical activities.",
            "q8-organising" => "Others trust you to organise, coordinate or bring people together.",
            "q9-technology" => "Technology and computers include software, digital tools, data and systems.",
            "q9-engineering" => "Engineering uses Maths, Science and practical thinking to design and improve things.",
            "q9-science" => "Science and research involve asking questions, testing ideas and discovering how things work.",
            "q9-healthcare" => "Healthcare involves understanding health, the human body and caring for people.",
            "q9-business" => "Business and Finance involve money, companies, markets, planning and decisions.",
            "q9-design" => "Design and creative work involve visual ideas, making things and expressing ideas.",
            "q9-law" => "Law, Government and Public Service involve society, rules, rights and community services.",
            "q9-education" => "Teaching and Education involve learning, explaining and helping others develop.",
            "q9-environment" => "Environment and Nature involve plants, animals, sustainability and protecting the planet.",
            "q9-media" => "Media and Communication involve writing, speaking, storytelling and creating content.",
            "q10-salary" => "This means having the potential to earn a high income is important to you.",
            "q10-stable" => "This means having steady work and feeling confident about long-term employment is important to you.",
            "q10-enjoy" => "This means enjoying what you do and finding your work interesting matters to you.",
            "q10-help" => "This means making a positive difference to other people's lives matters to you.",
            "q10-growth" => "This means having opportunities to learn, improve and take on more responsibility matters to you.",
            "q10-creative" => "This means having opportunities to imagine, create and express new ideas matters to you.",
            "q10-technology" => "This means working with computers, digital tools, software or technology interests you.",
            "q10-difference" => "This means doing work that has a positive impact on people or society matters to you.",
            _ => option.Info.Length > 0 ? option.Info : "This option describes something about your interests, preferences or strengths."
        };
    }

    private void OnOptionTapped(CareerQuestionOption option)
    {
        if (_question is null) return;
        var isNoneOption = option.Id.EndsWith("-none", StringComparison.OrdinalIgnoreCase);

        if (_question.AllowMultiple)
        {
            if (isNoneOption)
            {
                _selectedOptionIds.Clear();
                _selectedOptionIds.Add(option.Id);
            }
            else
            {
                _selectedOptionIds.RemoveAll(id => id.EndsWith("-none", StringComparison.OrdinalIgnoreCase));

                if (_selectedOptionIds.Contains(option.Id))
                {
                    _selectedOptionIds.Remove(option.Id);
                }
                else if (_index == 10)
                {
                    if (_selectedOptionIds.Count >= 2)
                        return;
                    _selectedOptionIds.Add(option.Id);
                }
                else
                {
                    _selectedOptionIds.Add(option.Id);
                }
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
        if (_question is null || _selectedOptionIds.Count == 0) return;

        var selectedTexts = _question.Options.Where(o => _selectedOptionIds.Contains(o.Id)).Select(o => o.Text).ToList();
        CareerDiscoverySessionService.Instance.SetAnswer(_index, [.. _selectedOptionIds], selectedTexts);

        var total = _recommendationService.Questions.Count;
        if (_index < total)
            await Shell.Current.GoToAsync($"{nameof(CareerQuestionPage)}?index={_index + 1}");
        else
            await Shell.Current.GoToAsync(nameof(CareerResultsPage));
    }

    private async void OnBackTapped(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnHomeTapped(object? sender, TappedEventArgs e) => await Shell.Current.GoToAsync(nameof(MyCareerPage));
}
