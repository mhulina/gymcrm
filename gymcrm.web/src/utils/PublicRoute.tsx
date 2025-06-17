import { Navigate } from "react-router-dom";
import {JSX} from "react";

const PublicRoute = ({ children }: { children: JSX.Element }) =>{
    const token = localStorage.getItem("token");
    const isLoggedIn = !!token;

    return !isLoggedIn ? children : <Navigate to="/member/home" replace />;
};

export default PublicRoute;