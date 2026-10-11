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