namespace StudyMap.Models;

public class Stream
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public string AccentColor { get; set; } = "#F6A64F";

    public string GradientStartColor { get; set; } = "#FFFFFF";

    public string GradientEndColor { get; set; } = "#FFFFFF";

    public string TagLine { get; set; } = string.Empty;

    public int CareerCount { get; set; }

    public List<string> FeaturedCareers { get; set; } = [];
}