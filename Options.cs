namespace Preflight;

public class Options : IOptions
{
    /// <summary>
    /// <inheritdoc cref="IOptions.Url"/>
    /// </summary>
    public Uri? Url { get; set; }

    /// <summary>
    /// <inheritdoc cref="IOptions.HttpMethod"/>
    /// </summary>
    public string HttpMethod { get; set; } = "GET";
    
    /// <summary>
    /// <inheritdoc cref="IOptions.Headers"/>
    /// </summary>
    public string? Headers { get; set; }
    
    /// <summary>
    /// <inheritdoc cref="IOptions.Origin"/>
    /// </summary>
    public Uri? Origin { get; set; }
}