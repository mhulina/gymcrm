import { Routes, Route } from "react-router-dom";
import HomePage from "./pages/HomePage";
import LoginPage from "../modules/identity/pages/LoginPage";
import RegisterMemberPage from "../modules/identity/pages/RegisterMemberPage";
import MemberHomePage from "../modules/identity/pages/MemberHomePage";
import PublicRoute from "../shared/auth/PublicRoute";
import PrivateRoute from "../shared/auth/PrivateRoute";

export default function AppRoutes() {
    return (
        <Routes>
            <Route path="/" element={<HomePage />} />
            <Route path="/login" element={
                <PublicRoute>
                    <LoginPage />
                </PublicRoute>
            } />
            <Route path="/register" element={
                <PublicRoute>
                    <RegisterMemberPage />
                </PublicRoute>
            } />
            <Route path="/member/home" element={
                <PrivateRoute>
                    <MemberHomePage />
                </PrivateRoute>
            } />
        </Routes>
    );
}
