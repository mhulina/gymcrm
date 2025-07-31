namespace GymCRM.IdentityAPI.Tests;

public class AuthenticationOptions
{
    public string SecretForKey { get; set; }
    public string Issuer { get; set; }
    public string Audience { get; set; }
}