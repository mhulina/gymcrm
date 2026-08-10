import { Navigate, useLocation } from "react-router-dom";
import {JSX} from "react";
import {useAuth} from "./AuthContext";

const CHANGE_PASSWORD_PATH = "/change-password";

const PrivateRoute = ({ children }: { children: JSX.Element }) =>{
    const { isAuthenticated, hasAdminAccount, mustChangePassword } = useAuth();
    const location = useLocation();

    // Show loading state while checking
    if (isAuthenticated === null || hasAdminAccount === null) {
        return <div>Loading...</div>;
    }

    // Setup takes priority even over an already-authenticated session - someone could have
    // self-registered as a Member before any admin ever completed setup.
    if (!hasAdminAccount) {
        return <Navigate to="/setup" replace />;
    }

    if (!isAuthenticated) {
        return <Navigate to="/login" replace />;
    }

    // A temporary (admin-assigned) password must be changed before anything else is usable -
    // except the change-password page itself, or this would redirect-loop forever.
    if (mustChangePassword && location.pathname !== CHANGE_PASSWORD_PATH) {
        return <Navigate to={CHANGE_PASSWORD_PATH} replace />;
    }

    return children;
};

export default PrivateRoute;