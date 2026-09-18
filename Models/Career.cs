namespace StudyMap.Models;

public class Career
{
	public string Id { get; set; } = string.Empty;
	
	public string Name { get; set; } = string.Empty;

	public string Description { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public string Icon { get; set; } = string.Empty;

	public string AccentColor { get; set; } = "#F6A64F";

	public bool IsPopular { get; set; }

	public string SalaryRangeIndia { get; set; } = string.Empty;

	public string FutureDemand { get; set; } = string.Empty;

	public string EducationPath { get; set; } = string.Empty;

	public List<string> SearchAliases { get; set; } = [];

	public List<string> EntranceExams { get; set; } = [];

	public List<string> KeySkills { get; set; } = [];

	public List<string> Workplaces { get; set; } = [];

	public List<string> RoadmapSteps { get; set; } = [];

	public List<string> SalaryInsights { get; set; } = [];

	public List<string> TopColleges { get; set; } = [];

	// Bonus features support
	public bool IsFavorite { get; set; }

	public bool IsRecentlyViewed { get; set; }

	public List<string> RelatedCareers { get; set; } = [];

	public string Duration { get; set; } = string.Empty;

	public string FutureScope { get; set; } = string.Empty;

	public List<string> RecommendedSubjects { get; set; } = [];

	// Career Discovery ("Not Sure What to Choose") recommendation matching
	public List<string> CareerCategories { get; set; } = [];

	public List<string> Interests { get; set; } = [];

	public List<string> PreferredSubjects { get; set; } = [];

	public List<string> WorkStyles { get; set; } = [];

	// Parent Insights — optional structured overrides.
	// When left empty, ParentInsightsService derives a sensible value from the fields above,
	// so existing careers.json entries keep working without any data migration.
	public int? SalaryMinLpa { get; set; }

	public int? SalaryMaxLpa { get; set; }

	public string SalaryDescription { get; set; } = string.Empty;

	public string StabilityLevel { get; set; } = string.Empty;

	public string StabilityDescription { get; set; } = string.Empty;

	public string FutureDemandLevel { get; set; } = string.Empty;

	public string FutureDemandDescription { get; set; } = string.Empty;

	public string DifficultyLevel { get; set; } = string.Empty;

	public string DifficultyDescription { get; set; } = string.Empty;

	public string CompetitionLevel { get; set; } = string.Empty;

	public string CompetitionDescription { get; set; } = string.Empty;

	public string TypicalUGDuration { get; set; } = string.Empty;

	public string EducationInvestment { get; set; } = string.Empty;

	public List<string> UGCourses { get; set; } = [];

	public List<string> PGCourses { get; set; } = [];

	public List<string> ParentConsiderations { get; set; } = [];

	public List<string> SuitableFor { get; set; } = [];

	public List<string> ThingsToConsider { get; set; } = [];
}