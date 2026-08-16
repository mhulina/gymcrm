namespace GymCRM.BillingAPI.Models.Exceptions;

public class SubscriptionNotRenewableException : Exception
{
    public SubscriptionNotRenewableException(string message = "Cannot renew a subscription that has been cancelled or expired") : base(message) { }
    public SubscriptionNotRenewableException(string message, Exception innerException) : base(message, innerException) { }
}
