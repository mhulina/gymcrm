import { useEffect, useState } from "react";
import { fetchUserInfoByGuid } from "../api/identityApi";
import AppLayout from "../../../app/AppLayout";
import {MemberInfoDashboard} from "../components/MemberInfoDashboard";
import {MemberData} from "../types/member";

export default function MemberHomePage() {
    // Define a state to store the user data
    const [userData, setUserData] = useState<MemberData | null>(null);
    const [loading, setLoading] = useState(true); // State for loading state
    const [error, setError] = useState<string | null>(null); // Error handling state

    useEffect(() => {
        fetchUserInfoByGuid()
            .then((data) => {
                if (data) {
                    setUserData(data); // Set the user data in the state
                } else {
                    setError("No user data");
                }
            })
            .catch((err) => {
                setError("Error fetching user data");
                console.error("Error:", err);
            })
            .finally(() => {
                setLoading(false); // Set loading to false once fetching is done
            });
    }, []);

    if (loading) {
        return (
            <AppLayout>
                <div>Loading...</div>
            </AppLayout>); // Display loading state until data is fetched
    }

    if (error) {
        return (
            <AppLayout>
                <div>{error}</div>
            </AppLayout>); // Display error message if there's an issue
    }

    if (!userData) {
        return (
            <AppLayout>
                <div>No user data found.</div>
            </AppLayout>); // Fallback for no data
    }

    return (
        <AppLayout showLogout={true}>
            <MemberInfoDashboard userData={userData} />
        </AppLayout>
    );
}