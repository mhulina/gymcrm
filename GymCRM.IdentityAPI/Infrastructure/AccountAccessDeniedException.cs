namespace GymCRM.IdentityAPI.Models;

public class AccountAccessDeniedException : Exception
{
    public AccountAccessDeniedException(string message = "You are not allowed to create accounts on behalf of other users") : base(message) { }
    public AccountAccessDeniedException(string message, Exception innerException) : base(message, innerException) { }
}
