import {axios} from "../../../shared/api/httpClient";
import {Member} from "../types/member";
import {GymSubscriptionType} from "../types/gymSubscriptionType";
import {AccountType} from "../types/accountType";
import {Gender} from "../types/gender";
import {InsertAccount} from "../types/insertAccount";
import {AuthenticationRequestBody} from "../types/authenticationRequestBody";

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

export async function updateMember(member: Member): Promise<boolean> {
    try {
        const response = await axios.put<boolean>(`UpdateMember`, member);
        return response.data === true;
    } catch (error) {
        console.error("Error updating member: ", error);
        return false;
    }
}

export async function handleLogin(
    email: string,
    password: string,
    navigate: (path: string, options?: { replace?: boolean }) => void
) : Promise<boolean> {
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
            navigate("/member/home", { replace: true });
            return true;
        } else {
            const error = await response.text();
            console.error("Login failed: ", error);
            return false;
        }
    }
    catch (error) {
        console.error("Error logging member in: ", error);
        return false;
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
        gymSubscriptionType: GymSubscriptionType.Monthly,
        gender: Gender.Male,
        timeZone: Intl.DateTimeFormat().resolvedOptions().timeZone,
    });

    if (!success) {
        return false;
    }

    return handleLogin(email, password, navigate);
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
// Register (creates the Account + a bare Member) -> GetUserByEmail (fetch its
// AccountGuid) -> UpdateMember (fill in the rest of the profile).
export async function adminCreateMember(input: AdminCreateMemberInput): Promise<boolean> {
    const created = await registerAccount(input.insertAccount);
    if (!created) {
        return false;
    }

    const member = await fetchMemberByEmail(input.insertAccount.email);
    if (!member) {
        console.error("Account was created but the new member record could not be found");
        return false;
    }

    return updateMember({ ...member, ...input.profile });
}
