namespace StudyMap.Models;

/// <summary>
/// A ranked career suggestion produced by <see cref="Services.CareerRecommendationService"/>.
/// </summary>
public class CareerRecommendation
{
    public Career Career { get; set; } = null!;

    public int Score { get; set; }

    /// <summary>Interest Match percentage shown to the student. Not a scientific probability.</summary>
    public int MatchPercentage { get; set; }

    public List<string> MatchedTags { get; set; } = [];

    public string WhyItMatches { get; set; } = string.Empty;
}
