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
            "career-doctor" => "Science with PCB (Physics, Chemistry, Biology)",
            "career-dentist" => "Science with PCB (Physics, Chemistry, Biology)",
            "career-nurse" => "Science with PCB (Physics, Chemistry, Biology)",
            "career-pharmacist" or "pharmacist" => "Science with PCB or PCM (Physics, Chemistry, Biology/Mathematics)",
            "career-software-engineer" => "Science with PCM (Physics, Chemistry, Mathematics); Computer Science is recommended if available",
            "career-data-scientist" => "Science with PCM (Physics, Chemistry, Mathematics)",
            "career-civil-engineer" => "Science with PCM (Physics, Chemistry, Mathematics)",
            "career-architect" => "Science with Physics and Mathematics; PCM is the clearest route",
            "career-chartered-accountant" => "Commerce; Accountancy, Business Studies and Economics are recommended",
            "career-investment-banker" => "Commerce; Science with Mathematics is also a strong route",
            "career-business-analyst" => "Commerce, Science or Arts/Humanities; Mathematics and/or Computer Science are helpful",
            "career-financial-planner" => "Commerce; Accountancy, Economics and Mathematics are recommended",
            "career-marketing-manager" => "Commerce or Arts/Humanities; Science is also eligible",
            "career-human-resources-manager" => "Commerce or Arts/Humanities; Science is also eligible",
            "career-supply-chain-manager" => "Commerce; Science with Mathematics is also a strong route",
            "career-lawyer" or "advocate" => "Arts/Humanities, Commerce or Science; there is no mandatory stream for law",
            "career-psychologist" => "Arts/Humanities, preferably with Psychology if available",
            "career-journalist" => "Arts/Humanities; Commerce or Science is also eligible",
            "career-graphic-designer" => "Arts/Humanities; Art/Design subjects are helpful if available",
            "career-ui-ux-designer" => "Arts/Humanities for a design-focused route; Science or Commerce also work",
            "career-fashion-designer" => "Arts/Humanities; Art/Design subjects are helpful if available",
            "career-hotel-manager" => "Any stream; Commerce or Arts/Humanities can be especially useful",
            "career-interior-designer" => "Arts/Humanities; Science or Commerce are also eligible",
            "career-ias-officer" or "career-civil-services-officer" => "Any stream; choose subjects that match your strengths and future degree plans",
            "career-ips-officer" or "police-officer" => "Any stream; focus on academics, fitness and general awareness",
            "career-defence-officer" => "Science with PCM for NDA Navy/Air Force; NDA Army also accepts other streams",
            "career-pilot" => "Science with PCM (Physics, Chemistry, Mathematics)",
            "career-biotechnologist" => "Science with PCB or PCM; PCB is preferred for biology/life-science routes",
            "career-agricultural-scientist" => "Science with PCB or PCM; PCB is a natural fit for life-science routes",
            "career-physiotherapist" => "Science with PCB (Physics, Chemistry, Biology)",
            "career-radiologic-technologist" => "Science with PCB (Physics, Chemistry, Biology)",
            "career-occupational-therapist" => "Science with PCB (Physics, Chemistry, Biology)",
            "career-animator" => "Arts/Humanities for a design-focused route; any stream is eligible and Computer Science can help",
            "career-social-worker" => "Arts/Humanities; other streams are also eligible",
            "career-content-creator" => "Any stream; Arts/Humanities or Commerce can be useful for communication and media skills",
            "entrepreneur" => "Any stream; Commerce is useful for business fundamentals but is not mandatory",
            "company-secretary" => "Commerce for the strongest foundation; other streams are also eligible",
            _ => GetFallbackStream(career)
        };
    }

    private static string GetFallbackStream(Career career)
    {
        var category = career.Category?.Trim().ToLowerInvariant() ?? string.Empty;
        var preferred = career.PreferredSubjects.Count > 0
            ? string.Join(", ", career.PreferredSubjects.Take(3))
            : "subjects relevant to the career";

        if (category.Contains("commerce") || category.Contains("finance") || category.Contains("business"))
        {
            return $"Commerce; focus on {preferred}";
        }

        if (category.Contains("design") || category.Contains("media") || category.Contains("arts"))
        {
            return $"Arts/Humanities; focus on {preferred}";
        }

        if (category.Contains("engineering") || category.Contains("technology") || category.Contains("science"))
        {
            return $"Science; choose the Mathematics/Science combination required by the target course, with {preferred} as useful subjects";
        }

        return "Any stream; choose subjects that match the career's college/entrance requirements and your strengths";
    }

    private static string GetClass11To12Subjects(Career career)
    {
        var stream = GetRecommendedStream(career);
        var preferredSubjects = career.PreferredSubjects.Count > 0
            ? string.Join(", ", career.PreferredSubjects.Take(4))
            : string.Empty;

        if (stream.Contains("PCB", StringComparison.OrdinalIgnoreCase))
        {
            return "Focus on Physics, Chemistry and Biology (PCB). Keep English strong and meet the subject eligibility of the target course.";
        }

        if (stream.Contains("PCM", StringComparison.OrdinalIgnoreCase))
        {
            return "Focus on Physics, Chemistry and Mathematics (PCM). Keep English strong; add Computer Science if it supports your target course.";
        }

        if (stream.StartsWith("Commerce", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(preferredSubjects)
                ? "Focus on Accountancy, Business Studies and Economics; take Mathematics when useful for your target degree."
                : $"Focus on Accountancy, Business Studies and Economics; relevant subjects include {preferredSubjects}.";
        }

        if (stream.StartsWith("Arts/Humanities", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(preferredSubjects)
                ? "Focus on English and relevant Humanities/Social Science subjects; choose Psychology, Economics or other relevant subjects when available."
                : $"Focus on English and relevant Humanities/Social Science subjects; useful subjects include {preferredSubjects}.";
        }

        return string.IsNullOrWhiteSpace(preferredSubjects)
            ? "Choose Class 11–12 subjects that keep the target degree and entrance requirements open; strengthen English and the subjects most relevant to the career."
            : $"Keep the pathway flexible and strengthen these relevant subjects: {preferredSubjects}. Check the target course eligibility before finalizing subjects.";
    }

    private static string GetEntranceExamGuidance(Career career)
    {
        if (career.EntranceExams.Count == 0)
        {
            return "Check the admission route for the target undergraduate course; some colleges admit through merit rather than a common entrance exam.";
        }

        return $"Prepare for {string.Join(", ", career.EntranceExams)}. Confirm the current eligibility, subject requirements and exam pattern before applying.";
    }

    private static string GetUndergraduateGuidance(Career career)
    {
        if (career.RoadmapSteps.Count > 2 && !string.IsNullOrWhiteSpace(career.RoadmapSteps[2]))
        {
            return career.RoadmapSteps[2];
        }

        return string.IsNullOrWhiteSpace(career.EducationPath)
            ? $"Complete the appropriate undergraduate qualification for {career.Name}."
            : career.EducationPath;
    }

    private static string GetAdvancedPathGuidance(Career career)
    {
        if (career.RoadmapSteps.Count > 3 && !string.IsNullOrWhiteSpace(career.RoadmapSteps[3]))
        {
            return career.RoadmapSteps[3];
        }

        return "Consider postgraduate study, specialization, certification or deeper industry experience based on your career goals.";
    }

    private static List<string> GetCareerLaunchDetails(Career career)
    {
        var details = new List<string>();

        if (career.Workplaces.Count > 0)
        {
            details.Add($"Target workplaces: {string.Join(", ", career.Workplaces.Take(3))}");
        }

        details.Add($"Build the key skills employers expect: {string.Join(", ", career.KeySkills.Take(3))}.");
        details.Add("Apply for relevant internships, trainee roles or entry-level opportunities and build practical evidence of your skills.");

        return details;
    }

    private List<CareerStage> GenerateRoadmapStages(Career career)
    {
        var stages = new List<CareerStage>();
        var recommendedStream = GetRecommendedStream(career);

        stages.Add(new CareerStage
        {
            Title = "Class 10",
            Description = $"Build the foundation needed for {career.Name} and prepare for the right Class 11–12 subject combination.",
            Icon = "📘",
            AccentColor = "#4A61F8",
            Details = new List<string>
            {
                $"Explore what {career.Name} involves and whether it matches your interests.",
                $"Strengthen your current school subjects, especially: {(career.PreferredSubjects.Count > 0 ? string.Join(", ", career.PreferredSubjects.Take(3)) : "the subjects relevant to your pathway")}.",
                "Research the Class 11 stream and subject eligibility for the courses you may target after Class 12."
            },
            Tips = career.KeySkills.Take(2).ToList()
        });

        stages.Add(new CareerStage
        {
            Title = "Choose Stream",
            Description = $"For Class 11–12: choose {recommendedStream}.",
            Icon = "🧪",
            AccentColor = "#16A085",
            Details = new List<string>
            {
                $"Class 11–12 recommendation: {recommendedStream}.",
                "Check the eligibility requirements of the colleges and entrance exams you plan to target before finalizing subjects.",
                "Choose a combination you can perform well in while keeping the intended career pathway open."
            },
            Tips = new List<string>
            {
                "Confirm the subject combination before Class 11 admission.",
                "Discuss the choice with parents, teachers or a career counsellor if you are unsure."
            }
        });

        stages.Add(new CareerStage
        {
            Title = "Class 11–12 Subjects",
            Description = GetClass11To12Subjects(career),
            Icon = "📚",
            AccentColor = "#F39C12",
            Details = new List<string>
            {
                GetClass11To12Subjects(career),
                $"Build the academic foundation required for {career.Name} and keep your target course eligibility in mind.",
                "Track the subject-specific requirements of the colleges and entrance exams you intend to apply for."
            },
            Tips = career.KeySkills.Take(3).ToList()
        });

        stages.Add(new CareerStage
        {
            Title = "Entrance Exams",
            Description = GetEntranceExamGuidance(career),
            Icon = "📝",
            AccentColor = "#E74C3C",
            Details = new List<string>
            {
                career.EntranceExams.Count > 0 ? $"Key exams: {string.Join(", ", career.EntranceExams)}" : "No single common entrance exam is specified for this career.",
                "Check the latest eligibility, subject requirements, application dates and syllabus before applying.",
                "Use mock tests and a structured preparation plan where an entrance exam is required."
            },
            Tips = new List<string>
            {
                "Start exam preparation early when the target course has a competitive entrance process.",
                "Do not assume every college uses the same admission route."
            }
        });

        stages.Add(new CareerStage
        {
            Title = "Undergraduate Degree",
            Description = GetUndergraduateGuidance(career),
            Icon = "🎓",
            AccentColor = "#8E44AD",
            Details = new List<string>
            {
                career.TopColleges.Count > 0 ? $"Example colleges: {string.Join(", ", career.TopColleges.Take(3))}" : "Shortlist colleges that offer the required qualification.",
                "Build practical experience through projects, labs, internships, clinical training or fieldwork as appropriate.",
                "Choose electives and activities that strengthen the skills required for the career."
            },
            Tips = career.KeySkills.Take(3).ToList()
        });

        stages.Add(new CareerStage
        {
            Title = "Specialization / Advanced Path",
            Description = GetAdvancedPathGuidance(career),
            Icon = "🎯",
            AccentColor = "#2C3E50",
            Details = new List<string>
            {
                "Decide whether you need postgraduate study, professional certification or deeper industry experience.",
                "Choose specialization based on your interests, eligibility and the roles you want to pursue.",
                $"Future demand for this career: {career.FutureDemand}."
            },
            Tips = new List<string>
            {
                "Compare the cost, duration and career value of advanced study before enrolling.",
                "Continue building practical expertise even when postgraduate study is optional."
            }
        });

        stages.Add(new CareerStage
        {
            Title = "Career Launch",
            Description = $"Start your professional journey as a {career.Name} and build experience in the relevant work environment.",
            Icon = "🚀",
            AccentColor = "#27AE60",
            Details = GetCareerLaunchDetails(career),
            Tips = new List<string>
            {
                "Build a strong resume or portfolio with measurable evidence of your skills.",
                "Keep upgrading your skills as the role and industry evolve."
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
