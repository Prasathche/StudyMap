using StudyMap.Models;

namespace StudyMap.Services;

/// <summary>
/// Builds parent-friendly, balanced career insights from the existing Career data.
/// Explicit values on <see cref="Career"/> are used when present; otherwise a
/// rule-based (no AI, no external data) derivation provides a sensible, structured default.
/// Business logic only — no UI concerns live here.
/// </summary>
public class ParentInsightsService
{
    private readonly CareersService _careersService;

    // Landing-page category chips mapped to the CareerCategories tags already stored per career.
    public static readonly (string Chip, string Tag)[] CategoryChips =
    [
        ("Technology", "Technology"),
        ("Healthcare", "Healthcare"),
        ("Engineering", "Engineering"),
        ("Finance", "Business & Finance"),
        ("Business", "Business & Finance"),
        ("Law", "Law & Public Service"),
        ("Design", "Design & Media"),
        ("Government", "Law & Public Service"),
        ("Science", "Science & Research"),
    ];

    public ParentInsightsService(CareersService? careersService = null)
    {
        _careersService = careersService ?? new CareersService();
    }

    // ── Career lookups (delegates to CareersService, no duplication) ──
    public Task<List<Career>> GetAllCareersAsync() => _careersService.GetAllCareersAsync();

    public Task<List<Career>> GetPopularCareersAsync() => _careersService.GetPopularCareersAsync();

    public Task<Career?> GetCareerByIdAsync(string careerId) => _careersService.GetCareerByIdAsync(careerId);

    public async Task<List<Career>> GetCareersByCategoryChipAsync(string chip)
    {
        var all = await _careersService.GetAllCareersAsync();
        if (string.IsNullOrWhiteSpace(chip) || chip == "All")
        {
            return all;
        }

        var tag = CategoryChips.FirstOrDefault(c => c.Chip == chip).Tag;
        if (string.IsNullOrEmpty(tag))
        {
            return all;
        }

        return all.Where(c => c.CareerCategories.Contains(tag)).ToList();
    }

    public async Task<List<Career>> SearchCareersAsync(string query)
    {
        var all = await _careersService.GetAllCareersAsync();
        if (string.IsNullOrWhiteSpace(query))
        {
            return all;
        }

        return all.Where(c =>
            c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.Category.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            c.CareerCategories.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    // ── Insight building ───────────────────────────────────────────────
    public CareerParentInsight BuildInsight(Career career)
    {
        var primaryTag = career.CareerCategories.FirstOrDefault() ?? career.Category;

        var stability = ResolveStability(career);
        var demand = ResolveFutureDemand(career, primaryTag);
        var difficulty = ResolveDifficulty(career);
        var competition = ResolveCompetition(career);
        var education = ResolveEducation(primaryTag);

        var insight = new CareerParentInsight
        {
            Career = career,

            SalaryRangeText = ResolveSalaryRangeText(career),
            SalaryDescription = FirstNonEmpty(career.SalaryDescription,
                "Typical range varies significantly based on experience, skills, company and location."),

            StabilityLevel = stability.Level,
            StabilityDescription = stability.Description,
            StabilityIndicator = IndicatorFor(stability.Level, ["Excellent", "Good"], ["Moderate", "Changing"], ["Limited"]),

            FutureDemandLevel = demand.Level,
            FutureDemandDescription = demand.Description,
            FutureDemandIndicator = IndicatorFor(demand.Level, ["High"], ["Moderate", "Emerging"], ["Declining", "Uncertain"]),

            DifficultyLevel = difficulty.Level,
            DifficultyDescription = difficulty.Description,
            DifficultyIndicator = IndicatorFor(difficulty.Level, ["Beginner Friendly"], ["Moderate"], ["Challenging", "Highly Competitive"]),

            CompetitionLevel = competition.Level,
            CompetitionDescription = competition.Description,
            CompetitionIndicator = IndicatorFor(competition.Level, ["Low"], ["Moderate"], ["High", "Very High"]),

            TypicalUGDuration = FirstNonEmpty(career.TypicalUGDuration, education.Duration),
            UGCourses = career.UGCourses.Count > 0 ? career.UGCourses : education.UgCourses,
            PGCourses = career.PGCourses.Count > 0 ? career.PGCourses : education.PgCourses,
            EntranceExams = career.EntranceExams,
        };

        insight.ParentConsiderations = career.ParentConsiderations.Count > 0
            ? career.ParentConsiderations
            : BuildParentConsiderations(career, insight);

        insight.SuitableFor = career.SuitableFor.Count > 0
            ? career.SuitableFor
            : BuildSuitableFor(career);

        insight.ThingsToConsider = career.ThingsToConsider.Count > 0
            ? career.ThingsToConsider
            : BuildThingsToConsider(career, insight);

        insight.OpportunityStatement = demand.Description;
        insight.RealityStatement = competition.Description;
        insight.ConsiderationStatement = insight.ThingsToConsider.FirstOrDefault()
            ?? "Continuous skill development is important in most careers.";

        return insight;
    }

    // ── Salary ───────────────────────────────────────────────────────
    private static string ResolveSalaryRangeText(Career career)
    {
        if (career.SalaryMinLpa is int min && career.SalaryMaxLpa is int max)
        {
            return $"₹{min}L – ₹{max}L / year";
        }

        var match = System.Text.RegularExpressions.Regex.Match(career.SalaryRangeIndia, @"(\d+)\s*-\s*(\d+)");
        if (match.Success)
        {
            return $"₹{match.Groups[1].Value}L – ₹{match.Groups[2].Value}L / year";
        }

        return string.IsNullOrWhiteSpace(career.SalaryRangeIndia) ? "Information unavailable" : career.SalaryRangeIndia;
    }

    // ── Stability ──────────────────────────────────────────────────────
    private static (string Level, string Description) ResolveStability(Career career)
    {
        if (!string.IsNullOrWhiteSpace(career.StabilityLevel))
        {
            return (career.StabilityLevel, FirstNonEmpty(career.StabilityDescription,
                "This field's stability can vary with industry trends and individual specialization."));
        }

        var level = career.FutureDemand.ToLowerInvariant() switch
        {
            "very high" => "Excellent",
            "high" => "Good",
            "steady" => "Moderate",
            _ => "Moderate"
        };

        var primaryTag = career.CareerCategories.FirstOrDefault() ?? string.Empty;
        var description = primaryTag switch
        {
            "Technology" => "Technology skills remain valuable across most industries, although specific tools and roles can change over time.",
            "Healthcare" => "Healthcare roles tend to stay in consistent demand, since patient care needs remain steady across economic cycles.",
            "Business & Finance" => "Business and finance roles are generally steady, though they can be sensitive to broader economic conditions.",
            "Engineering" => "Core engineering skills remain relevant, though demand can vary by sector and infrastructure investment.",
            "Design & Media" => "Design and media roles can be steady for skilled professionals, though trends and platforms shift often.",
            "Law & Public Service" => "Legal and public service roles tend to offer long-term stability once a position is secured.",
            "Science & Research" => "Research roles can be stable in established institutions, though funding availability varies.",
            "Education" => "Education-related roles tend to remain in consistent demand across regions.",
            _ => "This field remains an important part of most industries, although specific roles can change over time."
        };

        return (level, description);
    }

    // ── Future demand ────────────────────────────────────────────────
    private static (string Level, string Description) ResolveFutureDemand(Career career, string primaryTag)
    {
        if (!string.IsNullOrWhiteSpace(career.FutureDemandLevel))
        {
            return (career.FutureDemandLevel, FirstNonEmpty(career.FutureDemandDescription,
                "Demand can vary with industry trends, location and economic conditions."));
        }

        var level = career.FutureDemand.ToLowerInvariant() switch
        {
            "very high" => "High",
            "high" => "High",
            "steady" => "Moderate",
            _ => "Moderate"
        };

        var description = primaryTag switch
        {
            "Technology" => "Demand is expected to remain strong for professionals with current technical and problem-solving skills.",
            "Healthcare" => "Demand is expected to remain strong given consistent, ongoing patient care needs across India.",
            "Business & Finance" => "Demand remains steady for professionals who combine domain knowledge with analytical skills.",
            "Engineering" => "Demand is closely tied to infrastructure and industrial growth, and can vary by specialization.",
            "Design & Media" => "Demand is growing in digital-first roles, though it can be more project- and platform-dependent.",
            "Law & Public Service" => "Demand remains steady, particularly for specialized legal and administrative expertise.",
            "Science & Research" => "Demand depends on research funding and institutional growth, and can vary by specialization.",
            "Education" => "Demand remains steady given the consistent need for teaching and training across India.",
            _ => "Demand can vary with industry trends, location and economic conditions."
        };

        return (level, description);
    }

    // ── Difficulty ───────────────────────────────────────────────────
    private static readonly string[] HighlyCompetitiveExamKeywords =
        ["UPSC", "NEET", "JEE", "CLAT", "NDA", "CA Foundation", "CA Intermediate", "CA Final", "GATE", "AFCAT", "CDS", "AILET"];

    private static (string Level, string Description) ResolveDifficulty(Career career)
    {
        if (!string.IsNullOrWhiteSpace(career.DifficultyLevel))
        {
            return (career.DifficultyLevel, FirstNonEmpty(career.DifficultyDescription,
                "Requires consistent learning and skill development over time."));
        }

        var hasCompetitiveExam = career.EntranceExams.Any(exam =>
            HighlyCompetitiveExamKeywords.Any(keyword => exam.Contains(keyword, StringComparison.OrdinalIgnoreCase)));

        string level;
        if (hasCompetitiveExam)
        {
            level = "Highly Competitive";
        }
        else if (career.EntranceExams.Count > 0)
        {
            level = "Challenging";
        }
        else if (career.KeySkills.Count >= 3)
        {
            level = "Moderate";
        }
        else
        {
            level = "Beginner Friendly";
        }

        var topSkills = career.KeySkills.Take(2).ToList();
        var description = topSkills.Count > 0
            ? $"Requires consistent learning, {string.Join(" and ", topSkills.Select(s => s.ToLowerInvariant()))} development."
            : "Requires consistent learning and steady skill development.";

        return (level, description);
    }

    // ── Competition ──────────────────────────────────────────────────
    private static (string Level, string Description) ResolveCompetition(Career career)
    {
        if (!string.IsNullOrWhiteSpace(career.CompetitionLevel))
        {
            return (career.CompetitionLevel, FirstNonEmpty(career.CompetitionDescription,
                "Competition exists but is manageable with the right skills and preparation."));
        }

        var hasVeryCompetitiveExam = career.EntranceExams.Any(exam =>
            exam.Contains("UPSC", StringComparison.OrdinalIgnoreCase) ||
            exam.Contains("NEET-UG", StringComparison.OrdinalIgnoreCase) ||
            exam.Contains("CLAT", StringComparison.OrdinalIgnoreCase) ||
            exam.Contains("NDA", StringComparison.OrdinalIgnoreCase));

        string level = hasVeryCompetitiveExam
            ? "Very High"
            : career.IsPopular ? "High" : "Moderate";

        var description = level switch
        {
            "Very High" => "This field attracts a very large number of applicants relative to available seats or positions, so consistent preparation matters.",
            "High" => "There are many candidates entering this field, so specialization and practical skills can improve employability.",
            "Moderate" => "Competition exists but is generally manageable with the right skills and preparation.",
            _ => "This field currently has relatively fewer applicants, though opportunities can still vary by location."
        };

        return (level, description);
    }

    // ── Education investment (category-based, broad and India-specific) ──
    private static (string Duration, List<string> UgCourses, List<string> PgCourses) ResolveEducation(string primaryTag) =>
        primaryTag switch
        {
            "Technology" => ("3–4 years for UG", ["B.Tech / B.E.", "BCA", "B.Sc Computer Science"], ["M.Tech", "MCA", "MS"]),
            "Healthcare" => ("4–5.5 years for UG (varies by course)", ["MBBS", "BDS", "B.Sc Nursing", "B.Pharm", "BPT"], ["MD / MS", "MDS", "M.Pharm", "MPT"]),
            "Engineering" => ("4 years for UG", ["B.Tech / B.E."], ["M.Tech", "M.E."]),
            "Business & Finance" => ("3–4 years for UG", ["B.Com", "BBA", "BMS"], ["MBA", "M.Com", "CA / CS professional qualification"]),
            "Design & Media" => ("3–4 years for UG", ["B.Des", "BFA", "Journalism / Mass Communication degree"], ["M.Des", "MFA"]),
            "Law & Public Service" => ("3–5 years for UG (or graduation + exam preparation)", ["LLB / 5-year integrated law degree", "Any graduation stream"], ["LLM", "Public Administration diploma"]),
            "Science & Research" => ("3–4 years for UG", ["B.Sc", "B.Tech"], ["M.Sc", "PhD (for research roles)"]),
            "Education" => ("3–4 years for UG", ["B.A / B.Sc", "B.Ed"], ["M.A / M.Ed"]),
            _ => ("3–4 years for UG (varies by course)", ["Relevant undergraduate degree"], ["Relevant postgraduate specialization"])
        };

    // ── Dynamic, career-data-driven bullet lists ──────────────────────
    private static List<string> BuildParentConsiderations(Career career, CareerParentInsight insight)
    {
        var items = new List<string>();

        var interests = career.Interests.Take(2).Select(i => i.ToLowerInvariant()).ToList();
        items.Add(interests.Count > 0
            ? $"Does the student enjoy {string.Join(" and ", interests)}?"
            : "Does the student show genuine interest in this field, beyond the potential salary?");

        if (career.WorkStyles.Count > 0)
        {
            items.Add($"Is the student comfortable working {career.WorkStyles[0].ToLowerInvariant()}?");
        }

        var examSuffix = career.EntranceExams.Count > 0
            ? $", including preparation for exams like {string.Join(", ", career.EntranceExams.Take(2))}"
            : string.Empty;
        items.Add($"Can the family support a typical {insight.TypicalUGDuration.ToLowerInvariant()} education path{examSuffix}?");

        items.Add("Is the student interested in the actual day-to-day work, rather than only the salary potential?");

        var topSkill = career.KeySkills.FirstOrDefault();
        items.Add(topSkill is not null
            ? $"Are they willing to build practical skills (e.g., {topSkill.ToLowerInvariant()}) alongside their degree?"
            : "Are they willing to build practical skills alongside their degree?");

        if (insight.CompetitionLevel is "High" or "Very High")
        {
            items.Add("Are they prepared for a competitive selection process?");
        }

        return items;
    }

    private static List<string> BuildSuitableFor(Career career)
    {
        var items = new List<string>();

        items.AddRange(career.Interests.Select(i => $"Enjoys {i.ToLowerInvariant()}"));
        items.AddRange(career.PreferredSubjects.Select(s => $"Comfortable with {s}"));

        if (items.Count == 0)
        {
            items.Add("Enjoys exploring new subjects");
            items.Add("Is comfortable with continuous learning");
        }

        return items.Distinct().Take(6).ToList();
    }

    private static List<string> BuildThingsToConsider(Career career, CareerParentInsight insight)
    {
        var items = new List<string>
        {
            insight.DifficultyDescription,
            insight.CompetitionDescription,
            "Salary and growth vary significantly by skill, employer and location.",
            "A degree alone may not guarantee success — practical exposure and continuous learning matter."
        };

        if (insight.FutureDemandLevel is "Declining" or "Uncertain")
        {
            items.Add("This field's demand pattern may shift over time, so staying adaptable is useful.");
        }

        return items.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().Take(5).ToList();
    }

    // ── Small helpers ────────────────────────────────────────────────
    private static string FirstNonEmpty(string preferred, string fallback) =>
        string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;

    private static string IndicatorFor(string level, string[] positive, string[] moderate, string[] concern)
    {
        if (positive.Contains(level)) return "🟢";
        if (moderate.Contains(level)) return "🟠";
        if (concern.Contains(level)) return "🔴";
        return "⚪";
    }
}
