using StudyMap.Models;
using StudyMap.ViewModels;

namespace StudyMap;

public partial class CareerRoadmapPage : ContentPage
{
    private readonly CareerRoadmapViewModel _viewModel = new CareerRoadmapViewModel();

    public CareerRoadmapPage()
    {
        InitializeComponent();
        BindingContext = _viewModel;
        _viewModel.NavigateToDetailsAction = async stage =>
        {
            if (stage is null)
            {
                return;
            }

            await Shell.Current.Navigation.PushAsync(new CareerStageDetailsPage(stage));
        };

        _viewModel.ShowMessageAction = async message =>
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            await DisplayAlert("Saved", message, "OK");
        };
    }
}
