namespace StudyMap.Services;

/// <summary>
/// Schedules a search after the user pauses typing. A new query cancels the
/// previous pending delay, so only the latest query is applied.
/// </summary>
public sealed class DebouncedSearchService
{
    public const int DefaultDelayMilliseconds = 350;

    private CancellationTokenSource? _pendingSearch;

    public void Schedule(string? query, Action<string> applySearch, int delayMilliseconds = DefaultDelayMilliseconds)
    {
        ArgumentNullException.ThrowIfNull(applySearch);

        Cancel();

        var cancellation = new CancellationTokenSource();
        _pendingSearch = cancellation;
        _ = RunAfterDelayAsync(query?.Trim() ?? string.Empty, applySearch, cancellation.Token, delayMilliseconds);
    }

    public void Cancel()
    {
        var pending = Interlocked.Exchange(ref _pendingSearch, null);
        if (pending is null) return;

        pending.Cancel();
        pending.Dispose();
    }

    private static async Task RunAfterDelayAsync(
        string query,
        Action<string> applySearch,
        CancellationToken cancellationToken,
        int delayMilliseconds)
    {
        try
        {
            await Task.Delay(delayMilliseconds, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (!cancellationToken.IsCancellationRequested)
                    applySearch(query);
            });
        }
        catch (OperationCanceledException)
        {
            // A newer query arrived before the debounce interval elapsed.
        }
    }
}
