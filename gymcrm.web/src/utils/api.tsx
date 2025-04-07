// utils/api.ts
import {jwtDecode, JwtPayload} from "jwt-decode";

interface Member{
    username: string,
    email: string,
    guid: string,
}

export async function fetchUserInfoByGuid(): Promise<Member | null> {
    const token = localStorage.getItem("token") ?? "";

    if (!token) {
        console.error("Token not found in localStorage.");
        return Promise.resolve(null);
    }

    try {
        const decoded: JwtPayload = jwtDecode(token);
        const guid = decoded?.sub;

        if (!guid) {
            console.error("Token is invalid or missing subject (sub).");
            return Promise.resolve(null);
        }

        return fetch(
            `${process.env.REACT_APP_MEMBERS_ENDPOINT}GetUserByGuid/${guid}`,
            {
                method: "GET",
                mode: "cors",
                credentials: "include",
                headers: {
                    Authorization: `Bearer ${token}`,
                },
            }
        )
        .then((response) => {
            if (!response.ok) {
                throw new Error("Failed to fetch user data");
            }
            return response.json()
        })
        .then((data: Member) => {
            return data;
        })
        .catch((error) => {
            console.error("Error fetching user info:", error);
            return null; // Return null if there was an error
        });
    } catch (error) {
        console.error("Error fetching user info:", error);
        return Promise.resolve(null);
    }
}
