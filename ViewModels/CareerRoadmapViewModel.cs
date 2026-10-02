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
    private CareerRoadmap _selectedRoadmap = new CareerRoadmap();
    private int _currentStageIndex;
    private bool _isSaved;
    private string _statusMessage = string.Empty;
    private readonly CareersService _careersService = new();

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<CareerRoadmap> AvailableRoadmaps { get; } = new();
    public ObservableCollection<CareerStage> Stages { get; } = new();

    public ICommand ViewStageDetailsCommand { get; }
    public ICommand SaveRoadmapCommand { get; }

    public Action<CareerStage>? NavigateToDetailsAction { get; set; }
    public Action<string>? ShowMessageAction { get; set; }

    public CareerRoadmap SelectedRoadmap
    {
        get => _selectedRoadmap;
        set
        {
            if (value == _selectedRoadmap)
            {
                return;
            }

            _selectedRoadmap = value;
            OnPropertyChanged();
            RefreshRoadmap();
        }
    }

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

    public string ProgressText => Stages.Count == 0 ? "0/0 completed" : $"{Math.Min(CurrentStageIndex + 1, Stages.Count)}/{Stages.Count} completed";
    public double CompletionProgress => Stages.Count == 0 ? 0 : Math.Clamp((CurrentStageIndex + 1) / (double)Stages.Count, 0, 1);
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
            
            foreach (var career in careers)
            {
                var roadmap = ConvertCareerToRoadmap(career);
                AvailableRoadmaps.Add(roadmap);
            }

            if (AvailableRoadmaps.Count > 0)
            {
                SelectedRoadmap = AvailableRoadmaps.First();
            }
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
            CareerName = career.Name,
            Subtitle = $"{career.Description} - {career.SalaryRangeIndia}",
            Stages = GenerateRoadmapStages(career)
        };
    }

    private List<CareerStage> GenerateRoadmapStages(Career career)
    {
        var stages = new List<CareerStage>();

        // Class 10 Stage
        stages.Add(new CareerStage
        {
            Title = "Class 10",
            Description = career.RoadmapSteps.Count > 0 ? career.RoadmapSteps[0] : "Build foundation for your chosen career.",
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

        // Stream Selection Stage
        stages.Add(new CareerStage
        {
            Title = "Choose Stream",
            Description = career.RoadmapSteps.Count > 1 ? career.RoadmapSteps[1] : "Select appropriate subjects for your career.",
            Icon = "🧪",
            AccentColor = "#16A085",
            Details = new List<string>
            {
                $"Career Category: {career.Category}",
                "Consult with mentors and educators",
                "Evaluate your interests and strengths"
            },
            Tips = new List<string> { "Choose a stream aligned with your career goals.", "Connect with professionals in this field." }
        });

        // Class 11-12 Stage
        stages.Add(new CareerStage
        {
            Title = "Class 11–12 Subjects",
            Description = career.RoadmapSteps.Count > 2 ? career.RoadmapSteps[2] : "Master core subjects for your pathway.",
            Icon = "📚",
            AccentColor = "#F39C12",
            Details = career.RoadmapSteps.Count > 2 
                ? career.RoadmapSteps.Skip(2).Take(3).ToList() 
                : new List<string> { "Strengthen subject knowledge", "Practice examinations", "Build relevant skills" },
            Tips = career.KeySkills.Take(3).ToList()
        });

        // Entrance Exams Stage
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

        // Undergraduate Degree
        stages.Add(new CareerStage
        {
            Title = "Undergraduate Degree",
            Description = "Complete your bachelor's degree from a reputable institution.",
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

        // Specialization/Postgrad Stage
        stages.Add(new CareerStage
        {
            Title = "Specialization (Optional PG)",
            Description = "Consider advanced specialization or certification.",
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

        // Career Launch Stage
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

        foreach (var stage in _selectedRoadmap.Stages)
        {
            Stages.Add(stage);
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

    private void OnSaveRoadmap()
    {
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
