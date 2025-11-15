using System.Net.Http.Headers;
using System.Text;

namespace NetZerion.Http;

/// <summary>
/// HTTP message handler that adds Basic Authentication to requests.
/// </summary>
public class AuthenticationHandler : DelegatingHandler
{
    private readonly string _apiKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationHandler"/> class.
    /// </summary>
    /// <param name="apiKey">Zerion API key.</param>
    public AuthenticationHandler(string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key cannot be null or empty.", nameof(apiKey));

        _apiKey = apiKey;
    }

    /// <summary>
    /// Sends an HTTP request with Basic Authentication header.
    /// </summary>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Add Basic Authentication header (API key as username, empty password)
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_apiKey}:"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        return await base.SendAsync(request, cancellationToken);
    }
}
