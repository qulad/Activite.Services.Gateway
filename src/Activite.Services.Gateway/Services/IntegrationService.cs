using System.Threading.Tasks;
using Activite.Services.Gateway.DTOs.Integration;
using Convey.HTTP;

namespace Activite.Services.Gateway.Services;

public class IntegrationService
{
    private const string GoogleTokenUrl = "/Google/Token";

    private readonly string _webService1Url;
    private readonly IHttpClient _httpClient;

    public IntegrationService(IHttpClient httpClient, HttpClientOptions options)
    {
        _httpClient = httpClient;
        _webService1Url = options.Services["integration"];
    }

    public Task<GoogleTokenDto> GetGoogleTokenAsync(string accessToken)
    {
        return _httpClient.GetAsync<GoogleTokenDto>($"{_webService1Url}{GoogleTokenUrl}?accessToken={accessToken}");
    }
}