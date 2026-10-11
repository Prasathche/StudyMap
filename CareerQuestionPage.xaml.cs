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
            "q1-math" => "Enjoying Mathematics can be useful for engineering, technology, finance, economics, analytics and many problem-solving careers.",
            "q1-science" => "Enjoying Science can point toward engineering, medicine, healthcare, research, environmental science and other scientific fields.",
            "q1-biology" => "Interest in Biology is especially useful for medicine, dentistry, nursing, biotechnology, life sciences, agriculture and healthcare careers.",
            "q1-computers" => "Interest in Computers can lead toward software, AI, data science, cybersecurity, IT and other technology careers.",
            "q1-business" => "Business and Economics involve markets, money, organisations and decision-making. They are useful for finance, management, entrepreneurship and business careers.",
            "q1-languages" => "Languages can support careers involving communication, writing, teaching, journalism, law, media, translation and public-facing work.",
            "q1-art" => "Art and Design interest can be useful in graphic design, UI/UX, animation, architecture, fashion, media and other creative careers.",
            "q1-social" => "Social Studies can build interest in people, society, history, government and communities, supporting law, public service, education, journalism and social sciences.",

            "q2-puzzles" => "Enjoying puzzles suggests you may like analysing information, finding patterns and solving structured problems.",
            "q2-building" => "Building things can indicate an interest in creating practical solutions, useful in engineering, architecture, manufacturing and technology.",
            "q2-helping" => "Helping people is important in careers such as healthcare, education, counselling, social work and many service professions.",
            "q2-designing" => "Creating designs involves visual thinking and creativity and can connect with design, architecture, UI/UX, media and fashion.",
            "q2-computers" => "Working with computers suggests comfort with technology and can support software, IT, AI, data and cybersecurity careers.",
            "q2-leading" => "Leading a team involves coordinating people, making decisions and taking responsibility, useful in management, business and leadership roles.",
            "q2-writing" => "Writing or communicating is valuable in journalism, law, education, marketing, media, public relations and many professional roles.",
            "q2-understanding" => "Wanting to understand how things work can be useful in science, engineering, medicine, technology and research.",

            "q3-love" => "A strong enjoyment of Mathematics can support careers that use quantitative reasoning, such as engineering, technology, finance, economics and analytics.",
            "q3-enjoy" => "Being comfortable with Mathematics can open many options, including engineering, technology, finance, science and analytical careers.",
            "q3-okay" => "Being neutral about Mathematics does not close career options. Many careers use only some mathematical skills.",
            "q3-not" => "Not enjoying Mathematics is useful information. It may make some highly math-intensive paths less attractive, while many other careers remain open.",

            "q4-technology" => "Creating technology means designing or building digital or technical solutions. It can connect with software, AI, engineering, robotics and IT.",
            "q4-body" => "Interest in how the human body works can connect with medicine, dentistry, nursing, physiotherapy, psychology, biotechnology and health sciences.",
            "q4-business" => "Running a business involves planning, customers, finance, operations and leadership. It can lead toward management and entrepreneurship.",
            "q4-design" => "Designing creative things combines creativity with problem solving and can lead toward graphic design, UI/UX, architecture, fashion and media.",
            "q4-environment" => "Protecting the environment can connect with environmental science, agriculture, sustainability, conservation and related research careers.",
            "q4-money" => "Interest in money and investments can connect with finance, accounting, economics, banking, investment and financial analysis.",
            "q4-helping" => "Helping people can be central to healthcare, education, counselling, social work and other people-focused careers.",
            "q4-research" => "Research and discovery involve asking questions, testing ideas and learning from evidence. This is important in science, medicine, technology and social research.",

            "q5-logical" => "Logical thinking helps you analyse information, identify patterns and make reasoned decisions. It is useful across technology, science, engineering and business.",
            "q5-communication" => "Communication means explaining ideas clearly, listening and working with others. It is valuable in almost every career, especially people-facing roles.",
            "q5-creativity" => "Creativity helps you generate new ideas and approaches. It is useful in design, media, technology, business, research and many other fields.",
            "q5-problem" => "Problem solving means breaking a challenge into parts and finding workable solutions. It is useful across engineering, technology, healthcare and business.",
            "q5-leadership" => "Leadership involves guiding people, making decisions and taking responsibility. It can be useful in management, business, public service and team-based professions.",
            "q5-numbers" => "Being comfortable with numbers can support finance, accounting, economics, data, engineering and analytical careers.",
            "q5-people" => "Working well with people can be valuable in healthcare, education, counselling, management, sales, law and public service.",
            "q5-detail" => "Attention to detail helps when accuracy matters, including healthcare, finance, engineering, research, software testing and design.",

            "q6-independent" => "Independent work means you are comfortable planning and completing tasks with less direct supervision. Many professional and specialist careers offer this style.",
            "q6-small-team" => "Small-team work combines collaboration with personal responsibility and is common in technology, research, design, consulting and many professional fields.",
            "q6-lots-people" => "Working with lots of people can suit careers involving communication, leadership, education, healthcare, customer interaction and public service.",

            "q7-technology" => "Technology problems involve creating, improving or troubleshooting digital and technical systems.",
            "q7-business" => "Business problems involve decisions about customers, operations, strategy, money, growth and organisations.",
            "q7-health" => "Health-related problems involve understanding health, disease, treatment, wellbeing or healthcare systems.",
            "q7-design" => "Design problems involve improving how something looks, feels or works for people through creative problem solving.",
            "q7-environment" => "Environmental problems involve sustainability, pollution, natural resources, climate, conservation and how people interact with nature.",
            "q7-financial" => "Financial problems involve budgets, investments, risk, markets, accounting and making decisions with financial information.",
            "q7-social" => "Social problems involve communities, fairness, public policy, law, government and improving society.",

            "q8-office" => "An office or technology environment may involve computers, software, meetings, analysis, project work and digital tools.",
            "q8-hospital" => "Hospital and healthcare environments involve patient care, clinical teams, health services and medical technology.",
            "q8-lab" => "Laboratory and research environments involve experiments, testing, analysis and evidence-based discovery.",
            "q8-corporate" => "Business and corporate environments often involve teams, projects, customers, finance, strategy and organisational decision-making.",
            "q8-studio" => "Creative studios can involve design, visual communication, media, animation, content creation and collaborative creative work.",
            "q8-outdoors" => "Outdoor work can involve nature, agriculture, construction, surveying, environmental work, field research and related activities.",
            "q8-government" => "Government and public-service environments involve public programmes, administration, policy, law and serving communities.",

            "q9-earning" => "High earning potential means income is an important factor in your career choice. Actual earnings vary by role, experience, location and performance.",
            "q9-stability" => "Job stability means you value predictable employment and long-term demand. Stability varies by profession, employer and economic conditions.",
            "q9-creativity" => "Creativity means you want opportunities to generate ideas, create things or approach problems in original ways.",
            "q9-helping" => "Helping people means making a positive difference to others is an important part of what you want from your work.",
            "q9-innovation" => "Innovation means developing new ideas, products, processes or solutions and improving how things are done.",
            "q9-leadership" => "Leadership means you are interested in guiding people, making decisions and taking responsibility for outcomes.",
            "q9-balance" => "Work-life balance means having time and energy for life outside work. Different careers and employers offer different levels of flexibility.",
            "q9-international" => "Working internationally means you are interested in opportunities involving other countries, cultures, markets or global organisations.",

            "q10-technology" => "Technology covers careers such as software development, AI, data science, cybersecurity, IT and other digital fields.",
            "q10-healthcare" => "Healthcare includes medicine, dentistry, nursing, pharmacy, physiotherapy, allied health and many health-science careers.",
            "q10-business" => "Business and Finance includes accounting, banking, investment, economics, management, consulting and entrepreneurship.",
            "q10-engineering" => "Engineering applies mathematics and science to design, build and improve products, systems, structures and technology.",
            "q10-science" => "Science and Research involves studying the natural or social world through observation, experiments, analysis and evidence.",
            "q10-design" => "Design and Media includes visual design, UI/UX, animation, fashion, content, communication and other creative fields.",
            "q10-law" => "Law and Public Service includes legal careers, government, civil services, policy, administration and community-focused roles.",
            "q10-education" => "Education includes teaching, training, academic support and roles focused on helping others learn and develop.",

            _ => option.Info.Length > 0
                ? option.Info
                : "This option describes an interest or preference that can help StudyMap suggest career areas to explore."
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
