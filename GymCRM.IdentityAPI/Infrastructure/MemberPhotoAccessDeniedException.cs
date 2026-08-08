namespace GymCRM.IdentityAPI.Models;

public class MemberPhotoAccessDeniedException : Exception
{
    public MemberPhotoAccessDeniedException(string message = "You are not allowed to change this member's photo") : base(message) { }
    public MemberPhotoAccessDeniedException(string message, Exception innerException) : base(message, innerException) { }
}
