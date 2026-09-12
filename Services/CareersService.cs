using StudyMap.Models;
using System.Text.Json;

namespace StudyMap.Services;

public class CareersService
{
    private List<Career> _careers = [];
    private bool _isLoaded = false;

    public async Task<List<Career>> GetAllCareersAsync()
    {
        if (_isLoaded)
            return _careers;

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("careers.json");
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _careers = JsonSerializer.Deserialize<List<Career>>(json, options) ?? [];
            _isLoaded = true;

            return _careers;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading careers: {ex.Message}");
            return [];
        }
    }

    public async Task<Career?> GetCareerByIdAsync(string careerId)
    {
        var careers = await GetAllCareersAsync();
        return careers.FirstOrDefault(c => c.Id == careerId);
    }

    public async Task<List<Career>> GetCareersByCategoryAsync(string category)
    {
        var careers = await GetAllCareersAsync();
        return careers.Where(c => c.Category == category).ToList();
    }

    public async Task<List<Career>> GetPopularCareersAsync()
    {
        var careers = await GetAllCareersAsync();
        return careers.Where(c => c.IsPopular).ToList();
    }

    public async Task<List<string>> GetAllCategoriesAsync()
    {
        var careers = await GetAllCareersAsync();
        return careers.Select(c => c.Category).Distinct().ToList();
    }
}
