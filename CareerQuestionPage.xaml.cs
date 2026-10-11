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
            "q1-math" => "If you enjoy Maths, you may like careers where you use numbers and solve problems, such as engineering, computers, business or finance.",
            "q1-science" => "If you enjoy Science, you may like learning how things work. It can lead to careers in medicine, engineering, nature, research and more.",
            "q1-biology" => "Biology is the study of living things. Enjoying it can be useful for careers in medicine, healthcare, animals, plants and life science.",
            "q1-computers" => "If you enjoy Computers, you may like creating apps, websites, games or other technology.",
            "q1-business" => "Business and Economics are about money, buying and selling, companies and making decisions. They can lead to many business and finance careers.",
            "q1-languages" => "If you enjoy Languages, you may like reading, writing, speaking or teaching. These skills are useful in many careers.",
            "q1-art" => "If you enjoy Art and Design, you may like drawing, creating and making things look good. This can lead to many creative careers.",
            "q1-social" => "Social Studies helps you learn about people, communities, history and how society works. It can be useful for law, teaching, government and media.",
            "q2-puzzles" => "If you enjoy puzzles, you may enjoy finding clues, spotting patterns and working out answers.",
            "q2-building" => "If you enjoy building things, you may like making, fixing or creating useful things. This can be useful in engineering, design and technology.",
            "q2-helping" => "If you enjoy helping people, you may like careers where you care for, teach, guide or support others.",
            "q2-designing" => "If you enjoy creating designs, you may like drawing, planning how things look and making new ideas. This is useful in design, fashion, media and architecture.",
            "q2-computers" => "If you enjoy working with computers, you may like creating software, solving computer problems or working with technology.",
            "q2-leading" => "If you enjoy leading a team, you may like helping a group work together, making decisions and taking responsibility.",
            "q2-writing" => "If you enjoy writing or communicating, you may like sharing ideas, telling stories or speaking with people. These skills are useful in many careers.",
            "q2-understanding" => "If you like understanding how things work, you may enjoy science, engineering, medicine or technology.",
            "q3-love" => "If you love Maths, many careers are open to you, especially careers that use numbers, logic and problem solving.",
            "q3-enjoy" => "If you enjoy Maths, it can be helpful in careers such as engineering, computers, science, business and finance.",
            "q3-okay" => "Being okay with Maths is perfectly fine. Many careers need only basic Maths, while some need more.",
            "q3-not" => "Not enjoying Maths is okay. There are many careers where Maths is not a major part of the work.",
            "q4-technology" => "This means you like creating or using technology. You may enjoy computers, apps, robots, games or other digital tools.",
            "q4-body" => "If you like learning about the human body, you may enjoy careers such as doctor, dentist, nurse, physiotherapist or other health careers.",
            "q4-business" => "If you like running a business, you may enjoy planning, making decisions, working with customers and leading a team.",
            "q4-design" => "If you like designing creative things, you may enjoy drawing, choosing colours, planning ideas and creating new things.",
            "q4-environment" => "If you care about the environment, you may enjoy work related to plants, animals, nature, farming or protecting our planet.",
            "q4-money" => "If you enjoy learning about money and investments, you may like careers in banking, accounting, finance or business.",
            "q4-helping" => "If you enjoy helping people, you may like careers where you care for, teach, guide or support others.",
            "q4-research" => "Research means asking questions, finding answers and learning something new. It is important in science, medicine, technology and many other areas.",
            "q5-logical" => "Logical thinking means looking at information carefully and working out the best answer. It is useful in many careers.",
            "q5-communication" => "Good communication means explaining your ideas clearly, listening to others and working well with people.",
            "q5-creativity" => "Creativity means coming up with new ideas and different ways of doing things. It is useful in both creative and technical careers.",
            "q5-problem" => "Problem solving means looking at a challenge and finding a way to fix or improve it. Almost every career needs this skill.",
            "q5-leadership" => "Leadership means helping others, making decisions and taking responsibility. It can be useful when you lead a team or project.",
            "q5-numbers" => "If you are good with numbers, you may enjoy careers in finance, accounting, science, engineering, data or business.",
            "q5-people" => "If you enjoy working with people, you may like careers in healthcare, teaching, business, law, sales or public service.",
            "q5-detail" => "Attention to detail means noticing small things and making sure work is correct. This is useful in many careers.",
            "q6-independent" => "This means you like working on your own, planning your tasks and finishing them without needing someone to guide you all the time.",
            "q6-small-team" => "This means you like working with a few people, sharing ideas and helping each other complete a task.",
            "q6-lots-people" => "This means you enjoy being around and talking with many people. This can suit teaching, healthcare, business and many other careers.",
            "q7-technology" => "You may enjoy finding ways to create, improve or fix technology such as computers, apps and digital systems.",
            "q7-business" => "You may enjoy solving problems about customers, money, companies, planning and how a business can improve.",
            "q7-health" => "You may enjoy helping solve problems related to people's health, wellbeing and care.",
            "q7-design" => "You may enjoy finding creative ways to make something look better, feel easier to use or work better.",
            "q7-environment" => "You may enjoy finding ways to protect nature, reduce pollution, save resources and care for the planet.",
            "q7-financial" => "You may enjoy solving problems involving money, saving, spending, budgets and investments.",
            "q7-social" => "You may enjoy solving problems that affect people, communities, fairness, rules or government.",
            "q8-office" => "This can mean working with computers, meetings, projects and other tasks in an office or technology setting.",
            "q8-hospital" => "This can mean working with patients, doctors, nurses and other healthcare workers to help people stay healthy.",
            "q8-lab" => "A laboratory is a place where people test ideas, study things closely and make new discoveries.",
            "q8-corporate" => "A business setting can involve working with teams, customers, projects, money and business decisions.",
            "q8-studio" => "A creative studio can be a place where people design, draw, create videos, make media or develop new ideas.",
            "q8-outdoors" => "Outdoor work can involve nature, farming, building, surveying, environmental work or other activities outside an office.",
            "q8-government" => "Government and public-service work focuses on helping communities and running services that people use.",
            "q9-earning" => "This means earning a good income is important to you when choosing a career. Income can change with the job, experience and location.",
            "q9-stability" => "This means you would like a career with steady work and good chances of finding work for many years.",
            "q9-creativity" => "This means you want a career where you can create, imagine new ideas and do things in your own way.",
            "q9-helping" => "This means making a positive difference to other people is important to you.",
            "q9-innovation" => "Innovation means thinking of new ideas or better ways to do something.",
            "q9-leadership" => "This means you are interested in guiding people, making decisions and taking responsibility.",
            "q9-balance" => "This means having enough time for family, friends, hobbies and rest as well as work.",
            "q9-international" => "This means you would like opportunities to work with people or organisations from other countries.",
            "q10-technology" => "Technology includes careers involving computers, apps, software, AI, data and other digital tools.",
            "q10-healthcare" => "Healthcare includes careers such as doctor, dentist, nurse, pharmacist and physiotherapist, as well as many other health careers.",
            "q10-business" => "Business and Finance includes careers involving money, companies, banking, accounting, management and starting a business.",
            "q10-engineering" => "Engineering uses Maths and Science to design, build and improve things such as machines, buildings, roads and technology.",
            "q10-science" => "Science and Research means asking questions, doing tests and learning more about the world around us.",
            "q10-design" => "Design and Media includes careers where you create pictures, products, websites, videos, fashion or other creative work.",
            "q10-law" => "Law and Public Service includes careers that work with rules, government, public services and helping communities.",
            "q10-education" => "Education includes careers where you help others learn, such as teaching, training and academic support.",
            _ => option.Info.Length > 0 ? option.Info : "This option tells us something about what you like or prefer. It helps StudyMap suggest career areas for you to explore."
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
