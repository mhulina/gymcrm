namespace GymCRM.IdentityAPI.Models;

public class PhotoTooLargeException : Exception
{
    public PhotoTooLargeException(string message) : base(message) { }
    public PhotoTooLargeException(string message, Exception innerException) : base(message, innerException) { }
}
