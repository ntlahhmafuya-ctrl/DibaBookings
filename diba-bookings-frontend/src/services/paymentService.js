import api from "./api";

export const createPaymentCheckout = (bookingId) =>
    api.post("/Payments", { bookingId });

// Uses the shared API client so the user's JWT is included automatically.
export const getPayment = (paymentId) =>
    api.get(`/Payments/${paymentId}`);
