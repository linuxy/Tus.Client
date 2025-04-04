namespace Tus.Client;

/// <summary>
/// Interface for storing and retrieving upload URLs by fingerprint.
/// </summary>
public interface ITusUrlStore
{
    /// <summary>
    /// Gets the upload URL for a fingerprint.
    /// </summary>
    /// <param name="fingerprint">The fingerprint.</param>
    /// <returns>The upload URL, or null if not found.</returns>
    string? Get(string fingerprint);

    /// <summary>
    /// Sets the upload URL for a fingerprint.
    /// </summary>
    /// <param name="fingerprint">The fingerprint.</param>
    /// <param name="url">The upload URL.</param>
    void Set(string fingerprint, string url);

    /// <summary>
    /// Removes the upload URL for a fingerprint.
    /// </summary>
    /// <param name="fingerprint">The fingerprint.</param>
    void Remove(string fingerprint);
} 