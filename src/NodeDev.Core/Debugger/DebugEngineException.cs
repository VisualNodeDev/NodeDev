namespace NodeDev.Core.Debugger;

/// <summary>
/// Exception thrown when a debug engine operation fails.
/// </summary>
public class DebugEngineException : Exception
{
    /// <summary>
    /// Initializes a new instance of DebugEngineException.
    /// </summary>
    public DebugEngineException()
    {
    }

    /// <summary>
    /// Initializes a new instance of DebugEngineException with a message.
    /// </summary>
    public DebugEngineException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of DebugEngineException with a message and inner exception.
    /// </summary>
    public DebugEngineException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
