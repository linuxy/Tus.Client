using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Http;
using Tus.Client.Models;
using Tus.Client.Exceptions;

namespace Tus.Client;

/// <summary>
/// A client for the tus resumable upload protocol.
/// </summary>
public class TusClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;
    private bool _resumingEnabled;
    private ITusUrlStore? _urlStore;
    private bool _removeFingerprintOnSuccess;

    /// <summary>
    /// Initializes a new instance of the <see cref="TusClient"/> class.
    /// </summary>
    /// <param name="baseUrl">The base URL of the tus server.</param>
    /// <param name="httpClient">Optional HttpClient instance. If not provided, a new one will be created.</param>
    public TusClient(string baseUrl, HttpClient? httpClient = null)
    {
        if (string.IsNullOrEmpty(baseUrl))
        {
            throw new ArgumentNullException(nameof(baseUrl));
        }

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        _httpClient.DefaultRequestHeaders.Add("Tus-Resumable", "1.0.0");

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    /// <summary>
    /// Enables resuming of uploads using the specified URL store.
    /// </summary>
    /// <param name="urlStore">The URL store to use for storing and retrieving upload URLs.</param>
    public void EnableResuming(ITusUrlStore urlStore)
    {
        _resumingEnabled = true;
        _urlStore = urlStore;
    }

    /// <summary>
    /// Disables resuming of uploads.
    /// </summary>
    public void DisableResuming()
    {
        _resumingEnabled = false;
        _urlStore = null;
    }

    /// <summary>
    /// Gets a value indicating whether resuming is enabled.
    /// </summary>
    public bool ResumingEnabled => _resumingEnabled;

    /// <summary>
    /// Enables removing fingerprints after a successful upload.
    /// </summary>
    public void EnableRemoveFingerprintOnSuccess()
    {
        _removeFingerprintOnSuccess = true;
    }

    /// <summary>
    /// Disables removing fingerprints after a successful upload.
    /// </summary>
    public void DisableRemoveFingerprintOnSuccess()
    {
        _removeFingerprintOnSuccess = false;
    }

    /// <summary>
    /// Gets a value indicating whether fingerprints are removed after a successful upload.
    /// </summary>
    public bool RemoveFingerprintOnSuccessEnabled => _removeFingerprintOnSuccess;

    /// <summary>
    /// Creates a new upload.
    /// </summary>
    /// <param name="fileSize">The size of the file in bytes.</param>
    /// <param name="metadata">Optional metadata to associate with the upload.</param>
    /// <returns>The upload information.</returns>
    /// <exception cref="ProtocolException">Thrown when the server sends an unexpected response.</exception>
    public async Task<UploadInfo> CreateUploadAsync(long fileSize, Dictionary<string, string>? metadata = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "files");

        request.Headers.Add("Upload-Length", fileSize.ToString());

        if (metadata != null)
        {
            var metadataString = string.Join(",", metadata.Select(kvp => $"{kvp.Key} {Convert.ToBase64String(Encoding.UTF8.GetBytes(kvp.Value))}"));
            request.Headers.Add("Upload-Metadata", metadataString);
        }

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            var errMessage = await response.Content.ReadAsStringAsync();
            throw new ProtocolException($"Unexpected status code ({response.StatusCode}) while creating upload: {errMessage}", response);
        }

        var uploadUrl = response.Headers.Location?.ToString();
        if (string.IsNullOrEmpty(uploadUrl))
        {
            throw new ProtocolException("Missing upload URL in response for creating upload", response);
        }

        var uploadInfo = new UploadInfo
        {
            UploadUrl = uploadUrl,
            Size = fileSize,
            Metadata = metadata ?? new Dictionary<string, string>()
        };

        return uploadInfo;
    }

    /// <summary>
    /// Gets the current status of an upload.
    /// </summary>
    /// <param name="uploadUrl">The upload URL.</param>
    /// <returns>The upload information.</returns>
    /// <exception cref="ProtocolException">Thrown when the server sends an unexpected response.</exception>
    public async Task<UploadInfo> GetUploadStatusAsync(string uploadUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Head, uploadUrl);
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new ProtocolException($"Unexpected status code ({response.StatusCode}) while getting upload status", response);
        }

        var uploadLength = response.Headers.GetValues("Upload-Length").FirstOrDefault();
        var uploadOffset = response.Headers.GetValues("Upload-Offset").FirstOrDefault();

        if (string.IsNullOrEmpty(uploadLength))
        {
            throw new ProtocolException("Missing Upload-Length header in response", response);
        }

        if (string.IsNullOrEmpty(uploadOffset))
        {
            throw new ProtocolException("Missing Upload-Offset header in response", response);
        }

        var uploadInfo = new UploadInfo
        {
            UploadUrl = uploadUrl,
            Id = Path.GetFileName(uploadUrl),
            Size = long.Parse(uploadLength),
            Offset = long.Parse(uploadOffset)
        };

        if (response.Headers.TryGetValues("Upload-Metadata", out var metadataValues))
        {
            var metadata = metadataValues.First().Split(',')
                .Select(m => m.Split(' '))
                .ToDictionary(
                    m => m[0],
                    m => Encoding.UTF8.GetString(Convert.FromBase64String(m[1]))
                );
            uploadInfo.Metadata = metadata;
        }

        return uploadInfo;
    }

    /// <summary>
    /// Uploads a chunk of a file.
    /// </summary>
    /// <param name="uploadUrl">The upload URL.</param>
    /// <param name="data">The chunk data.</param>
    /// <param name="offset">The offset of the chunk in the file.</param>
    /// <returns>The new offset after the upload.</returns>
    /// <exception cref="ProtocolException">Thrown when the server sends an unexpected response.</exception>
    public async Task<long> UploadChunkAsync(string uploadUrl, byte[] data, long offset)
    {
        var request = new HttpRequestMessage(HttpMethod.Patch, uploadUrl);
        request.Headers.Add("Upload-Offset", offset.ToString());
        request.Content = new ByteArrayContent(data);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/offset+octet-stream");

        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new ProtocolException($"Unexpected status code ({response.StatusCode}) while uploading chunk", response);
        }

        var uploadOffset = response.Headers.GetValues("Upload-Offset").FirstOrDefault();
        if (string.IsNullOrEmpty(uploadOffset))
        {
            throw new ProtocolException("Missing Upload-Offset header in response", response);
        }

        return long.Parse(uploadOffset);
    }

    /// <summary>
    /// Uploads a file in chunks.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="chunkSize">The size of each chunk in bytes.</param>
    /// <param name="progress">Optional progress callback.</param>
    /// <returns>The upload information.</returns>
    /// <exception cref="ProtocolException">Thrown when the server sends an unexpected response.</exception>
    public async Task<UploadInfo> UploadFileAsync(string filePath, int chunkSize = 1024 * 1024, IProgress<double>? progress = null)
    {
        var fileInfo = new FileInfo(filePath);
        var metadata = new Dictionary<string, string>
        {
            ["filename"] = fileInfo.Name
        };

        var uploadInfo = await CreateUploadAsync(fileInfo.Length, metadata);
        
        // Store the fingerprint in the URL store if resuming is enabled
        if (_resumingEnabled && _urlStore != null)
        {
            var fingerprint = GetFingerprint(filePath);
            _urlStore.Set(fingerprint, uploadInfo.UploadUrl);
        }
        
        var buffer = new byte[chunkSize];
        var totalBytesRead = 0L;

        using var fileStream = File.OpenRead(filePath);
        while (totalBytesRead < fileInfo.Length)
        {
            var bytesRead = await fileStream.ReadAsync(buffer);
            if (bytesRead == 0) break;

            var chunk = new byte[bytesRead];
            Array.Copy(buffer, chunk, bytesRead);
            uploadInfo.Offset = await UploadChunkAsync(uploadInfo.UploadUrl, chunk, totalBytesRead);
            totalBytesRead += bytesRead;

            progress?.Report((double)totalBytesRead / fileInfo.Length);
        }

        var finalStatus = await GetUploadStatusAsync(uploadInfo.UploadUrl);
        
        // Handle fingerprint removal if enabled
        if (_resumingEnabled && _removeFingerprintOnSuccess && _urlStore != null)
        {
            var fingerprint = GetFingerprint(filePath);
            _urlStore.Remove(fingerprint);
        }

        return finalStatus;
    }

    /// <summary>
    /// Resumes an upload using a fingerprint.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="fingerprint">The fingerprint of the file.</param>
    /// <returns>The upload information.</returns>
    /// <exception cref="ResumingNotEnabledException">Thrown when resuming is not enabled.</exception>
    /// <exception cref="FingerprintNotFoundException">Thrown when the fingerprint is not found.</exception>
    /// <exception cref="ProtocolException">Thrown when the server sends an unexpected response.</exception>
    public async Task<UploadInfo> ResumeUploadAsync(string filePath, string fingerprint)
    {
        if (!_resumingEnabled)
        {
            throw new ResumingNotEnabledException();
        }

        if (_urlStore == null)
        {
            throw new ResumingNotEnabledException();
        }

        var uploadUrl = _urlStore.Get(fingerprint);
        if (uploadUrl == null)
        {
            throw new FingerprintNotFoundException(fingerprint);
        }

        return await BeginOrResumeUploadFromUrlAsync(filePath, uploadUrl);
    }

    /// <summary>
    /// Begins or resumes an upload from a URL.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="uploadUrl">The upload URL.</param>
    /// <returns>The upload information.</returns>
    /// <exception cref="ProtocolException">Thrown when the server sends an unexpected response.</exception>
    public async Task<UploadInfo> BeginOrResumeUploadFromUrlAsync(string filePath, string uploadUrl)
    {
        var request = new HttpRequestMessage(HttpMethod.Head, uploadUrl);
        var response = await _httpClient.SendAsync(request);
        
        if (!response.IsSuccessStatusCode)
        {
            throw new ProtocolException($"Unexpected status code ({response.StatusCode}) while resuming upload", response);
        }

        var uploadOffset = response.Headers.GetValues("Upload-Offset").FirstOrDefault();
        if (string.IsNullOrEmpty(uploadOffset))
        {
            throw new ProtocolException("Missing Upload-Offset header in response", response);
        }

        var offset = long.Parse(uploadOffset);
        var fileInfo = new FileInfo(filePath);
        
        if (offset >= fileInfo.Length)
        {
            // Upload is already complete
            return await GetUploadStatusAsync(uploadUrl);
        }

        // Continue uploading from the offset
        var buffer = new byte[1024 * 1024]; // 1MB chunks
        var totalBytesRead = offset;

        using var fileStream = File.OpenRead(filePath);
        fileStream.Position = offset; // Skip to the offset

        while (totalBytesRead < fileInfo.Length)
        {
            var bytesRead = await fileStream.ReadAsync(buffer);
            if (bytesRead == 0) break;

            var chunk = new byte[bytesRead];
            Array.Copy(buffer, chunk, bytesRead);
            offset = await UploadChunkAsync(uploadUrl, chunk, totalBytesRead);
            totalBytesRead += bytesRead;
        }

        var finalStatus = await GetUploadStatusAsync(uploadUrl);
        
        // Handle fingerprint removal if enabled
        if (_resumingEnabled && _removeFingerprintOnSuccess && _urlStore != null)
        {
            var fingerprint = GetFingerprint(filePath);
            _urlStore.Remove(fingerprint);
        }

        return finalStatus;
    }

    /// <summary>
    /// Resumes an upload or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <param name="fingerprint">The fingerprint of the file.</param>
    /// <returns>The upload information.</returns>
    /// <exception cref="ProtocolException">Thrown when the server sends an unexpected response.</exception>
    public async Task<UploadInfo> ResumeOrCreateUploadAsync(string filePath, string fingerprint)
    {
        try
        {
            return await ResumeUploadAsync(filePath, fingerprint);
        }
        catch (ResumingNotEnabledException)
        {
            // If resuming is not enabled, create a new upload
            return await UploadFileAsync(filePath);
        }
        catch (FingerprintNotFoundException)
        {
            // If the fingerprint is not found, create a new upload
            return await UploadFileAsync(filePath);
        }
        catch (ProtocolException ex) when (ex.Response?.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // If the server returns 404, create a new upload
            return await UploadFileAsync(filePath);
        }
    }

    /// <summary>
    /// Gets a fingerprint for a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The fingerprint.</returns>
    private string GetFingerprint(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        // Create a fingerprint based on the file path and size, similar to the Java implementation
        return $"{fileInfo.FullName}-{fileInfo.Length}";
    }

    /// <summary>
    /// Actions to be performed after a successful upload completion.
    /// Manages URL removal from the URL store if remove fingerprint on success is enabled.
    /// </summary>
    /// <param name="filePath">The path to the file that has been uploaded.</param>
    protected void UploadFinished(string filePath)
    {
        if (_resumingEnabled && _removeFingerprintOnSuccess && _urlStore != null)
        {
            var fingerprint = GetFingerprint(filePath);
            _urlStore.Remove(fingerprint);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="TusClient"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _httpClient.Dispose();
        }

        _disposed = true;
    }
} 