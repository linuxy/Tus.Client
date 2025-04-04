using System;
using System.Threading.Tasks;
using Tus.Client.Examples;

namespace Tus.Client.Examples;

/// <summary>
/// Main program for the Tus.Client examples.
/// </summary>
public class Program
{
    /// <summary>
    /// Main entry point for the examples.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static async Task Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: Tus.Client.Examples <filePath> <uploadUrl>");
            Console.WriteLine("   or: Tus.Client.Examples --batch <directoryPath> <uploadUrl> [batchSize] [maxFileSizeMB]");
            return;
        }

        if (args[0] == "--batch")
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: Tus.Client.Examples --batch <directoryPath> <uploadUrl> [batchSize] [maxFileSizeMB]");
                return;
            }

            var directoryPath = args[1];
            var uploadUrl = args[2];
            var batchSize = args.Length > 3 ? int.Parse(args[3]) : 100;
            var maxFileSizeMB = args.Length > 4 ? int.Parse(args[4]) : 10;
            var maxFileSize = maxFileSizeMB * 1024L * 1024L;

            Console.WriteLine("Tus.Client Examples - Batch Upload");
            Console.WriteLine("=================================");
            Console.WriteLine($"Directory: {directoryPath}");
            Console.WriteLine($"Upload URL: {uploadUrl}");
            Console.WriteLine($"Batch Size: {batchSize}");
            Console.WriteLine($"Max File Size: {maxFileSizeMB} MB");
            Console.WriteLine();

            // Run the batch upload example
            await BatchUploadExample.RunAsync(directoryPath, uploadUrl, batchSize, maxFileSize);
        }
        else
        {
            var filePath = args[0];
            var uploadUrl = args[1];

            Console.WriteLine("Tus.Client Examples");
            Console.WriteLine("==================");
            Console.WriteLine($"File: {filePath}");
            Console.WriteLine($"Upload URL: {uploadUrl}");
            Console.WriteLine();

            // Run the fingerprint example
            await FingerprintExample.RunAsync(filePath, uploadUrl);
            
            Console.WriteLine("\nPress any key to continue to the resumable upload example...");
            Console.ReadKey();
            
            // Run the resumable upload example
            await ResumableUploadExample.RunAsync(filePath, uploadUrl);
        }
    }
} 