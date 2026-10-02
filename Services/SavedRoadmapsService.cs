using System.Collections.Generic;

namespace StudyMap.Services;

/// <summary>
/// Persists the career IDs for roadmaps saved by the student.
/// </summary>
public class SavedRoadmapsService
{
    private readonly HashSet<string> _savedRoadmapIds = [];
    private const string PrefKey = "saved_roadmap_career_ids";

    public SavedRoadmapsService()
    {
        LoadFromPreferences();
    }

    public bool IsSaved(string careerId) =>
        !string.IsNullOrWhiteSpace(careerId) && _savedRoadmapIds.Contains(careerId);

    public void Add(string careerId)
    {
        if (string.IsNullOrWhiteSpace(careerId))
            return;

        _savedRoadmapIds.Add(careerId);
        SaveToPreferences();
    }

    public void Remove(string careerId)
    {
        if (string.IsNullOrWhiteSpace(careerId))
            return;

        _savedRoadmapIds.Remove(careerId);
        SaveToPreferences();
    }

    public IReadOnlySet<string> GetAll() => _savedRoadmapIds;

    private void LoadFromPreferences()
    {
        try
        {
            var saved = Preferences.Default.Get(PrefKey, string.Empty);
            if (string.IsNullOrWhiteSpace(saved))
                return;

            foreach (var id in saved.Split(',', StringSplitOptions.RemoveEmptyEntries))
                _savedRoadmapIds.Add(id.Trim());
        }
        catch
        {
            // Gracefully fall back to an empty set.
        }
    }

    private void SaveToPreferences()
    {
        try
        {
            Preferences.Default.Set(PrefKey, string.Join(',', _savedRoadmapIds));
        }
        catch
        {
            // Ignore persistence failures.
        }
    }
}
