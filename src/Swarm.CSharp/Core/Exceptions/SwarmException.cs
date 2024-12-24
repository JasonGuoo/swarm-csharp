using System;

namespace Swarm.CSharp.Core.Exceptions;

/// <summary>
/// Base exception class for all Swarm-related exceptions.
/// </summary>
public class SwarmException : Exception
{
    /// <summary>
    /// Gets the error code associated with this exception.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SwarmException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorCode">The error code.</param>
    /// <param name="innerException">The inner exception.</param>
    public SwarmException(string message, string errorCode = "SWARM_ERROR", Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
