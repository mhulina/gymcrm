import {createContext, ReactNode, useContext, useEffect, useState} from "react";

interface AuthContextType {
    isAuthenticated: boolean | null;
    setIsAuthenticated: (value: boolean) => void;
    checkAuth: () => Promise<void>;
    logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const AUTH_STATE_KEY = 'gym_crm_auth_state';
const AUTH_CHECK_KEY = 'gym_crm_auth_checked';

export const AuthProvider = ({ children }: { children: ReactNode }) => {
    const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(() => {
        const cached = sessionStorage.getItem(AUTH_STATE_KEY);
        return cached ? JSON.parse(cached) : null;
    });
    const updateAuthState = (value: boolean) => {
        setIsAuthenticated(value);
        sessionStorage.setItem(AUTH_STATE_KEY, JSON.stringify(value));
    };
    
    const checkAuth = async () => {
        try {
            const response = await fetch(
                `${process.env.REACT_APP_ACCOUNTS_ENDPOINT}CheckAuth`,
                {
                    method: "GET",
                    credentials: "include",
                }
            );
            updateAuthState(response.ok);
        } catch (error) {
            console.error(`Auth check failed`, error);
            updateAuthState(false);
        }
    };
    
    const logout = () => {
        updateAuthState(false);
        sessionStorage.removeItem(AUTH_CHECK_KEY);
    };
    
    useEffect(() => {
        const hasChecked = sessionStorage.getItem(AUTH_CHECK_KEY);
        
        if (!hasChecked) {
            checkAuth();
            sessionStorage.setItem(AUTH_CHECK_KEY, 'true');
        }
        // Intentionally run once on mount only; checkAuth is stable in practice here.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    
    return (
        <AuthContext.Provider value={{ isAuthenticated, setIsAuthenticated: updateAuthState, checkAuth, logout }}>
            {children}
        </AuthContext.Provider>
    );
};

export const useAuth = () => {
    const context = useContext(AuthContext);
    if (!context) {
        throw new Error("useAuth must be used within AuthProvider");
    }
    return context;
};