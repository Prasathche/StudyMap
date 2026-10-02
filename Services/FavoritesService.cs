using StudyMap.Models;

namespace StudyMap.Services;

/// <summary>
/// Manages favorite careers using in-memory storage with
/// optional persistence via Preferences for production use.
/// </summary>
public class FavoritesService
{
    private readonly HashSet<string> _favoriteIds = [];
    private const string PrefKey = "favorite_career_ids";

    public FavoritesService()
    {
        LoadFromPreferences();
    }

    public bool IsFavorite(string careerId) =>
        _favoriteIds.Contains(careerId);

    public bool Toggle(string careerId)
    {
        if (_favoriteIds.Contains(careerId))
        {
            _favoriteIds.Remove(careerId);
            SaveToPreferences();
            return false;
        }

        _favoriteIds.Add(careerId);
        SaveToPreferences();
        return true;
    }

    public void Add(string careerId)
    {
        _favoriteIds.Add(careerId);
        SaveToPreferences();
    }

    public void Remove(string careerId)
    {
        _favoriteIds.Remove(careerId);
        SaveToPreferences();
    }

    public IReadOnlySet<string> GetAll() => _favoriteIds;

    private void LoadFromPreferences()
    {
        try
        {
            var saved = Preferences.Default.Get(PrefKey, string.Empty);
            if (string.IsNullOrWhiteSpace(saved))
                return;

            foreach (var id in saved.Split(',', StringSplitOptions.RemoveEmptyEntries))
                _favoriteIds.Add(id.Trim());
        }
        catch
        {
            // Gracefully fall back to empty if preferences fail
        }
    }

    private void SaveToPreferences()
    {
        try
        {
            Preferences.Default.Set(PrefKey, string.Join(',', _favoriteIds));
        }
        catch
        {
            // Ignore save errors silently
        }
    }
}
