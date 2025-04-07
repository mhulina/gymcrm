// utils/api.ts
import {jwtDecode, JwtPayload} from "jwt-decode";
import {AccountType} from "../Pages/Account/RegisterMember";

interface Member{
    username: string,
    email: string,
    guid: string,
}

export async function fetchUserInfoByGuid(): Promise<Member | null> {
    const token = localStorage.getItem("token") ?? "";

    if (!token) {
        console.error("Token not found in localStorage.");
        return Promise.resolve(null);
    }

    try {
        const decoded: JwtPayload = jwtDecode(token);
        const guid = decoded?.sub;

        if (!guid) {
            console.error("Token is invalid or missing subject (sub).");
            return Promise.resolve(null);
        }

        return fetch(
            `${process.env.REACT_APP_MEMBERS_ENDPOINT}GetUserByGuid/${guid}`,
            {
                method: "GET",
                mode: "cors",
                credentials: "include",
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        )
        .then((response) => {
            if (!response.ok) {
                throw new Error("Failed to fetch user data");
            }
            return response.json()
        })
        .then((data: Member) => {
            return data;
        })
        .catch((error) => {
            console.error("Error fetching user info:", error);
            return null; // Return null if there was an error
        });
    } catch (error) {
        console.error("Error fetching user info:", error);
        return Promise.resolve(null);
    }
}

export async function handleLogin(
    email: string,
    password: string,
    navigate: (path: string, options?: { replace?: boolean }) => void
) {
    let jsonLogin = JSON.stringify({username: email.trim(), password: password});
    console.log(jsonLogin);
    try {
        await fetch(
            process.env.REACT_APP_ACCOUNTS_ENDPOINT+"Login",{
                headers: {"Content-Type": "application/json"},
                method: "POST",
                body: jsonLogin
            })
            .then(async res => {
                if (res.ok) {
                    return res.json()
                } else {
                    const error = await res.text();
                    console.error("Login failed:", error);
                }
            })
            .then(json => {
                console.log(json);
    
                if (json) {
                    localStorage.setItem("token", json);
                    navigate("/member/home", { replace: true});
                }
                else{
                    localStorage.setItem("token", "");
                }
            });
    }
    catch (error) {
        console.error("Error logging member in:", error);
    }
}

export async function handleMemberRegistration(
    email: string,
    password: string,
    navigate: (path: string, options?: { replace?: boolean }) => void
) {
    let jsonMemberRegister = JSON.stringify({
        email: email.trim(), 
        password: password, 
        accountType: AccountType.Member.toString(), 
        gymSubscriptionType: "1"});
    console.log(jsonMemberRegister);

    try {
        const res = await fetch(
            process.env.REACT_APP_ACCOUNTS_ENDPOINT+"Register",{
                headers: {"Content-Type": "application/json"},
                method: "POST",
                body: jsonMemberRegister
            });
        
        if (res.ok){
            await handleLogin(email, password, navigate);
        }
        else{
            const error = await res.text();
            console.error("Registration failed:", error);
        }
    }
    catch(error) {
        console.error("Error fetching user info:", error);
    }
}
