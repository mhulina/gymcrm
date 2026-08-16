using GymCRM.BillingAPI.Models.DTOs;
using GymCRM.BillingAPI.Models.Exceptions;

namespace GymCRM.BillingAPI.Services.Interface;

public interface ISubscriptionsService
{
    /// <summary>
    /// Creates a new, immediately-active subscription for a member, computing its
    /// <c>NextRenewalDate</c> from the given plan type. Admin-only.
    /// </summary>
    /// <param name="insertSubscription">The DTO containing the new subscription's details.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The newly created <see cref="Subscription"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="insertSubscription"/> is null.</exception>
    /// <exception cref="SubscriptionAccessDeniedException">Thrown if <paramref name="callerIsAdmin"/> is false.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<Subscription> CreateSubscriptionAsync(
        InsertSubscription insertSubscription,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves a single subscription by its unique identifier. Callable by the subscription's
    /// owning member or an Admin.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription.</param>
    /// <param name="callerAccountGuid">The caller's own Account ID in GymCRM.IdentityAPI.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The matching <see cref="Subscription"/>.</returns>
    /// <exception cref="SubscriptionNotFoundException">Thrown if no subscription with the given ID exists.</exception>
    /// <exception cref="SubscriptionAccessDeniedException">
    /// Thrown if the caller neither owns the subscription nor is an Admin.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<Subscription> GetSubscriptionByIdAsync(
        Guid subscriptionId,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves the member's currently active subscription, if any. Callable by the member
    /// themselves or an Admin.
    /// </summary>
    /// <param name="memberAccountGuid">The member's Account ID in GymCRM.IdentityAPI.</param>
    /// <param name="callerAccountGuid">The caller's own Account ID in GymCRM.IdentityAPI.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The member's active <see cref="Subscription"/>, or <c>null</c> if they don't have one.</returns>
    /// <exception cref="SubscriptionAccessDeniedException">
    /// Thrown if the caller is neither the member nor an Admin.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<Subscription?> GetActiveSubscriptionForMemberAsync(
        Guid memberAccountGuid,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Retrieves the full subscription history for a member. Callable by the member themselves
    /// or an Admin.
    /// </summary>
    /// <param name="memberAccountGuid">The member's Account ID in GymCRM.IdentityAPI.</param>
    /// <param name="callerAccountGuid">The caller's own Account ID in GymCRM.IdentityAPI.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>An enumerable collection of the member's <see cref="Subscription"/> records.</returns>
    /// <exception cref="SubscriptionAccessDeniedException">
    /// Thrown if the caller is neither the member nor an Admin.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<IEnumerable<Subscription>> GetSubscriptionsForMemberAsync(
        Guid memberAccountGuid,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Renews a subscription: sets its status back to Active and pushes its
    /// <c>NextRenewalDate</c> forward by one plan period from now. Admin-only.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription to renew.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The renewed <see cref="Subscription"/>.</returns>
    /// <exception cref="SubscriptionNotFoundException">Thrown if no subscription with the given ID exists.</exception>
    /// <exception cref="SubscriptionNotRenewableException">Thrown if the subscription is Cancelled or Expired.</exception>
    /// <exception cref="SubscriptionAccessDeniedException">Thrown if <paramref name="callerIsAdmin"/> is false.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<Subscription> RenewSubscriptionAsync(
        Guid subscriptionId,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Cancels a subscription: sets its status to Cancelled and clears its <c>NextRenewalDate</c>.
    /// Callable by the subscription's owning member or an Admin.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription to cancel.</param>
    /// <param name="callerAccountGuid">The caller's own Account ID in GymCRM.IdentityAPI.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The cancelled <see cref="Subscription"/>.</returns>
    /// <exception cref="SubscriptionNotFoundException">Thrown if no subscription with the given ID exists.</exception>
    /// <exception cref="SubscriptionAccessDeniedException">
    /// Thrown if the caller neither owns the subscription nor is an Admin.
    /// </exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<Subscription> CancelSubscriptionAsync(
        Guid subscriptionId,
        Guid callerAccountGuid,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Marks a subscription as PastDue, e.g. after a renewal payment attempt fails. Admin-only.
    /// </summary>
    /// <param name="subscriptionId">The unique identifier of the subscription.</param>
    /// <param name="callerIsAdmin">Whether the caller is an Admin.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>The updated <see cref="Subscription"/>.</returns>
    /// <exception cref="SubscriptionNotFoundException">Thrown if no subscription with the given ID exists.</exception>
    /// <exception cref="SubscriptionAccessDeniedException">Thrown if <paramref name="callerIsAdmin"/> is false.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    Task<Subscription> MarkSubscriptionPastDueAsync(
        Guid subscriptionId,
        bool callerIsAdmin,
        CancellationToken cancellationToken = default);
}
