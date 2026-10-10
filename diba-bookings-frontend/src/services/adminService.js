import api from "./api";

export const getAdminOverview = () =>
    api.get("/Administration/overview");

export const getUsers = () => api.get("/Users");

export const getAuditLogs = () => api.get("/AuditLogs");

export const getRoles = () => api.get("/Administration/roles");

export const updateUserStatus = (userId, active) =>
    api.put(`/Administration/users/${userId}/status?active=${active}`);

export const updateUserRole = (userId, roleId) =>
    api.put(`/Administration/users/${userId}/role`, { roleId });

export const createUser = (user) =>
    api.post("/Administration/users", user);


// PRIVACY REQUESTS: administrator queue and status updates for user-submitted privacy requests.
export const getPrivacyRequests = () => api.get("/PrivacyRequests");

export const updatePrivacyRequest = (requestId, update) =>
    api.put(`/PrivacyRequests/${requestId}`, update);
