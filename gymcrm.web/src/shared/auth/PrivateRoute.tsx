import { Navigate } from "react-router-dom";
import {JSX} from "react";
import {useAuth} from "./AuthContext";

const PrivateRoute = ({ children }: { children: JSX.Element }) =>{
    const { isAuthenticated, hasAdminAccount } = useAuth();

    // Show loading state while checking
    if (isAuthenticated === null || hasAdminAccount === null) {
        return <div>Loading...</div>;
    }

    // Setup takes priority even over an already-authenticated session - someone could have
    // self-registered as a Member before any admin ever completed setup.
    if (!hasAdminAccount) {
        return <Navigate to="/setup" replace />;
    }

    return isAuthenticated ? children : <Navigate to="/login" replace />;
};

export default PrivateRoute;