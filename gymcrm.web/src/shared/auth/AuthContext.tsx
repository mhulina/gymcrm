import {createContext, ReactNode, useContext, useEffect, useState} from "react";

interface AuthContextType {
    isAuthenticated: boolean | null;
    setIsAuthenticated: (value: boolean) => void;
    checkAuth: () => Promise<void>;
    logout: () => void;
    hasAdminAccount: boolean | null;
    refreshHasAdminAccount: () => Promise<void>;
    mustChangePassword: boolean;
    setMustChangePassword: (value: boolean) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const AUTH_STATE_KEY = 'gym_crm_auth_state';
const AUTH_CHECK_KEY = 'gym_crm_auth_checked';
const MUST_CHANGE_PASSWORD_KEY = 'gym_crm_must_change_password';

export const AuthProvider = ({ children }: { children: ReactNode }) => {
    const [isAuthenticated, setIsAuthenticated] = useState<boolean | null>(() => {
        const cached = sessionStorage.getItem(AUTH_STATE_KEY);
        return cached ? JSON.parse(cached) : null;
    });
    const updateAuthState = (value: boolean) => {
        setIsAuthenticated(value);
        sessionStorage.setItem(AUTH_STATE_KEY, JSON.stringify(value));
    };

    // Cached in sessionStorage the same way isAuthenticated is (unlike hasAdminAccount below) -
    // without this, an F5 on /change-password before actually changing it would reset this to
    // false on remount and PrivateRoute would wave the user straight into /member/home.
    const [mustChangePassword, setMustChangePasswordState] = useState<boolean>(() => {
        const cached = sessionStorage.getItem(MUST_CHANGE_PASSWORD_KEY);
        return cached ? JSON.parse(cached) : false;
    });
    const updateMustChangePassword = (value: boolean) => {
        setMustChangePasswordState(value);
        sessionStorage.setItem(MUST_CHANGE_PASSWORD_KEY, JSON.stringify(value));
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
            if (response.ok) {
                const data = await response.json();
                updateMustChangePassword(Boolean(data?.mustChangePassword));
            }
        } catch (error) {
            console.error(`Auth check failed`, error);
            updateAuthState(false);
        }
    };

    const logout = () => {
        updateAuthState(false);
        updateMustChangePassword(false);
        sessionStorage.removeItem(AUTH_CHECK_KEY);
    };

    // Deliberately NOT cached in sessionStorage like isAuthenticated - a second tab/window
    // could complete the first-run admin setup independently, so this must always reflect
    // current DB truth rather than a stale per-tab snapshot. Runs on every mount.
    const [hasAdminAccount, setHasAdminAccount] = useState<boolean | null>(null);

    const refreshHasAdminAccount = async () => {
        try {
            const response = await fetch(
                `${process.env.REACT_APP_ACCOUNTS_ENDPOINT}HasAdminAccount`,
                { method: "GET", credentials: "include" }
            );
            if (response.ok) {
                setHasAdminAccount(await response.json());
            } else {
                // Fail closed: if the check itself fails, don't accidentally expose the
                // setup screen - assume an admin exists rather than risk a bypass.
                setHasAdminAccount(true);
            }
        } catch (error) {
            console.error("Admin account check failed", error);
            setHasAdminAccount(true);
        }
    };

    useEffect(() => {
        const hasChecked = sessionStorage.getItem(AUTH_CHECK_KEY);

        if (!hasChecked) {
            checkAuth();
            sessionStorage.setItem(AUTH_CHECK_KEY, 'true');
        }
        refreshHasAdminAccount();
        // Intentionally run once on mount only; checkAuth is stable in practice here.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return (
        <AuthContext.Provider value={{
            isAuthenticated, setIsAuthenticated: updateAuthState, checkAuth, logout,
            hasAdminAccount, refreshHasAdminAccount,
            mustChangePassword, setMustChangePassword: updateMustChangePassword,
        }}>
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