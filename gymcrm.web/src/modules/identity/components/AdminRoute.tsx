import { Navigate } from "react-router-dom";
import { JSX, useEffect, useState } from "react";
import { fetchUserInfoByGuid } from "../api/identityApi";
import { AccountType } from "../types/accountType";

// Role gate layered on top of PrivateRoute: PrivateRoute confirms the visitor
// is logged in, this confirms they're an Admin. Client-side only - the
// backend doesn't enforce role-based authorization on these endpoints yet,
// so this hides the page rather than truly securing the underlying API calls.
const AdminRoute = ({ children }: { children: JSX.Element }) => {
    const [isAdmin, setIsAdmin] = useState<boolean | null>(null);

    useEffect(() => {
        fetchUserInfoByGuid().then((member) => {
            setIsAdmin(member?.accountType === AccountType.Admin);
        });
    }, []);

    if (isAdmin === null) {
        return <div>Loading...</div>;
    }

    return isAdmin ? children : <Navigate to="/member/home" replace />;
};

export default AdminRoute;
