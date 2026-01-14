import { useEffect, useState } from "react";
import { fetchUserInfoByGuid } from "../../utils/MembershipApi";
import Layout from "../../Layout";
import {MemberInfoDashboard} from "../../components/MemberInfoDashboard";
import {MemberData} from "../../models/Member";

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
            <Layout>
                <div>Loading...</div>
            </Layout>); // Display loading state until data is fetched
    }

    if (error) {
        return (
            <Layout>
                <div>{error}</div>
            </Layout>); // Display error message if there's an issue
    }

    if (!userData) {
        return (
            <Layout>
                <div>No user data found.</div>
            </Layout>); // Fallback for no data
    }

    return (
        <Layout showLogout={true}>
            <MemberInfoDashboard userData={userData} />
        </Layout>
    );
}