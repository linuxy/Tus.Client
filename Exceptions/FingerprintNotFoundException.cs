namespace Tus.Client.Exceptions;

/// <summary>
/// This exception is thrown when no upload URL has been stored for a given fingerprint.
/// </summary>
public class FingerprintNotFoundException : Exception
{
    /// <summary>
    /// Gets the fingerprint that was not found.
    /// </summary>
    public string Fingerprint { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FingerprintNotFoundException"/> class.
    /// </summary>
    /// <param name="fingerprint">The fingerprint that was not found.</param>
    public FingerprintNotFoundException(string fingerprint) 
        : base($"Fingerprint not found in storage: {fingerprint}")
    {
        Fingerprint = fingerprint;
    }
} 