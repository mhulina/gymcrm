namespace GymCRM.IdentityAPI.Models;

public class AdminAccountAlreadyExistsException : Exception
{
    public AdminAccountAlreadyExistsException(string message = "An admin account already exists") : base(message) { }
    public AdminAccountAlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }
}
