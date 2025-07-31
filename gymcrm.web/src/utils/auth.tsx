import {jwtDecode} from "jwt-decode";

export function isTokenValid (token: string | null): boolean {
    if (!token || token.trim() === "") {
        return false;
    }
    
    try {
        const decoded = jwtDecode(token);
        
        if (!decoded.exp) {
            return false;
        }
        
        const now = Date.now() / 1000; // Current time in seconds
        return decoded.exp > now;
    }
    catch (error) {
        console.error("Invalid token format: ", error);
        return false;
    }
}

export function clearToken(): void {
    localStorage.removeItem("token");
}