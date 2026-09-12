using System.Collections.Generic;

namespace StudyMap.Models;

public class CareerStage
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#7C57F5";
    public List<string> Details { get; set; } = new();
    public List<string> Tips { get; set; } = new();
    public bool IsCurrent { get; set; }
}

public class CareerRoadmap
{
    public string CareerName { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public List<CareerStage> Stages { get; set; } = new();

    public override string ToString() => CareerName;
}
