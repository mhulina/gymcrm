import { Navigate } from "react-router-dom";
import {JSX} from "react";
import {useAuth} from "./AuthContext";

const PublicRoute = ({ children }: { children: JSX.Element }) =>{
    const { isAuthenticated, hasAdminAccount } = useAuth();

    if (isAuthenticated === null || hasAdminAccount === null) {
        return (
            <div style={{
                display: "flex",
                justifyContent: "center",
                alignItems: "center",
                height: "100vh"
            }}>
                Loading...
            </div>
        );
    }

    // Setup must be unavoidable until an admin exists - even login/register bounce to it.
    if (!hasAdminAccount) {
        return <Navigate to="/setup" replace />;
    }

    return !isAuthenticated ? children : <Navigate to="/member/home" replace />;
};

export default PublicRoute;