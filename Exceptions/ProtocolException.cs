using System.Net.Http;

namespace Tus.Client.Exceptions;

/// <summary>
/// Exception thrown when the tus server sends an unexpected response, such as wrong status codes or missing/invalid headers.
/// </summary>
public class ProtocolException : Exception
{
    /// <summary>
    /// Gets the HTTP response that caused the exception.
    /// </summary>
    public HttpResponseMessage? Response { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtocolException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="response">The HTTP response that caused the exception.</param>
    public ProtocolException(string message, HttpResponseMessage? response = null) 
        : base(message)
    {
        Response = response;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtocolException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    /// <param name="response">The HTTP response that caused the exception.</param>
    public ProtocolException(string message, Exception innerException, HttpResponseMessage? response = null) 
        : base(message, innerException)
    {
        Response = response;
    }
} 