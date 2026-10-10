import api from "./api";

export const getBookings = () => api.get("/Bookings");

export const getBookingAvailability = (params) =>
    api.get("/Bookings/availability", { params });

export const getVenueAvailability = (venueId, date) =>
    api.get(`/Bookings/venue/${venueId}/availability`, {
        params: { date }
    });

export const createBooking = (booking) =>
    api.post("/Bookings", booking);

export const updateBooking = (bookingId, booking) =>
    api.put(`/Bookings/${bookingId}`, booking);

export const cancelBooking = (bookingId) =>
    api.put(`/Bookings/${bookingId}/cancel`);

export const approveBooking = (bookingId) =>
    api.put(`/Bookings/${bookingId}/approve`);

export const rejectBooking = (bookingId, reason) =>
    api.put(`/Bookings/${bookingId}/reject`, { reason });

// Staff/admin close-out for an approved booking after its scheduled end time.
export const completeBooking = (bookingId) =>
    api.put(`/Bookings/${bookingId}/complete`);
