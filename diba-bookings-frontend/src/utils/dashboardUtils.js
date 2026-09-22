const dateText = (value) =>
    value
        ? new Date(value).toLocaleDateString([], {
              day: "2-digit",
              month: "short",
              year: "numeric",
          })
        : "-";

const timeText = (value) =>
    value
        ? new Date(value).toLocaleTimeString([], {
              hour: "2-digit",
              minute: "2-digit",
          })
        : "-";

export { dateText, timeText };

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

export const formatDate = (value) =>
    value
        ? new Date(value).toLocaleString([], {
              day: "2-digit",
              month: "short",
              hour: "2-digit",
              minute: "2-digit",
          })
        : "-";

export const shortId = (value) =>
    value ? `#${value.slice(0, 8).toUpperCase()}` : "-";

export const clearSession = () => {
    ["token", "userId", "firstName", "lastName", "email", "role"].forEach(
        (key) => localStorage.removeItem(key)
    );

    window.location.href = "/login";
};

const statusName = (status = "Pending") => String(status || "Pending").trim();
const statusClass = (status = "Pending") => statusName(status).toLowerCase().replace(/\s/g, "-");
const getDate = (item) => item.startDateTime || item.bookingDate || item.date;
const bookingStart = (booking, events) => booking.startDateTime || events.find((event) => event.eventId === booking.eventId)?.startDateTime;
const isFutureBooking = (booking, events) => {
    const start = new Date(bookingStart(booking, events));
    return !Number.isNaN(start.getTime()) && start >= new Date();
};
const dateText = (value) => value ? new Date(value).toLocaleDateString([], { day: "2-digit", month: "short", year: "numeric" }) : "-";
const timeText = (value) => value ? new Date(value).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "-";
const localInput = (value) => value ? new Date(value).toISOString().slice(0, 16) : "";
const errorText = (error, fallback) => error.response?.data?.message || error.response?.data || fallback;
const overlaps = (booking, candidate, excludeId) => booking.bookingId !== excludeId && ["Pending", "Approved"].includes(booking.statusName) && booking.venueId === candidate.venueId && new Date(booking.startDateTime) < new Date(candidate.endDateTime) && new Date(booking.endDateTime) > new Date(candidate.startDateTime);
