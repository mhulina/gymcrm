import { Navigate } from "react-router-dom";
import { useAuth } from "../shared/auth/AuthContext";

export default function RootRedirect() {
    const { isAuthenticated } = useAuth();

    if (isAuthenticated === null) {
        return (
            <div className="flex min-h-screen items-center justify-center bg-slate-50 dark:bg-slate-950">
                <p className="text-sm text-slate-500 dark:text-slate-400">Loading...</p>
            </div>
        );
    }

    return <Navigate to={isAuthenticated ? "/member/home" : "/login"} replace />;
}
