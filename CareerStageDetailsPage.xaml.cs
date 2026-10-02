using StudyMap.Models;

namespace StudyMap;

public partial class CareerStageDetailsPage : ContentPage
{
    private CareerStage _selectedStage = new CareerStage();

    public CareerStage SelectedStage
    {
        get => _selectedStage;
        set
        {
            _selectedStage = value ?? new CareerStage();
            OnPropertyChanged();
        }
    }

    public CareerStageDetailsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public CareerStageDetailsPage(CareerStage selectedStage)
        : this()
    {
        SelectedStage = selectedStage;
    }
}
