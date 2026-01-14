import { Navigate } from "react-router-dom";
import {JSX, useEffect, useState} from "react";
import {useAuth} from "../contexts/AuthContext";

const PrivateRoute = ({ children }: { children: JSX.Element }) =>{
    const { isAuthenticated } = useAuth();

    // Show loading state while checking
    if (isAuthenticated === null) {
        return <div>Loading...</div>;
    }

    return isAuthenticated ? children : <Navigate to="/login" replace />;
};

export default PrivateRoute;