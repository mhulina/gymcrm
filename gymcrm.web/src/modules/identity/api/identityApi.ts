import {axios} from "../../../shared/api/httpClient";
import {MemberData} from "../types/member";
import {GymSubscriptionType} from "../types/gymSubscriptionType";
import {AccountType} from "../types/accountType";

export async function fetchUserInfoByGuid(): Promise<MemberData | null> {
    try {
        const response = await axios.get<MemberData>(`GetMe`)
        return response.data;
    } catch (error) {
        console.error("Error fetching user info: ", error);
        return null;
    }
}

export async function handleLogin(
    email: string,
    password: string,
    navigate: (path: string, options?: { replace?: boolean }) => void
) : Promise<boolean> {
    const loginData = JSON.stringify({username: email.trim(), password: password});

    try {
        const response = await fetch(
            process.env.REACT_APP_ACCOUNTS_ENDPOINT + "Login",{
                headers: {"Content-Type": "application/json"},
                method: "POST",
                credentials: "include",
                body: loginData
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

export async function handleMemberRegistration(
    email: string,
    password: string,
    navigate: (path: string, options?: { replace?: boolean }) => void
) : Promise<boolean> {
    const registrationData = JSON.stringify({
        email: email.trim(),
        password: password,
        accountType: AccountType.Admin,
        gymSubscriptionType: GymSubscriptionType.Monthly,
        gender: 0
    });

    try {
        const res = await fetch(
            process.env.REACT_APP_ACCOUNTS_ENDPOINT+"Register",{
                headers: {"Content-Type": "application/json"},
                method: "POST",
                credentials: "include",
                body: registrationData
            });

        if (res.ok){
            const success = await handleLogin(email, password, navigate);
            return success;
        }
        else{
            const error = await res.text();
            console.error("Registration failed:", error);
            return false;
        }
    }
    catch(error) {
        console.error("Error fetching user info:", error);
        return false;
    }
}
