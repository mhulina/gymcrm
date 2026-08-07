import { Navigate } from "react-router-dom";
import { JSX } from "react";
import { useAuth } from "../../../shared/auth/AuthContext";

// Mirrors AdminRoute's shape but inverted: only reachable while no admin account exists
// yet. Once setup is complete, this bounces to /login so a stale bookmark can't be reused.
const SetupRoute = ({ children }: { children: JSX.Element }) => {
    const { hasAdminAccount } = useAuth();

    if (hasAdminAccount === null) {
        return <div>Loading...</div>;
    }

    return hasAdminAccount === false ? children : <Navigate to="/login" replace />;
};

export default SetupRoute;
