import axios from "axios";

// API CLIENT: centralises the backend URL and JSON defaults for frontend requests.
const configuredApiBaseUrl = process.env.REACT_APP_API_BASE_URL || "https://localhost:7054/api";

const api = axios.create({
    baseURL: configuredApiBaseUrl,
    headers: {
        "Content-Type": "application/json",
    },
});

// AUTHENTICATION HEADER: attach the saved bearer token to requests when the user is signed in.
api.interceptors.request.use((config) => {
    const token = localStorage.getItem("token");
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

export default api;