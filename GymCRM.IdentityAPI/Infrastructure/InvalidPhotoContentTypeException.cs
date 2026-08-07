namespace GymCRM.IdentityAPI.Models;

public class InvalidPhotoContentTypeException : Exception
{
    public InvalidPhotoContentTypeException(string message) : base(message) { }
    public InvalidPhotoContentTypeException(string message, Exception innerException) : base(message, innerException) { }
}
