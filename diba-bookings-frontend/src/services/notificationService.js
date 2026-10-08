import api from "./api";

export const getNotifications = () => api.get("/Notifications");

export const markNotificationRead = (notificationId) =>
    api.put(`/Notifications/${notificationId}/read`);
