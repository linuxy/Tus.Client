namespace Tus.Client;

/// <summary>
/// An in-memory implementation of the <see cref="ITusUrlStore"/> interface.
/// </summary>
/// <remarks>
/// This implementation stores the upload URLs in memory, so they will be lost when the application exits.
/// </remarks>
public class InMemoryTusUrlStore : ITusUrlStore
{
    private readonly Dictionary<string, string> _store = new();

    /// <inheritdoc/>
    public string? Get(string fingerprint)
    {
        return _store.TryGetValue(fingerprint, out var url) ? url : null;
    }

    /// <inheritdoc/>
    public void Set(string fingerprint, string url)
    {
        _store[fingerprint] = url;
    }

    /// <inheritdoc/>
    public void Remove(string fingerprint)
    {
        _store.Remove(fingerprint);
    }
} 