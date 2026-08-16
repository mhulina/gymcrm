import {axios} from "../../../shared/api/httpClient";
import {Member} from "../types/member";
import {AccountType} from "../types/accountType";
import {Gender} from "../types/gender";
import {InsertAccount} from "../types/insertAccount";
import {AuthenticationRequestBody} from "../types/authenticationRequestBody";
import {extractErrorMessage} from "../../../shared/api/extractErrorMessage";

export async function fetchUserInfoByGuid(): Promise<Member | null> {
    try {
        const response = await axios.get<Member>(`GetMe`)
        return response.data;
    } catch (error) {
        console.error("Error fetching user info: ", error);
        return null;
    }
}

export async function fetchMemberByEmail(email: string): Promise<Member | null> {
    try {
        const response = await axios.get<Member>(`GetUserByEmail/${encodeURIComponent(email)}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching member by email: ", error);
        return null;
    }
}

export async function fetchMemberByGuid(guid: string): Promise<Member | null> {
    try {
        const response = await axios.get<Member>(`GetUserByGuid/${guid}`);
        return response.data;
    } catch (error) {
        console.error("Error fetching member by guid: ", error);
        return null;
    }
}

export async function fetchAllMembers(): Promise<Member[]> {
    try {
        const response = await axios.get<Member[]>(`GetAllUsers`);
        return response.data;
    } catch (error) {
        console.error("Error fetching members: ", error);
        return [];
    }
}

export async function updateMember(member: Member): Promise<{ success: boolean; error?: string }> {
    try {
        const response = await axios.put<boolean>(`UpdateMember`, member);
        return response.data === true
            ? { success: true }
            : { success: false, error: "We couldn't save these changes." };
    } catch (error) {
        console.error("Error updating member: ", error);
        return { success: false, error: extractErrorMessage(error, "We couldn't save these changes.") };
    }
}

export async function uploadMemberPhoto(accountGuid: string, file: File): Promise<{ success: boolean; error?: string }> {
    try {
        const formData = new FormData();
        formData.append("file", file);
        await axios.post(`UploadPhoto/${accountGuid}`, formData, {
            headers: { "Content-Type": "multipart/form-data" },
        });
        return { success: true };
    } catch (error) {
        console.error("Error uploading member photo: ", error);
        return { success: false, error: extractErrorMessage(error, "We couldn't upload this photo.") };
    }
}

export async function deleteMemberPhoto(accountGuid: string): Promise<{ success: boolean; error?: string }> {
    try {
        await axios.delete(`DeletePhoto/${accountGuid}`);
        return { success: true };
    } catch (error) {
        console.error("Error deleting member photo: ", error);
        return { success: false, error: extractErrorMessage(error, "We couldn't remove this photo.") };
    }
}

// Fetches a member's photo as an authenticated blob and returns a local object URL - a
// plain <img src="...GetPhoto/..."> won't work here, since the auth cookies are
// SameSite=Lax and an <img> tag is a cross-origin *subresource* request in dev (different
// port), not a top-level navigation, so the cookie wouldn't reliably be sent. Going through
// this axios instance (withCredentials: true) is the same pattern already used for every
// other authenticated call in this app, just applied to an image instead of JSON.
// Callers own the returned URL and must URL.revokeObjectURL it when done (see usePhotoUrl).
export async function fetchMemberPhotoUrl(accountGuid: string): Promise<string | null> {
    try {
        const response = await axios.get(`GetPhoto/${accountGuid}`, { responseType: "blob" });
        return URL.createObjectURL(response.data as Blob);
    } catch (error) {
        console.error("Error fetching member photo: ", error);
        return null;
    }
}

// Creates the first Admin account (first-run setup) - raw fetch to AuthenticationController,
// matching registerAccount/handleLogin's pattern, since REACT_APP_ACCOUNTS_ENDPOINT is a
// different host/base path than the axios instance's REACT_APP_MEMBERS_ENDPOINT.
export async function setupAdminAccount(email: string, password: string): Promise<{ success: boolean; error?: string }> {
    try {
        const res = await fetch(
            process.env.REACT_APP_ACCOUNTS_ENDPOINT + "SetupAdminAccount", {
                headers: {"Content-Type": "application/json"},
                method: "POST",
                credentials: "include",
                body: JSON.stringify({
                    email: email.trim(),
                    password,
                    timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
                }),
            });

        if (!res.ok) {
            const error = await res.text();
            console.error("Admin setup failed:", error);
            return { success: false, error: error || "We couldn't create the admin account." };
        }

        return { success: true };
    } catch (error) {
        console.error("Error setting up admin account:", error);
        return { success: false, error: "We couldn't create the admin account." };
    }
}

export async function handleLogin(
    email: string,
    password: string,
    navigate: (path: string, options?: { replace?: boolean }) => void
) : Promise<{ success: boolean; mustChangePassword: boolean }> {
    const loginData: AuthenticationRequestBody = {username: email.trim(), password: password};

    try {
        const response = await fetch(
            process.env.REACT_APP_ACCOUNTS_ENDPOINT + "Login",{
                headers: {"Content-Type": "application/json"},
                method: "POST",
                credentials: "include",
                body: JSON.stringify(loginData)
            });

        if (response.ok) {
            const data = await response.json();
            const mustChangePassword = Boolean(data?.mustChangePassword);
            navigate(mustChangePassword ? "/change-password" : "/member/home", { replace: true });
            return { success: true, mustChangePassword };
        } else {
            const error = await response.text();
            console.error("Login failed: ", error);
            return { success: false, mustChangePassword: false };
        }
    }
    catch (error) {
        console.error("Error logging member in: ", error);
        return { success: false, mustChangePassword: false };
    }
}

export async function handleLogout(
    navigate: (path: string, options?: { replace?: boolean }) => void,
    setIsAuthenticated: (value: boolean) => void
) {
    try {
        const response = await fetch(
            process.env.REACT_APP_ACCOUNTS_ENDPOINT + "Logout",
            {
                method: "POST",
                credentials: "include",
                headers: { "Content-Type": "application/json" }
            }
        );

        if (response.ok) {
            setIsAuthenticated(false);
            navigate("/login", { replace: true });
        } else {
            console.error("Logout failed");
        }
    } catch (error) {
        console.error("Error logging out:", error);
        // Still redirect to login even if logout fails
        navigate("/login", { replace: true });
    }
}

// Raw POST /Authentication/Register call, no side effects beyond the request itself.
export async function registerAccount(insertAccount: InsertAccount): Promise<boolean> {
    try {
        const res = await fetch(
            process.env.REACT_APP_ACCOUNTS_ENDPOINT + "Register",{
                headers: {"Content-Type": "application/json"},
                method: "POST",
                credentials: "include",
                body: JSON.stringify(insertAccount)
            });

        if (!res.ok) {
            const error = await res.text();
            console.error("Registration failed:", error);
        }

        return res.ok;
    }
    catch (error) {
        console.error("Error registering account:", error);
        return false;
    }
}

// Raw POST /Authentication/AdminCreateAccount call - same shape as registerAccount, but
// authenticated (the caller must already be signed in as an Admin) and flags the resulting
// account MustChangePassword, since the password was assigned by the admin, not its owner.
export async function adminCreateAccount(insertAccount: InsertAccount): Promise<boolean> {
    try {
        const res = await fetch(
            process.env.REACT_APP_ACCOUNTS_ENDPOINT + "AdminCreateAccount",{
                headers: {"Content-Type": "application/json"},
                method: "POST",
                credentials: "include",
                body: JSON.stringify(insertAccount)
            });

        if (!res.ok) {
            const error = await res.text();
            console.error("Admin account creation failed:", error);
        }

        return res.ok;
    }
    catch (error) {
        console.error("Error creating account:", error);
        return false;
    }
}

// Changes the signed-in user's own password. On success the backend also clears
// MustChangePassword and reissues session cookies, so no separate re-login is needed here.
export async function changePassword(oldPassword: string, newPassword: string): Promise<{ success: boolean; error?: string }> {
    try {
        const res = await fetch(
            process.env.REACT_APP_ACCOUNTS_ENDPOINT + "ChangePassword", {
                headers: {"Content-Type": "application/json"},
                method: "POST",
                credentials: "include",
                body: JSON.stringify({ oldPassword, newPassword })
            });

        if (!res.ok) {
            const error = await res.text();
            console.error("Change password failed:", error);
            return { success: false, error: error || "We couldn't change your password." };
        }

        return { success: true };
    }
    catch (error) {
        console.error("Error changing password:", error);
        return { success: false, error: "We couldn't change your password." };
    }
}

// Public self-service sign-up: registers a standard Member account and logs them in.
export async function handleMemberRegistration(
    email: string,
    password: string,
    navigate: (path: string, options?: { replace?: boolean }) => void
) : Promise<boolean> {
    const success = await registerAccount({
        email: email.trim(),
        password: password,
        accountType: AccountType.Member,
        gender: Gender.Male,
        timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    });

    if (!success) {
        return false;
    }

    const result = await handleLogin(email, password, navigate);
    return result.success;
}

export interface AdminCreateMemberInput {
    insertAccount: InsertAccount;
    profile: Partial<Pick<Member,
        "firstName" | "middleName" | "lastName" | "phoneNumber" | "mobileNumber" |
        "workingExperienceInMonths" | "personalTrainerId">>;
}

// Admin "add new member" flow. There is no single backend endpoint that accepts
// a full profile on creation (InsertMember exists in MembersService but has no
// controller action), so this composes the three endpoints that do exist:
// AdminCreateAccount (creates the Account + a bare Member, flagged MustChangePassword) ->
// GetUserByEmail (fetch its AccountGuid) -> UpdateMember (fill in the rest of the profile).
export async function adminCreateMember(input: AdminCreateMemberInput): Promise<boolean> {
    const created = await adminCreateAccount(input.insertAccount);
    if (!created) {
        return false;
    }

    const member = await fetchMemberByEmail(input.insertAccount.email);
    if (!member) {
        console.error("Account was created but the new member record could not be found");
        return false;
    }

    const result = await updateMember({ ...member, ...input.profile });
    return result.success;
}
