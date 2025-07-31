import { Navigate } from "react-router-dom";
import {JSX} from "react";

const PrivateRoute = ({ children }: { children: JSX.Element }) =>{
    const token = localStorage.getItem("token");
    const isLoggedIn = !!token;
    
    return isLoggedIn ? children : <Navigate to="/login" replace />;
};

export default PrivateRoute;