using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using StudyMap.Models;
using StudyMap.Services;

namespace StudyMap.ViewModels;

public class CareerRoadmapViewModel : INotifyPropertyChanged
{
    private CareerRoadmap? _selectedRoadmap;
    private int _currentStageIndex;
    private bool _isSaved;
    private string _statusMessage = string.Empty;
    private readonly CareersService _careersService = new();
    private readonly SavedRoadmapsService _savedRoadmapsService = new();
    private readonly List<CareerRoadmap> _allRoadmaps = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CareerRoadmap> AvailableRoadmaps { get; } = new();
    public ObservableCollection<CareerStage> Stages { get; } = new();

    public ICommand ViewStageDetailsCommand { get; }
    public ICommand SaveRoadmapCommand { get; }
    public ICommand ClearCareerCommand { get; }

    public Action<CareerStage>? NavigateToDetailsAction { get; set; }
    public Action<string>? ShowMessageAction { get; set; }

    public CareerRoadmap? SelectedRoadmap
    {
        get => _selectedRoadmap;
        set
        {
            if (ReferenceEquals(value, _selectedRoadmap))
            {
                return;
            }

            _selectedRoadmap = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedCareer));
            IsSaved = _selectedRoadmap is not null && _savedRoadmapsService.IsSaved(_selectedRoadmap.CareerId);
            RefreshRoadmap();
        }
    }

    public bool HasSelectedCareer => SelectedRoadmap is not null;

    public int CurrentStageIndex
    {
        get => _currentStageIndex;
        set
        {
            if (value == _currentStageIndex)
            {
                return;
            }

            _currentStageIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ProgressText));
            OnPropertyChanged(nameof(CompletionProgress));
            UpdateStageProgress();
        }
    }

    public string ProgressText =>
        Stages.Count == 0
            ? "0/0 completed"
            : $"{Math.Min(CurrentStageIndex + 1, Stages.Count)}/{Stages.Count} completed";

    public double CompletionProgress =>
        Stages.Count == 0
            ? 0
            : Math.Clamp((CurrentStageIndex + 1) / (double)Stages.Count, 0, 1);

    public bool IsSaved
    {
        get => _isSaved;
        private set
        {
            if (value == _isSaved)
            {
                return;
            }

            _isSaved = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (value == _statusMessage)
            {
                return;
            }

            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public CareerRoadmapViewModel()
    {
        ViewStageDetailsCommand = new Command<CareerStage>(OnViewStageDetails);
        SaveRoadmapCommand = new Command(OnSaveRoadmap);
        ClearCareerCommand = new Command(ClearCareer);
        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        await LoadCareersFromDatabaseAsync();
    }

    private async Task LoadCareersFromDatabaseAsync()
    {
        try
        {
            var careers = await _careersService.GetAllCareersAsync();

            _allRoadmaps.Clear();
            AvailableRoadmaps.Clear();

            foreach (var career in careers)
            {
                var roadmap = ConvertCareerToRoadmap(career);
                _allRoadmaps.Add(roadmap);
                AvailableRoadmaps.Add(roadmap);
            }

            // Start with no career selected. The timeline appears only after
            // the student chooses a career from the dropdown.
            SelectedRoadmap = null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading careers: {ex.Message}");
            ShowMessageAction?.Invoke("Failed to load careers. Please check the data file.");
        }
    }

    private CareerRoadmap ConvertCareerToRoadmap(Career career)
    {
        return new CareerRoadmap
        {
            CareerId = career.Id,
            Category = string.IsNullOrWhiteSpace(career.Category) ? "Other" : career.Category,
            CareerName = career.Name,
            Subtitle = $"{career.Description} - {career.SalaryRangeIndia}",
            Stages = GenerateRoadmapStages(career)
        };
    }

    private static string GetRecommendedStream(Career career)
    {
        return career.Id switch
        {
            "career-doctor" => "Choose Science with PCB (Physics, Chemistry, Biology) in Class 11–12.",
            "career-dentist" => "Choose Science with PCB (Physics, Chemistry, Biology) in Class 11–12.",
            "career-nurse" => "Choose Science with PCB (Physics, Chemistry, Biology) in Class 11–12.",
            "career-pharmacist" => "Choose Science with PCB or PCM (Physics, Chemistry, Biology/Mathematics) in Class 11–12.",
            "career-software-engineer" => "Choose Science with PCM (Physics, Chemistry, Mathematics) in Class 11–12; Computer Science is recommended if available.",
            "career-data-scientist" => "Choose Science with PCM (Physics, Chemistry, Mathematics) in Class 11–12.",
            "career-civil-engineer" => "Choose Science with PCM (Physics, Chemistry, Mathematics) in Class 11–12.",
            "career-architect" => "Choose Science with Physics and Mathematics in Class 11–12; PCM is the clearest route.",
            "career-chartered-accountant" => "Choose Commerce in Class 11–12, preferably with Accountancy, Business Studies and Economics.",
            "career-investment-banker" => "Choose Commerce in Class 11–12; Science with Mathematics is also a strong route.",
            "career-business-analyst" => "Choose Commerce, Science or Arts/Humanities in Class 11–12; Mathematics and/or Computer Science are helpful.",
            "career-financial-planner" => "Choose Commerce in Class 11–12, preferably with Accountancy, Economics and Mathematics.",
            "career-marketing-manager" => "Choose Commerce in Class 11–12; Arts/Humanities is also a good option for communication-focused students.",
            "career-human-resources-manager" => "Choose Commerce or Arts/Humanities in Class 11–12; Science is also eligible.",
            "career-supply-chain-manager" => "Choose Commerce in Class 11–12; Science with Mathematics is also a strong option.",
            "career-lawyer" => "Choose Arts/Humanities, Commerce or Science in Class 11–12; there is no mandatory stream for law.",
            "career-psychologist" => "Choose Arts/Humanities in Class 11–12, preferably with Psychology if available; other streams are also eligible.",
            "career-journalist" => "Choose Arts/Humanities in Class 11–12; Commerce or Science is also eligible.",
            "career-graphic-designer" => "Choose Arts/Humanities in Class 11–12; Art/Design subjects are helpful if available.",
            "career-ui-ux-designer" => "Choose Arts/Humanities in Class 11–12 if you want a design-focused route; Science or Commerce also work.",
            "career-fashion-designer" => "Choose Arts/Humanities in Class 11–12; Art/Design subjects are helpful if available.",
            "career-hotel-manager" => "Choose any stream in Class 11–12; Commerce or Arts/Humanities can be especially useful for hospitality.",
            "career-interior-designer" => "Choose Arts/Humanities in Class 11–12; Science or Commerce are also eligible.",
            "career-ias-officer" => "Choose any stream in Class 11–12; select the stream and subjects that best match your strengths and future degree plans.",
            "career-ips-officer" => "Choose any stream in Class 11–12; focus on academics, fitness and general awareness.",
            "career-defence-officer" => "Choose Science with PCM for NDA Navy/Air Force routes; for NDA Army, other streams are eligible, and graduation-based entries have different requirements.",
            "career-pilot" => "Choose Science with PCM (Physics, Chemistry, Mathematics) in Class 11–12.",
            "career-biotechnologist" => "Choose Science with PCB or PCM in Class 11–12; PCB is preferred for biology/life-science routes.",
            "career-agricultural-scientist" => "Choose Science with PCB or PCM in Class 11–12; PCB is a natural fit for life-science and agriculture pathways.",
            "career-physiotherapist" => "Choose Science with PCB (Physics, Chemistry, Biology) in Class 11–12.",
            "career-radiologic-technologist" => "Choose Science with PCB (Physics, Chemistry, Biology) in Class 11–12.",
            "career-occupational-therapist" => "Choose Science with PCB (Physics, Chemistry, Biology) in Class 11–12.",
            "career-animator" => "Choose Arts/Humanities in Class 11–12 for a design-focused route; any stream is eligible and Computer Science can also help.",
            "career-social-worker" => "Choose Arts/Humanities in Class 11–12; other streams are also eligible.",
            "career-content-creator" => "Choose any stream in Class 11–12; Arts/Humanities or Commerce can be useful for communication and media skills.",
            "pharmacist" => "Choose Science with PCB or PCM in Class 11–12.",
            "police-officer" => "Choose any stream in Class 11–12; eligibility depends on the post, with graduation required for many SI/IPS routes.",
            "advocate" => "Choose Arts/Humanities, Commerce or Science in Class 11–12; there is no mandatory stream for law.",
            "entrepreneur" => "Choose any stream in Class 11–12; Commerce is useful for business fundamentals but is not mandatory.",
            "civil-services-officer" => "Choose any stream in Class 11–12; select subjects that match your strengths and future graduation plans.",
            "company-secretary" => "Choose Commerce in Class 11–12 for the strongest foundation; other streams are also eligible.",
            _ => "Choose the Class 11–12 stream that best matches the career's subject requirements and your strengths."
        };
    }

    private List<CareerStage> GenerateRoadmapStages(Career career)
    {
        var stages = new List<CareerStage>();
        var recommendedStream = GetRecommendedStream(career);

        stages.Add(new CareerStage
        {
            Title = "Class 10",
            Description = $"Build a strong foundation and start exploring the subjects and stream needed for {career.Name}.",
            Icon = "📘",
            AccentColor = "#4A61F8",
            Details = new List<string>
            {
                $"Prepare for stream selection towards {career.Name}",
                "Focus on core subjects based on the stream",
                $"Entrance requirements: {(career.EntranceExams.Count > 0 ? career.EntranceExams[0] : "Check requirements")}"
            },
            Tips = career.KeySkills.Take(2).ToList()
        });

        stages.Add(new CareerStage
        {
            Title = "Choose Stream",
            Description = recommendedStream,
            Icon = "🧪",
            AccentColor = "#16A085",
            Details = new List<string>
            {
                $"Class 11–12 recommendation: {recommendedStream}",
                "Check the eligibility requirements of the colleges and entrance exams you plan to target.",
                "Choose subjects you can perform well in while keeping the career pathway open."
            },
            Tips = new List<string>
            {
                "Confirm the subject combination before finalizing your Class 11 admission.",
                "Discuss the choice with parents, teachers and a career counsellor if you are unsure."
            }
        });

        stages.Add(new CareerStage
        {
            Title = "Class 11–12 Subjects",
            Description = career.RoadmapSteps.Count > 1 ? career.RoadmapSteps[1] : "Master core subjects for your pathway.",
            Icon = "📚",
            AccentColor = "#F39C12",
            Details = career.RoadmapSteps.Count > 1
                ? new List<string> { career.RoadmapSteps[1] }
                : new List<string> { "Strengthen subject knowledge", "Practice examinations", "Build relevant skills" },
            Tips = career.KeySkills.Take(3).ToList()
        });

        stages.Add(new CareerStage
        {
            Title = "Entrance Exams",
            Description = career.EntranceExams.Count > 0 ? $"Prepare for {string.Join(", ", career.EntranceExams)}" : "Prepare for competitive exams",
            Icon = "📝",
            AccentColor = "#E74C3C",
            Details = new List<string>
            {
                career.EntranceExams.Count > 0 ? $"Key exams: {string.Join(", ", career.EntranceExams)}" : "Check entrance requirements",
                "Create a disciplined study schedule",
                "Practice mock tests regularly"
            },
            Tips = new List<string>
            {
                "Master exam patterns and syllabus.",
                "Take regular timed practice tests."
            }
        });

        stages.Add(new CareerStage
        {
            Title = "Undergraduate Degree",
            Description = career.RoadmapSteps.Count > 2 ? career.RoadmapSteps[2] : "Complete your bachelor's degree from a reputable institution.",
            Icon = "🎓",
            AccentColor = "#8E44AD",
            Details = new List<string>
            {
                career.TopColleges.Count > 0 ? $"Top colleges: {string.Join(", ", career.TopColleges.Take(2))}" : "Attend top colleges",
                "Practical and internship experience",
                "Build projects relevant to the career"
            },
            Tips = career.KeySkills.Take(3).ToList()
        });

        stages.Add(new CareerStage
        {
            Title = "Specialization (Optional PG)",
            Description = career.RoadmapSteps.Count > 3 ? career.RoadmapSteps[3] : "Consider advanced specialization or certification.",
            Icon = "🎯",
            AccentColor = "#2C3E50",
            Details = new List<string>
            {
                "Evaluate career growth opportunities",
                "Explore master's or specialized certifications",
                "Build expertise in your focus area"
            },
            Tips = new List<string>
            {
                "Choose specialization based on market demand.",
                $"Future demand: {career.FutureDemand}"
            }
        });

        stages.Add(new CareerStage
        {
            Title = "Career Launch",
            Description = "Begin your professional journey in the field.",
            Icon = "🚀",
            AccentColor = "#27AE60",
            Details = new List<string>
            {
                $"Salary range: {career.SalaryRangeIndia}",
                career.Workplaces.Count > 0 ? $"Workplaces: {string.Join(", ", career.Workplaces.Take(2))}" : "Explore job opportunities",
                "Apply for entry-level or internship positions"
            },
            Tips = new List<string>
            {
                "Network with professionals in your field.",
                "Keep upgrading your skills continuously."
            }
        });

        return stages;
    }

    private void RefreshRoadmap()
    {
        Stages.Clear();

        if (SelectedRoadmap is not null)
        {
            foreach (var stage in SelectedRoadmap.Stages)
            {
                Stages.Add(stage);
            }
        }

        CurrentStageIndex = FindCurrentStageIndex();
        OnPropertyChanged(nameof(ProgressText));
        OnPropertyChanged(nameof(CompletionProgress));
    }

    private int FindCurrentStageIndex()
    {
        for (var index = 0; index < Stages.Count; index++)
        {
            if (Stages[index].Title.Contains("Class 10", StringComparison.OrdinalIgnoreCase))
            {
                return index;
            }
        }

        return 0;
    }

    private void UpdateStageProgress()
    {
        for (var index = 0; index < Stages.Count; index++)
        {
            Stages[index].IsCurrent = index == CurrentStageIndex;
        }
    }

    private void OnViewStageDetails(CareerStage stage)
    {
        NavigateToDetailsAction?.Invoke(stage);
    }

    private void ClearCareer()
    {
        // Clear the career and its timeline. The student can choose another career.
        SelectedRoadmap = null;
    }

    private void OnSaveRoadmap()
    {
        if (SelectedRoadmap is null)
        {
            return;
        }

        _savedRoadmapsService.Add(SelectedRoadmap.CareerId);
        IsSaved = true;
        StatusMessage = "✓ Roadmap saved successfully!";

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await Task.Delay(2000);
            StatusMessage = string.Empty;
            IsSaved = false;
        });
    }

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
