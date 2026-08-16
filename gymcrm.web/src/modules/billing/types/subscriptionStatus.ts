// Mirrors GymCRM.BillingAPI.Models.Enums.SubscriptionStatus 1:1. Wired as a raw int on the wire.
export enum SubscriptionStatus {
    Active,
    PastDue,
    Cancelled,
    Expired
}
