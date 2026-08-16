namespace GymCRM.BillingAPI.Models.Exceptions;

public class PaymentNotFoundException : Exception
{
    public PaymentNotFoundException(string message = "Payment was not found") : base(message) { }
    public PaymentNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}
