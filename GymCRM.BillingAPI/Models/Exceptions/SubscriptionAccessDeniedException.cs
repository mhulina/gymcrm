namespace GymCRM.BillingAPI.Models.Exceptions;

public class SubscriptionAccessDeniedException : Exception
{
    public SubscriptionAccessDeniedException(string message = "You are not allowed to access this subscription") : base(message) { }
    public SubscriptionAccessDeniedException(string message, Exception innerException) : base(message, innerException) { }
}
