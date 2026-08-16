// Mirrors GymCRM.IdentityAPI.Models.DTOs.InsertMember 1:1.
// Note: MembersService.InsertMemberAsync accepts this shape, but no
// MembersController action calls it yet - there is no POST endpoint for it.
// Modeled for completeness; the admin "add member" flow currently goes
// through Register -> GetUserByEmail -> UpdateMember instead (see identityApi.ts).
export interface InsertMember {
    accountType: number;
    email: string;
    workingExperienceInMonths?: number;
}
