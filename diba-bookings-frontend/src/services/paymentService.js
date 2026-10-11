import api from "./api";

// Event organisers can start checkout for their own approved bookings.
export const createPaymentCheckout = (bookingId) =>
    api.post("/Payments", { bookingId });

// Staff and Administrators can review payment/refund outcomes.
export const getPayments = () => api.get("/Payments");

export const getPayment = (paymentId) =>
    api.get(`/Payments/${paymentId}`);

// The API only permits retrying a refund that Yoco explicitly confirmed failed.
export const retryFailedRefund = (paymentId) =>
    api.post(`/Payments/${paymentId}/refund/retry`);

// Read the latest payment attempt for one of the signed-in organiser's bookings.
export const getBookingPayment = (bookingId) =>
    api.get(`/Payments/booking/${bookingId}`);
