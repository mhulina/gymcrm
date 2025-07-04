namespace GymCRM.MembershipAPI.Models;

public class AccountAlreadyExistsException : Exception
{
    public AccountAlreadyExistsException(string message = "Account with that email already exists") : base(message) { }
    public AccountAlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }
}