namespace GymCRM.IdentityAPI.Models.Exceptions;

public class MemberAccessDeniedException : Exception
{
    public MemberAccessDeniedException(string message = "You are not allowed to update this member's profile") : base(message) { }
    public MemberAccessDeniedException(string message, Exception innerException) : base(message, innerException) { }
}
