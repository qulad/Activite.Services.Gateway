using System;

namespace Activite.Services.Gateway.DTOs.User;

public class UserDto
{
    public Guid Id { get; set; }

    public string Email { get; set; }

    public string PhoneNumber { get; set; }

    public string Region { get; set; }

    public string Type { get; set; }

    public bool TermsAndServicesAccepted { get; set; }

    public bool Verified { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }
}