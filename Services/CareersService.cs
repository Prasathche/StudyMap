using StudyMap.Models;
using System.Text.Json;

namespace StudyMap.Services;

public class CareersService
{
    // All pages create their own CareersService instance. Keep one shared async
    // load so the large career catalogue is parsed only once per app process.
    private static readonly Lazy<Task<List<Career>>> _careersTask =
        new(LoadCareersAsync, LazyThreadSafetyMode.ExecutionAndPublication);

    public Task<List<Career>> GetAllCareersAsync() => _careersTask.Value;

    private static async Task<List<Career>> LoadCareersAsync()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            await using var stream = await FileSystem.OpenAppPackageFileAsync("careers.json");
            var careers = await JsonSerializer.DeserializeAsync<List<Career>>(stream, options) ?? [];

            // ACCA is included here so it is available consistently across
            // Explore, Filter, Discover, Decide and the Career Roadmap.
            if (!careers.Any(c => c.Id.Equals("career-acca", StringComparison.OrdinalIgnoreCase)))
            {
                careers.Add(new Career
                {
                    Id = "career-acca",
                    Name = "ACCA (Chartered Certified Accountant)",
                    Description = "Build an international accounting and finance career through the ACCA professional qualification.",
                    Category = "Commerce",
                    Icon = "📊",
                    AccentColor = "#A8D8C8",
                    IsPopular = false,
                    SalaryRangeIndia = "INR 4-18 LPA",
                    FutureDemand = "High",
                    EducationPath = "Commerce is a strong foundation. After Class 12, check the current ACCA entry requirements, complete the required ACCA examinations and practical experience, and progress toward ACCA membership.",
                    EntranceExams = new List<string>(),
                    KeySkills = new List<string>
                    {
                        "Financial accounting",
                        "Financial reporting",
                        "Taxation",
                        "Audit and assurance"
                    },
                    Workplaces = new List<string>
                    {
                        "Accounting and audit firms",
                        "Banks and financial services companies",
                        "Multinational companies"
                    },
                    RoadmapSteps = new List<string>
                    {
                        "Choose Commerce in Classes 11-12 and build a strong foundation in Accountancy, Economics and Mathematics where available.",
                        "After Class 12, check the latest ACCA entry requirements and register through the appropriate route.",
                        "Complete the ACCA examination pathway while building practical experience and relevant accounting skills.",
                        "Progress toward ACCA membership and build experience in audit, accounting, taxation, financial reporting or finance roles."
                    },
                    SalaryInsights = new List<string>
                    {
                        "Entry-level accounting and finance roles vary by employer, location, qualification progress and experience.",
                        "ACCA can support progression into accounting, audit, taxation, financial reporting and broader finance roles.",
                        "International recognition can be useful for professionals targeting multinational or cross-border finance opportunities."
                    },
                    TopColleges = new List<string>
                    {
                        "ACCA-approved learning providers",
                        "Universities and colleges offering ACCA-integrated programmes"
                    },
                    CareerCategories = new List<string>
                    {
                        "Accounting",
                        "Finance",
                        "Professional Qualification"
                    },
                    Interests = new List<string>
                    {
                        "Working with numbers",
                        "Understanding business",
                        "Analysing financial information"
                    },
                    PreferredSubjects = new List<string>
                    {
                        "Accountancy",
                        "Economics",
                        "Mathematics"
                    },
                    WorkStyles = new List<string>
                    {
                        "Independently",
                        "Office / Finance"
                    }
                });
            }

            System.Diagnostics.Debug.WriteLine(
                $"[CareersService] Loaded shared career catalogue once: {careers.Count} careers");

            return careers;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CareersService] Error loading careers: {ex}");
            return [];
        }
    }

    public async Task<Career?> GetCareerByIdAsync(string careerId)
    {
        if (string.IsNullOrWhiteSpace(careerId))
            return null;

        var careers = await GetAllCareersAsync();
        return careers.FirstOrDefault(c =>
            c.Id.Equals(careerId, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<Career>> GetCareersByCategoryAsync(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            return [];

        var careers = await GetAllCareersAsync();
        return careers
            .Where(c => c.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task<List<Career>> GetPopularCareersAsync()
    {
        var careers = await GetAllCareersAsync();
        return careers.Where(c => c.IsPopular).ToList();
    }

    public async Task<List<string>> GetAllCategoriesAsync()
    {
        var careers = await GetAllCareersAsync();
        return careers
            .Select(c => c.Category)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c)
            .ToList();
    }
}