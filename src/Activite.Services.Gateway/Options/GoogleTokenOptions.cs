namespace Activite.Services.Gateway.Options;

public class GoogleTokenOptions
{
    public const string SectionName = "GoogleToken";

    public string ClientId { get; set; }

    public string ProjectId { get; set; }
}