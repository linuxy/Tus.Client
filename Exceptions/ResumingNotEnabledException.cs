namespace Tus.Client.Exceptions;

/// <summary>
/// This exception is thrown when you try to resume an upload without enabling resuming first.
/// </summary>
public class ResumingNotEnabledException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResumingNotEnabledException"/> class.
    /// </summary>
    public ResumingNotEnabledException() 
        : base("Resuming not enabled for this client. Use EnableResuming() to do so.")
    {
    }
} 