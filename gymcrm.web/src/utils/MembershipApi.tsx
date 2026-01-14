// utils/api.ts
import axios from "axios";
import {MemberData} from "../models/Member";
import {GymSubscriptionType} from "../Constants/Enums/GymSubscriptionType";
import {error} from "ajv/dist/vocabularies/jtd/properties";
import {AccountType} from "../Constants/Enums/AccountType";

const API_BASE_URL = process.env.REACT_APP_MEMBERS_ENDPOINT;

interface Member{
    username: string,
    email: string,
    guid: string,
}

const httpClient = axios.create({
    baseURL: API_BASE_URL,
    headers: {
        "Content-Type": "application/json",
    },
    withCredentials: true
});

httpClient.interceptors.response.use(
    (response) => response, // Pass through successful responses
    async (error) => {
        const originalRequest = error.config;

        // If we get 401 and haven't retried yet
        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            try {
                // Try to refresh the token
                await axios.post(
                    `${process.env.REACT_APP_ACCOUNTS_ENDPOINT}RefreshToken`,
                    {},
                    { withCredentials: true }
                );

                // Retry the original request with new token (in cookie)
                return httpClient(originalRequest);
            } catch (refreshError) {
                // Refresh failed - redirect to login
                console.error("Token refresh failed:", refreshError);
                window.location.href = "/login";
                return Promise.reject(refreshError);
            }
        }

        return Promise.reject(error);
    }
);

const sendRequest = async (method: string, url: string, data = null, params = {}) => {
    try{
        const response = await httpClient({ method, url, data, params });
        return response.data;
    }
    catch(err: any){
        console.error("API error: ", err.response?.data || err.message);
        throw err.response?.data || err.message;
    }
};

const MembershipApi = {
    get: (url: string, params?: {}) => sendRequest("get", url, null, params),
    post: (url: string, data: any) => sendRequest("post", url, data),
    put: (url: string, data: any) => sendRequest("put", url, data),
    delete: (url: string, params?: {})=> sendRequest("delete", url, null, params)
};

export async function fetchUserInfoByGuid(): Promise<MemberData | null> {
    try {
        const response = await httpClient.get<MemberData>(`GetMe`)
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