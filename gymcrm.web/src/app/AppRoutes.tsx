import { Routes, Route } from "react-router-dom";
import RootRedirect from "./RootRedirect";
import LoginPage from "../modules/identity/pages/LoginPage";
import RegisterMemberPage from "../modules/identity/pages/RegisterMemberPage";
import MemberHomePage from "../modules/identity/pages/MemberHomePage";
import AdminAddMemberPage from "../modules/identity/pages/AdminAddMemberPage";
import AdminMembersListPage from "../modules/identity/pages/AdminMembersListPage";
import EditMemberProfilePage from "../modules/identity/pages/EditMemberProfilePage";
import AdminRoute from "../modules/identity/components/AdminRoute";
import SetupRoute from "../modules/identity/components/SetupRoute";
import AdminSetupPage from "../modules/identity/pages/AdminSetupPage";
import PublicRoute from "../shared/auth/PublicRoute";
import PrivateRoute from "../shared/auth/PrivateRoute";

export default function AppRoutes() {
    return (
        <Routes>
            <Route path="/" element={<RootRedirect />} />
            <Route path="/setup" element={
                <SetupRoute>
                    <AdminSetupPage />
                </SetupRoute>
            } />
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
            <Route path="/member/edit" element={
                <PrivateRoute>
                    <EditMemberProfilePage />
                </PrivateRoute>
            } />
            <Route path="/admin/members/new" element={
                <PrivateRoute>
                    <AdminRoute>
                        <AdminAddMemberPage />
                    </AdminRoute>
                </PrivateRoute>
            } />
            <Route path="/admin/members" element={
                <PrivateRoute>
                    <AdminRoute>
                        <AdminMembersListPage />
                    </AdminRoute>
                </PrivateRoute>
            } />
            <Route path="/admin/members/:guid/edit" element={
                <PrivateRoute>
                    <AdminRoute>
                        <EditMemberProfilePage />
                    </AdminRoute>
                </PrivateRoute>
            } />
        </Routes>
    );
}
