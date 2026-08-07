// Mirrors GymCRM.IdentityAPI.Models.DTOs.RefreshTokenRequest 1:1.
// Note: POST /Authentication/RefreshToken currently reads the refresh token
// from the httpOnly cookie and takes no request body, so this type is
// unused for now - modeled for completeness since the DTO exists.
export interface RefreshTokenRequest {
    refreshToken: string;
}
