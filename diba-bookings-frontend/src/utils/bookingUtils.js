export const statusName = (status = "Pending") =>
    String(status || "Pending").trim();

export const statusClass = (status = "Pending") =>
    statusName(status).toLowerCase().replace(/\s/g, "-");

export const bookingStart = (booking, events) =>
    booking.startDateTime ||
    events.find((event) => event.eventId === booking.eventId)
        ?.startDateTime;

export const isFutureBooking = (booking, events) => {
    const start = new Date(bookingStart(booking, events));
    return !Number.isNaN(start.getTime()) && start >= new Date();
};

export const overlaps = (booking, candidate, excludeId) =>
    booking.bookingId !== excludeId &&
    ["Pending", "Approved"].includes(booking.statusName) &&
    booking.venueId === candidate.venueId &&
    new Date(booking.startDateTime) < new Date(candidate.endDateTime) &&
    new Date(booking.endDateTime) > new Date(candidate.startDateTime);
