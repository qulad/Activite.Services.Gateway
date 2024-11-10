using Activite.Services.Gateway.Constants;
using Activite.Services.Gateway.Contexts;
using Activite.Services.Gateway.DTOs;
using Activite.Services.Gateway.Options;
using Activite.Services.Gateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Activite.Services.Gateway.Middlewares;

public class TokenMiddleware
{
    private const string ErrorCode = "invalid_token";

    private static readonly JsonSerializerOptions _jsonSerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly GoogleTokenOptions _googleTokenOptions;
    private readonly IntegrationService _integrationService;
    private readonly ILogger<TokenMiddleware> _logger;

    public TokenMiddleware(
        RequestDelegate next,
        IOptions<GoogleTokenOptions> googleTokenOptionsAccessor,
        IntegrationService integrationService,
        ILogger<TokenMiddleware> logger)
    {
        _next = next;
        _googleTokenOptions = googleTokenOptionsAccessor.Value;
        _integrationService = integrationService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";

        if (!context.Request.Headers.TryGetValue("Authorization", out var header))
        {
            _logger.LogWarning("Header with key 'Authorization' is absent");

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = new ErrorResponseDto
            {
                ErrorCode = ErrorCode,
                ErrorMessage = "Header with key 'Authorization' is absent"
            };

            var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

            await context.Response.WriteAsync(responseBody);

            return;
        }

        var authorizationHeader = header.ToString();

        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            _logger.LogWarning("Authorization header value not found");

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = new ErrorResponseDto
            {
                ErrorCode = ErrorCode,
                ErrorMessage = "Authorization header value not found"
            };

            var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

            await context.Response.WriteAsync(responseBody);

            return;
        }

        var tokenList = authorizationHeader.ToString().Split(" ");

        if (tokenList.Length != 2)
        {
            _logger.LogWarning("Authorization header value is invalid");

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = new ErrorResponseDto
            {
                ErrorCode = ErrorCode,
                ErrorMessage = "Authorization header value is invalid"
            };

            var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

            await context.Response.WriteAsync(responseBody);

            return;
        }

        if (tokenList[0] != "Bearer")
        {
            _logger.LogWarning("Token type must be 'Bearer'");

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = new ErrorResponseDto
            {
                ErrorCode = ErrorCode,
                ErrorMessage = "Token type must be 'Bearer'"
            };

            var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

            await context.Response.WriteAsync(responseBody);

            return;
        }

        var jwtToken = tokenList[1];

        if (!context.Request.Headers.TryGetValue("Token-Provider", out var tokenProviderHeader))
        {
            _logger.LogWarning("Header with key 'Token-Provider' is absent");

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = new ErrorResponseDto
            {
                ErrorCode = ErrorCode,
                ErrorMessage = "Header with key 'Token-Provider' is absent"
            };

            var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

            await context.Response.WriteAsync(responseBody);

            return;
        }

        var tokenProvider = tokenProviderHeader.ToString();

        if (string.IsNullOrWhiteSpace(tokenProvider))
        {
            _logger.LogWarning("Token provider is invalid");

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = new ErrorResponseDto
            {
                ErrorCode = ErrorCode,
                ErrorMessage = "Token provider is invalid"
            };

            var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

            await context.Response.WriteAsync(responseBody);

            return;
        }

        if (tokenProvider is not ("Google" or "Apple"))
        {
            _logger.LogWarning("Current 'Token-Provider' is not supported");

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = new ErrorResponseDto
            {
                ErrorCode = ErrorCode,
                ErrorMessage = "Current 'Token-Provider' is not supported"
            };

            var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

            await context.Response.WriteAsync(responseBody);

            return;
        }

        var validation = tokenProvider switch
        {
            TokenProviders.Apple => await ValidateAppleTokenAsync(jwtToken),
            TokenProviders.Google => await ValidateGoogleTokenAsync(jwtToken),
            _ => new Validation
            {
                IsValid = false
            }
        };

        if (!validation.IsValid)
        {
            _logger.LogWarning("Token is unauthorized");
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

            var response = new ErrorResponseDto
            {
                ErrorCode = ErrorCode,
                ErrorMessage = "Token is unauthorized"
            };

            var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

            await context.Response.WriteAsync(responseBody);

            return;
        }

        var validationContext = new ValidationContext
        {
            TokenProvider = tokenProvider,
            UserId = validation.UserId,
            UserEmail = validation.UserEmail
        };

        context.Items.Add(nameof(ValidationContext), validationContext);

        await _next(context);
    }

    private static Task<Validation> ValidateAppleTokenAsync(string token)
    {
        throw new NotImplementedException();
    }

    private async Task<Validation> ValidateGoogleTokenAsync(string token)
    {
        var googleToken = await _integrationService.GetGoogleTokenAsync(token);

        if (googleToken is null)
        {
            _logger.LogError("Google token is null");

            return new Validation
            {
                IsValid = false
            };
        }

        if (!string.IsNullOrEmpty(googleToken.ErrorMessage))
        {
            _logger.LogError("Google token validation failed: {ErrorMessage}", googleToken.ErrorMessage);

            return new Validation
            {
                IsValid = false
            };
        }

        if (string.IsNullOrEmpty(googleToken.AuthorizedParty) ||
            string.IsNullOrEmpty(googleToken.Audience) ||
            string.IsNullOrEmpty(googleToken.Subject) ||
            string.IsNullOrEmpty(googleToken.Email))
        {
            _logger.LogError("Google token informatin is invalid");

            return new Validation
            {
                IsValid = false
            };
        }

        if (_googleTokenOptions.ClientId != googleToken.AuthorizedParty ||
            _googleTokenOptions.ClientId != googleToken.Audience)
        {
            _logger.LogError("Google token audience is invalid");

            return new Validation
            {
                IsValid = false
            };
        }

        return new Validation
        {
            IsValid = true,
            UserId = googleToken.Subject,
            UserEmail = googleToken.Email
        };
    }

    private sealed record Validation
    {
        public bool IsValid { get; set; }

        public string UserId { get; set; }

        public string UserEmail { get; set; }
    }
}
