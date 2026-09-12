namespace StudyMap.Models;

/// <summary>
/// Fully-resolved, ready-to-display parent insight for a single career.
/// Produced by <see cref="Services.ParentInsightsService"/> — combines any explicit
/// overrides on <see cref="Career"/> with derived, rule-based defaults.
/// </summary>
public class CareerParentInsight
{
    public Career Career { get; set; } = null!;

    public string SalaryRangeText { get; set; } = "Information unavailable";
    public string SalaryDescription { get; set; } = string.Empty;

    public string StabilityLevel { get; set; } = "Information unavailable";
    public string StabilityDescription { get; set; } = string.Empty;
    public string StabilityIndicator { get; set; } = "⚪";

    public string FutureDemandLevel { get; set; } = "Information unavailable";
    public string FutureDemandDescription { get; set; } = string.Empty;
    public string FutureDemandIndicator { get; set; } = "⚪";

    public string DifficultyLevel { get; set; } = "Information unavailable";
    public string DifficultyDescription { get; set; } = string.Empty;
    public string DifficultyIndicator { get; set; } = "⚪";

    public string CompetitionLevel { get; set; } = "Information unavailable";
    public string CompetitionDescription { get; set; } = string.Empty;
    public string CompetitionIndicator { get; set; } = "⚪";

    public string TypicalUGDuration { get; set; } = "Information unavailable";
    public List<string> UGCourses { get; set; } = [];
    public List<string> PGCourses { get; set; } = [];
    public List<string> EntranceExams { get; set; } = [];

    public List<string> ParentConsiderations { get; set; } = [];
    public List<string> SuitableFor { get; set; } = [];
    public List<string> ThingsToConsider { get; set; } = [];

    // Opportunity + Reality + Consideration balance (see product principle in spec).
    public string OpportunityStatement { get; set; } = string.Empty;
    public string RealityStatement { get; set; } = string.Empty;
    public string ConsiderationStatement { get; set; } = string.Empty;
}
