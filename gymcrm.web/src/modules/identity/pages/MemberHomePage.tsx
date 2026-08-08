import { useEffect, useState } from "react";
import { fetchUserInfoByGuid } from "../api/identityApi";
import AppLayout from "../../../app/AppLayout";
import {MemberInfoDashboard} from "../components/MemberInfoDashboard";
import {Member} from "../types/member";

export default function MemberHomePage() {
    const [userData, setUserData] = useState<Member | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    function loadUser() {
        fetchUserInfoByGuid()
            .then((data) => {
                if (data) {
                    setUserData(data);
                } else {
                    setError("No user data");
                }
            })
            .catch((err) => {
                setError("Error fetching user data");
                console.error("Error:", err);
            })
            .finally(() => {
                setLoading(false);
            });
    }

    // eslint-disable-next-line react-hooks/exhaustive-deps
    useEffect(loadUser, []);

    if (loading) {
        return (
            <AppLayout showLogout>
                <p className="text-sm text-slate-500 dark:text-slate-400">Loading your dashboard...</p>
            </AppLayout>
        );
    }

    if (error || !userData) {
        return (
            <AppLayout showLogout>
                <p className="text-sm text-red-600 dark:text-red-400">{error ?? "No user data found."}</p>
            </AppLayout>
        );
    }

    return (
        <AppLayout showLogout>
            <MemberInfoDashboard userData={userData} onUserDataChanged={loadUser} />
        </AppLayout>
    );
}
