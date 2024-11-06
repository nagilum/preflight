namespace Preflight;

public interface IOptions
{
    /// <summary>
    /// URL to request.
    /// </summary>
    Uri? Url { get; set; }
    
    /// <summary>
    /// HTTP method to ask for.
    /// </summary>
    string HttpMethod { get; set; }
    
    /// <summary>
    /// Headers to ask for.
    /// </summary>
    string? Headers { get; set; }
    
    /// <summary>
    /// Origin to ask for.
    /// </summary>
    Uri? Origin { get; set; }
}