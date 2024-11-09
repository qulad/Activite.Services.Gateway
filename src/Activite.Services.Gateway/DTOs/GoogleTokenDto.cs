namespace Activite.Services.Gateway.DTOs;

public class GoogleTokenDto
{
    public string AuthorizedParty { get; set; }

    public string Audience { get; set; }

    public string Subject { get; set; }

    public string Scope { get; set; }

    public string Email { get; set; }

    public string ErrorMessage { get; set; }
}