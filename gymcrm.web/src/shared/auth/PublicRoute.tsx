import { Navigate } from "react-router-dom";
import {JSX} from "react";
import {useAuth} from "./AuthContext";

const PublicRoute = ({ children }: { children: JSX.Element }) =>{
    const { isAuthenticated, hasAdminAccount, mustChangePassword } = useAuth();

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

    if (isAuthenticated) {
        // Skip the extra bounce through /member/home for an already-signed-in user who's
        // still on a temporary password - PrivateRoute would just redirect them again.
        return <Navigate to={mustChangePassword ? "/change-password" : "/member/home"} replace />;
    }

    return children;
};

export default PublicRoute;