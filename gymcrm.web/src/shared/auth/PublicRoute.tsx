import { Navigate } from "react-router-dom";
import {JSX} from "react";
import {useAuth} from "./AuthContext";

const PublicRoute = ({ children }: { children: JSX.Element }) =>{
    const { isAuthenticated } = useAuth();

    if (isAuthenticated === null) {
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

    return !isAuthenticated ? children : <Navigate to="/member/home" replace />;
};

export default PublicRoute;