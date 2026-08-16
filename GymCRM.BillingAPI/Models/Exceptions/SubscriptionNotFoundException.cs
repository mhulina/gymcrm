namespace GymCRM.BillingAPI.Models.Exceptions;

public class SubscriptionNotFoundException : Exception
{
    public SubscriptionNotFoundException(string message = "Subscription was not found") : base(message) { }
    public SubscriptionNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
