namespace StudyMap.Services;

/// <summary>
/// Tracks recently viewed career IDs, capped at a configurable maximum.
/// Uses a LinkedList for O(1) front insertion and tail trimming.
/// </summary>
public class RecentlyViewedService
{
    private readonly LinkedList<string> _recentIds = new();
    private const int MaxItems = 10;
    private const string PrefKey = "recently_viewed_career_ids";

    public RecentlyViewedService()
    {
        LoadFromPreferences();
    }

    public void Record(string careerId)
    {
        // Remove existing occurrence so we can promote it to the front
        _recentIds.Remove(careerId);
        _recentIds.AddFirst(careerId);

        // Trim to capacity
        while (_recentIds.Count > MaxItems)
            _recentIds.RemoveLast();

        SaveToPreferences();
    }

    public IReadOnlyList<string> GetAll() => [.. _recentIds];

    public bool Contains(string careerId) => _recentIds.Contains(careerId);

    public void Clear()
    {
        _recentIds.Clear();
        SaveToPreferences();
    }

    private void LoadFromPreferences()
    {
        try
        {
            var saved = Preferences.Default.Get(PrefKey, string.Empty);
            if (string.IsNullOrWhiteSpace(saved))
                return;

            foreach (var id in saved.Split(',', StringSplitOptions.RemoveEmptyEntries))
                _recentIds.AddLast(id.Trim());
        }
        catch
        {
            // Gracefully ignore
        }
    }

    private void SaveToPreferences()
    {
        try
        {
            Preferences.Default.Set(PrefKey, string.Join(',', _recentIds));
        }
        catch
        {
            // Ignore silently
        }
    }
}
