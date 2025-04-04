using System;
using System.Threading.Tasks;
using Tus.Client;
using Tus.Client.Exceptions;

namespace Tus.Client.Examples;

/// <summary>
/// Example demonstrating the use of fingerprints for resumable uploads.
/// </summary>
public static class FingerprintExample
{
    /// <summary>
    /// Runs the fingerprint example.
    /// </summary>
    /// <param name="filePath">The path to the file to upload.</param>
    /// <param name="uploadUrl">The URL of the tus server.</param>
    public static async Task RunAsync(string filePath, string uploadUrl)
    {
        Console.WriteLine("Starting fingerprint example...");
        
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
            // Upload the file
            Console.WriteLine($"Uploading file: {filePath}");
            var uploadInfo = await client.UploadFileAsync(filePath, progress: new Progress<double>(p => 
                Console.WriteLine($"Upload progress: {p:P2}")));
            
            Console.WriteLine($"Upload completed. Upload URL: {uploadInfo.UploadUrl}");
            
            // Simulate an interruption and resuming the upload
            Console.WriteLine("\nSimulating an interruption...");
            
            // Get the fingerprint for the file
            var fingerprint = GetFingerprint(filePath);
            Console.WriteLine($"File fingerprint: {fingerprint}");
            
            // Try to resume the upload
            Console.WriteLine("Attempting to resume the upload...");
            try
            {
                var resumeInfo = await client.ResumeUploadAsync(filePath, fingerprint);
                Console.WriteLine($"Resume successful. Upload URL: {resumeInfo.UploadUrl}");
            }
            catch (FingerprintNotFoundException)
            {
                Console.WriteLine("Fingerprint not found in store. This is expected if the upload was completed successfully.");
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