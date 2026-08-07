import Axios from "axios";

const API_BASE_URL = process.env.REACT_APP_MEMBERS_ENDPOINT;

export const axios = Axios.create({
    baseURL: API_BASE_URL,
    headers: {
        "Content-Type": "application/json",
    },
    withCredentials: true
});

axios.interceptors.response.use(
    (response) => response, // Pass through successful responses
    async (error) => {
        const originalRequest = error.config;

        // If we get 401 and haven't retried yet
        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true;

            try {
                // Try to refresh the token (via the raw axios lib, not our instance,
                // so this call bypasses baseURL/interceptors and can't recurse)
                await Axios.post(
                    `${process.env.REACT_APP_ACCOUNTS_ENDPOINT}RefreshToken`,
                    {},
                    { withCredentials: true }
                );

                // Retry the original request with new token (in cookie)
                return axios(originalRequest);
            } catch (refreshError) {
                // Refresh failed - redirect to login
                console.error("Token refresh failed:", refreshError);
                window.location.href = "/login";
                return Promise.reject(refreshError);
            }
        }

        return Promise.reject(error);
    }
);

const sendRequest = async (method: string, url: string, data = null, params = {}) => {
    try{
        const response = await axios({ method, url, data, params });
        return response.data;
    }
    catch(err: any){
        console.error("API error: ", err.response?.data || err.message);
        throw err.response?.data || err.message;
    }
};

// Generic REST helpers for module-specific api/*.ts files to build on.
export const apiClient = {
    get: (url: string, params?: {}) => sendRequest("get", url, null, params),
    post: (url: string, data: any) => sendRequest("post", url, data),
    put: (url: string, data: any) => sendRequest("put", url, data),
    delete: (url: string, params?: {})=> sendRequest("delete", url, null, params)
};
