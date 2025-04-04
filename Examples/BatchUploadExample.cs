using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Tus.Client;

namespace Tus.Client.Examples;

/// <summary>
/// Example demonstrating batch uploading of multiple files using streaming gzip compression.
/// </summary>
public static class BatchUploadExample
{
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;
    private const int UploadDelayMs = 10;
    private const int ChunkSize = 1024 * 1024; // 1MB chunks

    /// <summary>
    /// Runs the batch upload example.
    /// </summary>
    /// <param name="directory">Path to the directory containing files to upload.</param>
    /// <param name="uploadUrl">The URL to upload the batch to.</param>
    /// <param name="batchSize">Maximum number of files to include in a single batch.</param>
    /// <param name="maxFileSize">Maximum file size in bytes to include in the batch.</param>
    public static async Task RunAsync(string directory, string uploadUrl, int batchSize = 100, long maxFileSize = 10 * 1024 * 1024)
    {
        Console.WriteLine("Batch Upload Example");
        Console.WriteLine("===================");
        Console.WriteLine($"Directory: {directory}");
        Console.WriteLine($"Upload URL: {uploadUrl}");
        Console.WriteLine($"Batch Size: {batchSize}");
        Console.WriteLine($"Max File Size: {maxFileSize / (1024 * 1024)} MB");
        Console.WriteLine();

        var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
            .Where(f => new FileInfo(f).Length <= maxFileSize)
            .ToList();

        Console.WriteLine($"Found {files.Count} files to upload.");
        Console.WriteLine();

        var client = new TusClient(uploadUrl);
        var urlStore = new InMemoryTusUrlStore();
        var failedUploads = new List<(string File, Exception Error)>();
        var successfulUploads = 0;

        for (var i = 0; i < files.Count; i += batchSize)
        {
            var batch = files.Skip(i).Take(batchSize).ToList();
            Console.WriteLine($"Processing batch {i / batchSize + 1} of {(files.Count + batchSize - 1) / batchSize} ({batch.Count} files)...");

            foreach (var file in batch)
            {
                var retryCount = 0;
                var success = false;

                while (retryCount < MaxRetries && !success)
                {
                    try
                    {
                        if (retryCount > 0)
                        {
                            Console.WriteLine($"Retrying upload of {Path.GetFileName(file)} (attempt {retryCount + 1}/{MaxRetries})...");
                            await Task.Delay(RetryDelayMs);
                        }

                        var progress = new Progress<double>(p => 
                        {
                            Console.Write($"\rUploading {Path.GetFileName(file)}: {p:P2}");
                        });

                        await client.UploadFileAsync(file, ChunkSize, progress);
                        Console.WriteLine(); // New line after progress
                        successfulUploads++;
                        success = true;

                        // Add a small delay between uploads to prevent overwhelming the server
                        await Task.Delay(UploadDelayMs);
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        if (retryCount >= MaxRetries)
                        {
                            Console.WriteLine($"\nFailed to upload {Path.GetFileName(file)} after {MaxRetries} attempts: {ex.Message}");
                            failedUploads.Add((file, ex));
                        }
                    }
                }
            }
        }

        Console.WriteLine("\nUpload Summary:");
        Console.WriteLine($"Total files: {files.Count}");
        Console.WriteLine($"Successful: {successfulUploads}");
        Console.WriteLine($"Failed: {failedUploads.Count}");

        if (failedUploads.Count > 0)
        {
            Console.WriteLine("\nFailed Uploads:");
            foreach (var (file, error) in failedUploads)
            {
                Console.WriteLine($"- {Path.GetFileName(file)}: {error.Message}");
            }
        }
    }

    /// <summary>
    /// Uploads a batch of files as a single compressed stream.
    /// </summary>
    private static async Task UploadBatchAsync(BatchManifest manifest, List<string> files, string baseDirectory, string uploadUrl)
    {
        // Create a memory stream to hold the compressed data
        using var memoryStream = new MemoryStream();
        
        // Create a gzip stream to compress the data
        using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal, true))
        {
            // Write the manifest as JSON at the beginning of the stream
            var manifestJson = JsonSerializer.Serialize(manifest);
            var manifestBytes = Encoding.UTF8.GetBytes(manifestJson);
            
            // Write the manifest length as a 4-byte integer
            var manifestLengthBytes = BitConverter.GetBytes(manifestBytes.Length);
            await gzipStream.WriteAsync(manifestLengthBytes, 0, manifestLengthBytes.Length);
            
            // Write the manifest JSON
            await gzipStream.WriteAsync(manifestBytes, 0, manifestBytes.Length);
            
            // Write each file
            foreach (var file in files)
            {
                // Write the file path length as a 4-byte integer
                var relativePath = Path.GetRelativePath(baseDirectory, file);
                var pathBytes = Encoding.UTF8.GetBytes(relativePath);
                var pathLengthBytes = BitConverter.GetBytes(pathBytes.Length);
                await gzipStream.WriteAsync(pathLengthBytes, 0, pathLengthBytes.Length);
                
                // Write the file path
                await gzipStream.WriteAsync(pathBytes, 0, pathBytes.Length);
                
                // Write the file size as an 8-byte integer
                var fileSize = new FileInfo(file).Length;
                var fileSizeBytes = BitConverter.GetBytes(fileSize);
                await gzipStream.WriteAsync(fileSizeBytes, 0, fileSizeBytes.Length);
                
                // Write the file content
                using var fileStream = File.OpenRead(file);
                await fileStream.CopyToAsync(gzipStream);
            }
        }
        
        // Reset the memory stream position
        memoryStream.Position = 0;
        
        // Create an HTTP client
        using var client = new HttpClient();
        
        // Create a multipart form data content
        using var content = new MultipartFormDataContent();
        
        // Add the batch ID as a form field
        content.Add(new StringContent(manifest.BatchId), "batchId");
        
        // Add the compressed data as a file
        var streamContent = new StreamContent(memoryStream);
        streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/gzip");
        content.Add(streamContent, "batch", "batch.gz");
        
        // Upload the batch
        var response = await client.PostAsync(uploadUrl, content);
        
        // Check if the upload was successful
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to upload batch: {response.StatusCode} - {errorMessage}");
        }
        
        Console.WriteLine($"Batch {manifest.BatchId} uploaded successfully.");
    }
}

/// <summary>
/// Represents a batch manifest containing information about the files in the batch.
/// </summary>
public class BatchManifest
{
    /// <summary>
    /// Gets or sets the unique identifier for the batch.
    /// </summary>
    public string BatchId { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the list of files in the batch.
    /// </summary>
    public List<BatchFileInfo> Files { get; set; } = new List<BatchFileInfo>();
}

/// <summary>
/// Represents information about a file in a batch.
/// </summary>
public class BatchFileInfo
{
    /// <summary>
    /// Gets or sets the relative path of the file.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// Gets or sets the last modified date and time of the file.
    /// </summary>
    public DateTime LastModified { get; set; }
} 