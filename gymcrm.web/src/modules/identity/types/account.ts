// Mirrors GymCRM.IdentityAPI.Models.DTOs.Account 1:1.
// Note: not currently returned or accepted by any controller action -
// modeled for completeness since the DTO exists.
export interface Account {
    guid?: string;
    email: string;
    password: string;
    accountType?: number;
    gymSubscriptionType?: number;
    gender?: number;
}
