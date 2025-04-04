using System;
using System.IO;
using System.Threading.Tasks;
using Tus.Client;
using Tus.Client.Exceptions;

namespace Tus.Client.Examples;

/// <summary>
/// Example demonstrating resumable uploads with fingerprints.
/// </summary>
public static class ResumableUploadExample
{
    /// <summary>
    /// Runs the resumable upload example.
    /// </summary>
    /// <param name="filePath">The path to the file to upload.</param>
    /// <param name="uploadUrl">The URL of the tus server.</param>
    public static async Task RunAsync(string filePath, string uploadUrl)
    {
        Console.WriteLine("Starting resumable upload example...");
        
        // Create a client
        using var client = new TusClient(uploadUrl);
        
        // Create an in-memory URL store
        var urlStore = new InMemoryTusUrlStore();
        
        // Enable resuming with the URL store
        client.EnableResuming(urlStore);
        
        // Enable removing fingerprints after successful uploads
        client.EnableRemoveFingerprintOnSuccess();
        
        try
        {
            // Example 1: Upload a file with progress tracking
            Console.WriteLine("\nExample 1: Upload a file with progress tracking");
            Console.WriteLine($"Uploading file: {filePath}");
            
            var progress = new Progress<double>(p => Console.WriteLine($"Upload progress: {p:P2}"));
            var uploadInfo = await client.UploadFileAsync(filePath, progress: progress);
            
            Console.WriteLine($"Upload completed. Upload URL: {uploadInfo.UploadUrl}");
            
            // Example 2: Try to resume the upload (should fail because it was completed)
            Console.WriteLine("\nExample 2: Try to resume the upload (should fail because it was completed)");
            
            var fingerprint = GetFingerprint(filePath);
            Console.WriteLine($"File fingerprint: {fingerprint}");
            
            try
            {
                var resumeInfo = await client.ResumeUploadAsync(filePath, fingerprint);
                Console.WriteLine($"Resume successful. Upload URL: {resumeInfo.UploadUrl}");
            }
            catch (FingerprintNotFoundException)
            {
                Console.WriteLine("Fingerprint not found in store. This is expected if the upload was completed successfully.");
            }
            
            // Example 3: Create a new upload and upload chunks manually
            Console.WriteLine("\nExample 3: Create a new upload and upload chunks manually");
            
            // Create a temporary file for this example
            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "This is a test file for the resumable upload example.");
            
            try
            {
                var fileInfo = new FileInfo(tempFilePath);
                var tempFingerprint = GetFingerprint(tempFilePath);
                
                Console.WriteLine($"Created temporary file: {tempFilePath}");
                Console.WriteLine($"File fingerprint: {tempFingerprint}");
                
                // Create a new upload
                var newUploadInfo = await client.CreateUploadAsync(fileInfo.Length, new Dictionary<string, string>
                {
                    ["filename"] = fileInfo.Name
                });
                
                Console.WriteLine($"Created new upload. Upload URL: {newUploadInfo.UploadUrl}");
                
                // Upload the file in chunks
                var chunkSize = 1024; // Small chunks for demonstration
                var buffer = new byte[chunkSize];
                var totalBytesRead = 0L;
                
                using var fileStream = File.OpenRead(tempFilePath);
                while (totalBytesRead < fileInfo.Length)
                {
                    var bytesRead = await fileStream.ReadAsync(buffer);
                    if (bytesRead == 0) break;
                    
                    var chunk = new byte[bytesRead];
                    Array.Copy(buffer, chunk, bytesRead);
                    newUploadInfo.Offset = await client.UploadChunkAsync(newUploadInfo.UploadUrl, chunk, totalBytesRead);
                    totalBytesRead += bytesRead;
                    
                    Console.WriteLine($"Uploaded {totalBytesRead:N0} of {fileInfo.Length:N0} bytes");
                }
                
                // Get final upload status
                var finalStatus = await client.GetUploadStatusAsync(newUploadInfo.UploadUrl);
                Console.WriteLine($"Upload completed. Final offset: {finalStatus.Offset:N0}");
            }
            finally
            {
                // Clean up the temporary file
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
            
            // Example 4: Demonstrate ResumeOrCreateUploadAsync
            Console.WriteLine("\nExample 4: Demonstrate ResumeOrCreateUploadAsync");
            
            // Create another temporary file
            var tempFilePath2 = Path.GetTempFileName();
            File.WriteAllText(tempFilePath2, "This is another test file for the resumable upload example.");
            
            try
            {
                var fileInfo = new FileInfo(tempFilePath2);
                var tempFingerprint = GetFingerprint(tempFilePath2);
                
                Console.WriteLine($"Created temporary file: {tempFilePath2}");
                Console.WriteLine($"File fingerprint: {tempFingerprint}");
                
                // Try to resume or create a new upload
                var resumeOrCreateInfo = await client.ResumeOrCreateUploadAsync(tempFilePath2, tempFingerprint);
                
                Console.WriteLine($"Resume or create successful. Upload URL: {resumeOrCreateInfo.UploadUrl}");
                
                // Upload the file
                var uploadResult = await client.UploadFileAsync(tempFilePath2, progress: progress);
                
                Console.WriteLine($"Upload completed. Upload URL: {uploadResult.UploadUrl}");
            }
            finally
            {
                // Clean up the temporary file
                if (File.Exists(tempFilePath2))
                {
                    File.Delete(tempFilePath2);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Gets a fingerprint for a file.
    /// </summary>
    /// <param name="filePath">The path to the file.</param>
    /// <returns>The fingerprint.</returns>
    private static string GetFingerprint(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        return $"{fileInfo.FullName}-{fileInfo.Length}";
    }
} 