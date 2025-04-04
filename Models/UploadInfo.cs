using System.Text.Json.Serialization;

namespace Tus.Client.Models;

/// <summary>
/// Represents information about a tus upload.
/// </summary>
public class UploadInfo
{
    /// <summary>
    /// Gets or sets the upload URL.
    /// </summary>
    [JsonPropertyName("uploadUrl")]
    public string UploadUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the upload ID.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>
    /// Gets or sets the offset of the upload in bytes.
    /// </summary>
    [JsonPropertyName("offset")]
    public long Offset { get; set; }

    /// <summary>
    /// Gets or sets the metadata associated with the upload.
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Gets or sets the creation date of the upload.
    /// </summary>
    [JsonPropertyName("creationDate")]
    public DateTime CreationDate { get; set; }

    /// <summary>
    /// Gets or sets the expiration date of the upload.
    /// </summary>
    [JsonPropertyName("expirationDate")]
    public DateTime? ExpirationDate { get; set; }
} 