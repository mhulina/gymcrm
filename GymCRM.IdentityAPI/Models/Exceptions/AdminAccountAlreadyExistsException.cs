namespace GymCRM.IdentityAPI.Models.Exceptions;

public class AdminAccountAlreadyExistsException : Exception
{
    public AdminAccountAlreadyExistsException(string message = "An admin account already exists") : base(message) { }
    public AdminAccountAlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }
}
