using System.Collections.ObjectModel;
using System.Text.Json;
using StudyMap.Models;
using StudyMap.Services;
using StudyMap.ViewModels;

namespace StudyMap;

public partial class MyCareerPage : ContentPage
{
    public MyCareerPage()
    {
        InitializeComponent();

        CareerCountLabel.Text = "0";
        RoadmapCountLabel.Text = "0";
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }

    private async void OnHomeClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//CareerExplorerPage");
    }

    private void OnCareersTabTapped(object? sender, TappedEventArgs e)
    {
        SavedCareersSection.IsVisible = true;
        RoadmapsSection.IsVisible = false;

        CareersTab.BackgroundColor = Color.FromArgb("#5B3FD3");
        RoadmapsTab.BackgroundColor = Colors.White;

        SetTabTextColor(CareersTab, Colors.White);
        SetTabTextColor(
            RoadmapsTab,
            Color.FromArgb("#555555"));
    }

    private void OnRoadmapsTabTapped(object? sender, TappedEventArgs e)
    {
        SavedCareersSection.IsVisible = false;
        RoadmapsSection.IsVisible = true;

        RoadmapsTab.BackgroundColor = Color.FromArgb("#5B3FD3");
        CareersTab.BackgroundColor = Colors.White;

        SetTabTextColor(RoadmapsTab, Colors.White);
        SetTabTextColor(
            CareersTab,
            Color.FromArgb("#555555"));
    }

    private static void SetTabTextColor(
        Border border,
        Color color)
    {
        if (border.Content is Label label)
        {
            label.TextColor = color;
        }
    }

    private async void OnExploreClicked(
        object? sender,
        EventArgs e)
    {
        await Shell.Current.GoToAsync(
            "//CareerExplorerPage");
    }
}