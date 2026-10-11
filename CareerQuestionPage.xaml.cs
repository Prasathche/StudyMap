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
        var info = GetOptionInfo(option);
        await DisplayAlert(option.Text, info, "Got it");
    }

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
            "q2-designing" => "Creating designs means planning how something should look, feel or work and turning ideas into a design.",
            "q2-computers" => "Working with computers means using digital tools, software and technology to create or solve problems.",
            "q2-leading" => "Leading a team means guiding a group, making decisions, listening to others and taking responsibility.",
            "q2-writing" => "Writing or communicating means sharing ideas clearly through words, speaking, writing or other forms of communication.",
            "q2-understanding" => "Understanding how things work means being curious about how parts connect and why something happens.",
            "q3-love" => "This means you really enjoy Maths, including numbers, patterns, logic and solving problems.",
            "q3-enjoy" => "This means you generally enjoy working with numbers, patterns and Maths problems.",
            "q3-okay" => "This means you are comfortable with Maths, even if it is not your favourite subject.",
            "q3-not" => "This means Maths is not something you particularly enjoy, and that is completely okay.",
            "q4-technology" => "Creating technology means using ideas, tools and digital systems to make something new or useful.",
            "q4-body" => "Learning about the human body means understanding how the body's organs, systems and processes work.",
            "q4-business" => "Running a business involves planning, making decisions, managing resources and understanding customers.",
            "q4-design" => "Designing creative things means using ideas, shapes, colours and layouts to create something.",
            "q4-environment" => "Protecting the environment means caring for nature, reducing pollution, saving resources and looking after the planet.",
            "q4-money" => "Understanding money and investments means learning about saving, spending, budgets, investing and how money grows or changes.",
            "q4-helping" => "Helping people means supporting others, understanding their needs and making a positive difference.",
            "q4-research" => "Research means asking questions, collecting information, testing ideas and learning something new.",
            "q5-logical" => "Logical thinking means looking at information carefully, finding patterns and working out a sensible answer.",
            "q5-communication" => "Good communication means explaining ideas clearly, listening carefully and understanding other people.",
            "q5-creativity" => "Creativity means coming up with new ideas and finding different ways to make or do things.",
            "q5-problem" => "Problem solving means understanding a challenge and finding a practical way to fix or improve it.",
            "q5-leadership" => "Leadership means guiding others, making decisions and taking responsibility.",
            "q5-numbers" => "Being good with numbers means working comfortably with calculations, quantities, patterns and numerical information.",
            "q5-people" => "Working with people means communicating, cooperating, listening and building good relationships with others.",
            "q5-detail" => "Attention to detail means noticing small things and making sure information or work is accurate.",
            "q6-independent" => "Working independently means planning your own tasks, making decisions and completing work by yourself.",
            "q6-small-team" => "Working with a small team means sharing ideas, helping each other and completing tasks together.",
            "q6-lots-people" => "Working with lots of people means enjoying regular interaction, conversations and teamwork with many different people.",
            "q7-technology" => "Technology problems involve finding ways to create, improve, fix or use digital tools and systems.",
            "q7-business" => "Business problems involve customers, money, planning, decisions and improving how something works.",
            "q7-health" => "Health-related problems involve understanding people's health, wellbeing and ways to support them.",
            "q7-design" => "Design problems involve finding creative ways to make something look better, feel easier to use or work better.",
            "q7-environment" => "Environmental problems involve protecting nature, reducing pollution, saving resources and caring for the planet.",
            "q7-financial" => "Financial problems involve money, saving, spending, budgets, costs and investments.",
            "q7-social" => "Social problems involve people, communities, fairness, rules and how society can improve.",
            "q8-office" => "An office or technology environment can involve computers, meetings, projects and organised work.",
            "q8-hospital" => "A hospital environment involves caring for people, working with others and supporting health and wellbeing.",
            "q8-lab" => "A laboratory is a place where people test ideas, study things closely, collect information and make discoveries.",
            "q8-corporate" => "A business or corporate environment involves teams, projects, customers, planning and decision-making.",
            "q8-studio" => "A creative studio is a place for developing ideas, designing, drawing, creating media or making things.",
            "q8-outdoors" => "Outdoor work means spending time outside rather than working mainly at a desk or indoors.",
            "q8-government" => "Government and public service involve working with communities, public systems, rules and services.",
            "q9-earning" => "This means having the potential to earn a good income is important to you.",
            "q9-stability" => "Job stability means having steady work and feeling confident about long-term employment.",
            "q9-creativity" => "Creativity means having opportunities to imagine, create and express new ideas.",
            "q9-helping" => "Helping people means making a positive difference to other people's lives is important to you.",
            "q9-innovation" => "Innovation means thinking of a new idea or finding a better way to do something.",
            "q9-leadership" => "Leadership means guiding people, making decisions and taking responsibility.",
            "q9-balance" => "Work-life balance means having enough time for work, family, friends, hobbies and rest.",
            "q9-international" => "Working internationally means having opportunities to interact with people, ideas or organisations from different countries.",
            "q10-technology" => "Technology is about computers, digital tools, software, data and systems that help people do things.",
            "q10-healthcare" => "Healthcare is about protecting health, preventing illness, understanding the body and caring for people.",
            "q10-business" => "Business and Finance are about money, companies, markets, planning, management and financial decisions.",
            "q10-engineering" => "Engineering uses Maths, Science and practical thinking to design, build and improve things.",
            "q10-science" => "Science and Research involve asking questions, testing ideas, observing results and learning about the world.",
            "q10-design" => "Design and Media involve creating visual ideas, products, images, websites, videos or other forms of creative work.",
            "q10-law" => "Law and Public Service involve rules, rights, responsibilities, government and services for communities.",
            "q10-education" => "Education is about learning, teaching, sharing knowledge and helping people develop new skills.",
            _ => option.Info.Length > 0 ? option.Info : "This option describes something about your interests, preferences or strengths."
        };
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
                _selectedOptionIds.Clear();
                _selectedOptionIds.Add(option.Id);
            }
            else
            {
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