using GymCRM.BillingAPI.Models.DTOs;
using GymCRM.BillingAPI.Models.Exceptions;

namespace GymCRM.BillingAPI.Services.Interface;

public interface IPaymentsService
{
    /// <summary>
    /// Records a payment against a subscription. If the payment succeeded and the subscription
    /// was PastDue, the subscription is renewed and set back to Active as a side effect.
    /// Admin-only.
    /// </summary>
    /// <param name="insertPayment">The DTO containing the new payment's details.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The newly recorded <see cref="Payment"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="insertPayment"/> is null.</exception>
    /// <exception cref="SubscriptionAccessDeniedException">Thrown if <paramref name="callerIsAdmin"/> is false.</exception>
    /// <exception cref="SubscriptionNotFoundException">Thrown if the referenced subscription does not exist.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<Payment> RecordPaymentAsync(
        InsertPayment insertPayment,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves a single payment by its unique identifier. Callable by the owning member of the
    /// underlying subscription or an Admin.
    /// </summary>
    /// <param name="paymentId">The unique identifier of the payment.</param>
    /// <param name="callerAccountGuid">The caller's own Account ID in GymCRM.IdentityAPI.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The matching <see cref="Payment"/>.</returns>
    /// <exception cref="PaymentNotFoundException">Thrown if no payment with the given ID exists.</exception>
    /// <exception cref="SubscriptionNotFoundException">Thrown if the underlying subscription no longer exists.</exception>
    /// <exception cref="SubscriptionAccessDeniedException">
    /// Thrown if the caller neither owns the underlying subscription nor is an Admin.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<Payment> GetPaymentByIdAsync(
        Guid paymentId,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves the full payment history for a single subscription. Callable by the
    /// subscription's owning member or an Admin.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription.</param>
    /// <param name="callerAccountGuid">The caller's own Account ID in GymCRM.IdentityAPI.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An enumerable collection of the subscription's <see cref="Payment"/> records.</returns>
    /// <exception cref="SubscriptionNotFoundException">Thrown if no subscription with the given ID exists.</exception>
    /// <exception cref="SubscriptionAccessDeniedException">
    /// Thrown if the caller neither owns the subscription nor is an Admin.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<IEnumerable<Payment>> GetPaymentsForSubscriptionAsync(
        Guid subscriptionId,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves the full payment history for a member, across all of their subscriptions.
    /// Callable by the member themselves or an Admin.
    /// </summary>
    /// <param name="memberAccountGuid">The member's Account ID in GymCRM.IdentityAPI.</param>
    /// <param name="callerAccountGuid">The caller's own Account ID in GymCRM.IdentityAPI.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An enumerable collection of the member's <see cref="Payment"/> records.</returns>
    /// <exception cref="SubscriptionAccessDeniedException">
    /// Thrown if the caller is neither the member nor an Admin.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<IEnumerable<Payment>> GetPaymentsForMemberAsync(
        Guid memberAccountGuid,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Marks a payment as Refunded. Admin-only.
    /// </summary>
    /// <param name="paymentId">The unique identifier of the payment to refund.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The refunded <see cref="Payment"/>.</returns>
    /// <exception cref="PaymentNotFoundException">Thrown if no payment with the given ID exists.</exception>
    /// <exception cref="SubscriptionAccessDeniedException">Thrown if <paramref name="callerIsAdmin"/> is false.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<Payment> RefundPaymentAsync(
        Guid paymentId,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
}
