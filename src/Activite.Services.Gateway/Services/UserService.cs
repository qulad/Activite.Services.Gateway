using System.Threading.Tasks;
using Activite.Services.Gateway.DTOs.User;
using Convey.CQRS.Queries;
using Convey.HTTP;

namespace Activite.Services.Gateway.Services;

public class UserService
{
    private const string AppleUserUrl = "/AppleUser";
    private const string GoogleUserUrl = "/GoogleUser";
    private const string UserUrl = "/User";

    private readonly string _webService1Url;
    private readonly IHttpClient _httpClient;

    public UserService(IHttpClient httpClient, HttpClientOptions options)
    {
        _httpClient = httpClient;
        _webService1Url = options.Services["user"];
    }

    public Task<PagedResult<AppleUserDto>> GetAppleUser(string email, string appleId)
    {
        return _httpClient.GetAsync<PagedResult<AppleUserDto>>($"{_webService1Url}{AppleUserUrl}?email={email}&appleId={appleId}");
    }

    public Task<PagedResult<GoogleUserDto>> GetGoogleUser(string email, string googleId)
    {
        return _httpClient.GetAsync<PagedResult<GoogleUserDto>>($"{_webService1Url}{GoogleUserUrl}?email={email}&googleId={googleId}");
    }

    public Task<PagedResult<UserDto>> GetUser(string email)
    {
        return _httpClient.GetAsync<PagedResult<UserDto>>($"{_webService1Url}{UserUrl}?email={email}");
    }
}