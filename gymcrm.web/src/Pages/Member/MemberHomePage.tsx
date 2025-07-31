import { useEffect, useState } from "react";
import { fetchUserInfoByGuid } from "../../utils/MembershipApi";
import Layout from "../../Layout";
import {MemberInfoDashboard} from "../../components/MemberInfoDashboard";

export default function MemberHomePage() {
    // Define a state to store the user data
    const [userData, setUserData] = useState<any>(null);
    const [loading, setLoading] = useState(true); // State for loading state
    const [error, setError] = useState<string | null>(null); // Error handling state
    
    FetchUserInfoByGuid();

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
        <MemberInfoDashboard userData={userData}></MemberInfoDashboard>
    );
    function FetchUserInfoByGuid() {
        // Use useEffect to fetch the data when the component mounts
        useEffect(() => {
            // Only fetch if userData is null or empty
            if (!userData) {
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
            } else {
                setLoading(false); // If userData is already set, stop loading
            }
        }); // Dependency array includes userData, so it runs again if userData changes
    }
}

