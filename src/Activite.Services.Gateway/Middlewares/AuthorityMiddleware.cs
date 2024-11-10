using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Activite.Services.Gateway.Constants;
using Activite.Services.Gateway.Contexts;
using Activite.Services.Gateway.DTOs;
using Activite.Services.Gateway.DTOs.User;
using Activite.Services.Gateway.Services;
using Convey.CQRS.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Activite.Services.Gateway.Middlewares;

public class AuthorityMiddleware
{
    private const string ErrorCode = "no_authority";

    private static readonly JsonSerializerOptions _jsonSerializationOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly UserService _userService;
    private readonly ILogger<AuthorityMiddleware> _logger;

    public AuthorityMiddleware(
        RequestDelegate next,
        UserService userService,
        ILogger<AuthorityMiddleware> logger)
    {
        _next = next;
        _userService = userService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Items.TryGetValue(nameof(ValidationContext), out var validationContextObj) ||
            validationContextObj is not ValidationContext validationContext)
        {
            _logger.LogError("ValidationContext not found");

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = new ErrorResponseDto
            {
                ErrorCode = ErrorCode,
                ErrorMessage = "Validation context not found"
            };

            var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

            await context.Response.WriteAsync(responseBody);

            return;
        }

        var userId = Guid.Empty;

        if (validationContext.TokenProvider == TokenProviders.Google)
        {
            var users = await _userService.GetGoogleUser(validationContext.UserEmail, validationContext.UserId);

            if (users is not null && !users.IsEmpty)
            {
                if (users.TotalResults > 1)
                {
                    _logger.LogWarning("Total user count is more then one for {Email}", validationContext.UserEmail);
                }

                userId = users.Items.First().Id;
            }
        }
        else if (validationContext.TokenProvider == TokenProviders.Apple)
        {
            var users = await _userService.GetAppleUser(validationContext.UserEmail, validationContext.UserId);

            if (users is not null && !users.IsEmpty)
            {
                if (users.TotalResults > 1)
                {
                    _logger.LogWarning("Total user count is more then one for {Email}", validationContext.UserEmail);
                }

                userId = users.Items.First().Id;
            }
        }

        var isUserValid = userId != Guid.Empty;

        if (!isUserValid)
        {
            if (context.Request.Method == HttpMethods.Post && IsUrlUserCreateUrl(context))
            {
                await _next(context);

                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;

            var response = new ErrorResponseDto
            {
                ErrorCode = ErrorCode,
                ErrorMessage = $"User with email {validationContext.UserEmail} is invalid"
            };

            var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

            await context.Response.WriteAsync(responseBody);

            return;
        }

        if (context.Request.Method == HttpMethods.Get)
        {
            await _next(context);

            return;
        }

        try
        {
            using var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true);

            var requestBody = await ReadRequestBodyAsync(context);

            if (string.IsNullOrEmpty(requestBody))
            {
                using var stream = CreateDefaultBody(userId);

                context.Request.ContentLength = stream.Length;
                context.Request.Body = stream;

                await _next(context);

                return;
            }

            var jsonObject = JsonNode.Parse(
                requestBody,
                nodeOptions: new JsonNodeOptions()
                {
                    PropertyNameCaseInsensitive = true
                });

            if (jsonObject is null)
            {
                using var stream = CreateDefaultBody(userId);

                context.Request.ContentLength = stream.Length;
                context.Request.Body = stream;

                await _next(context);

                return;
            }

            if (jsonObject["userid"] is null)
            {
                using var stream = CreateDefaultBody(userId);

                context.Request.ContentLength = stream.Length;
                context.Request.Body = stream;

                await _next(context);

                return;
            }

            if (jsonObject["userid"].ToString() != userId.ToString())
            {
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;

                var response = new ErrorResponseDto
                {
                    ErrorCode = ErrorCode,
                    ErrorMessage = "You do not have access to this user"
                };

                var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

                await context.Response.WriteAsync(responseBody);

                return;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadGateway;

            var response = new ErrorResponseDto
            {
                ErrorCode = ErrorCode,
                ErrorMessage = $"An exception occured: {ex.Message}"
            };

            var responseBody = JsonSerializer.Serialize(response, _jsonSerializationOptions);

            await context.Response.WriteAsync(responseBody);

            return;
        }

        await _next(context);
    }

    private static async Task<string> ReadRequestBodyAsync(HttpContext context)
    {
        using var memoryStream = new MemoryStream();

        await context.Request.Body.CopyToAsync(memoryStream);

        memoryStream.Position = 0;

        using var reader = new StreamReader(memoryStream, Encoding.UTF8);

        var requestBody = await reader.ReadToEndAsync();

        memoryStream.Position = 0;

        context.Request.Body = memoryStream;

        return requestBody;
    }

    private static Stream CreateDefaultBody(Guid userId)
    {
        var body = new DefaultRequestBody
        {
            UserId = userId
        };

        var jsonBody = JsonSerializer.Serialize(body, _jsonSerializationOptions);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonBody))
        {
            Position = 0
        };

        return stream;
    }

    private static bool IsUrlUserCreateUrl(HttpContext context)
    {
        var url = context.Request.Path.ToString().ToLowerInvariant();

        return url == "/users/user" || url == "/users/googleuser" || url == "/users/appleuser";
    }
}

sealed file class DefaultRequestBody
{
    public Guid UserId { get; set; }
}