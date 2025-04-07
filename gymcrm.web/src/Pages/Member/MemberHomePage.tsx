import { useEffect, useState } from "react";
import { fetchUserInfoByGuid } from "../../utils/api";
import Layout from "../../Layout";

export default function MemberHomePage() {
    // Define a state to store the user data
    const [userData, setUserData] = useState<any>(null);
    const [loading, setLoading] = useState(true); // State for loading state
    const [error, setError] = useState<string | null>(null); // Error handling state

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
    }, [userData]); // Dependency array includes userData, so it runs again if userData changes

    if (loading) {
        return <div>Loading...</div>; // Display loading state until data is fetched
    }

    if (error) {
        return <div>{error}</div>; // Display error message if there's an issue
    }

    if (!userData) {
        return <div>No user data found.</div>; // Fallback for no data
    }

    return (
        <Layout>
            <div className="MemberHomePage">
                <label className="username">Username</label>
                <br/>
                <label className="username-email">{userData.email}</label>
            </div>
        </Layout>
    );
}
