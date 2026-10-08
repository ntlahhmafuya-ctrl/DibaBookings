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

export const shortId = (value) =>
    value ? `#${value.slice(0, 8).toUpperCase()}` : "-";

export const errorText = (error, fallback) => error.response?.data?.message || error.response?.data || fallback;
