// Mirrors GymCRM.IdentityAPI.Models.DTOs.InsertAccount 1:1.
// Body for POST /Authentication/Register.
export interface InsertAccount {
    email: string;
    password: string;
    accountType?: number;
    gymSubscriptionType?: number;
    gender?: number;
}
