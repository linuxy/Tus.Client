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
            return;
        }

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