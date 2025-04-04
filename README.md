# Tus.Client

A Native AOT compatible C# client for the tus resumable upload protocol.

## Features

- Implements the tus v1.0.0 protocol
- Supports resumable uploads
- Native AOT compatible
- Configurable chunk size
- Progress tracking
- Metadata support
- URL storage for resuming uploads

## Installation

```bash
dotnet add package Tus.Client
```

## Usage

```csharp
// Create a client
var client = new TusClient("http://your-tus-server.com");

// Enable resuming (optional)
var urlStore = new InMemoryTusUrlStore();
client.EnableResuming(urlStore);

// Upload a file with progress tracking
var progress = new Progress<double>(p => Console.WriteLine($"Upload progress: {p:P2}"));
var filePath = "example.txt";
var fingerprint = filePath; // In a real app, you would compute a hash of the file

try
{
    // Try to resume an upload, or create a new one if it doesn't exist
    var uploadInfo = await client.ResumeOrCreateUploadAsync(filePath, fingerprint);
    Console.WriteLine($"Upload completed. Final offset: {uploadInfo.Offset:N0}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

## License

This project is licensed under the MIT License - see the LICENSE file for details. 