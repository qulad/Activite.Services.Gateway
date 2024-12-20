using System.Collections.Generic;

namespace Activite.Services.Gateway.Constants;

public static class UserTypes
{
    public const string AppleCustomer = nameof(AppleCustomer);

    public const string GoogleCustomer = nameof(GoogleCustomer);

    public const string GoogleLocation = nameof(GoogleLocation);

    public static HashSet<string> Customers => new()
    {
        AppleCustomer,
        GoogleCustomer
    };

    public static HashSet<string> Locations => new()
    {
        GoogleLocation
    };
}