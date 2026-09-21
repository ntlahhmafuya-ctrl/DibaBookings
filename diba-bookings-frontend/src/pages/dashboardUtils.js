export const emptyOverview = {
    totalUsers: 0,
    activeUsers: 0,
    totalBookings: 0,
    pendingBookings: 0,
    approvedBookings: 0,
    rejectedBookings: 0,
    cancelledBookings: 0,
    totalPayments: 0,
    recentActivity: [],
};

export const formatDate = (value) => value ? new Date(value).toLocaleString([], {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
}) : "-";

export const shortId = (value) => value ? `#${value.slice(0, 8).toUpperCase()}` : "-";

export const clearSession = () => {
    ["token", "userId", "firstName", "lastName", "email", "role"].forEach((key) => localStorage.removeItem(key));
    window.location.href = "/login";
};
