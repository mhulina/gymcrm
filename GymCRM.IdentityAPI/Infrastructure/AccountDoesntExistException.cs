namespace GymCRM.IdentityAPI.Models;

public class AccountDoesntExistException : Exception
{
    public AccountDoesntExistException(string message = "Account with that email does not exist") : base(message) { }
    public AccountDoesntExistException(string message, Exception innerException) : base(message, innerException) { }
}