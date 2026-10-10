import api from "./api";

export const createPaymentCheckout = (bookingId) =>
    api.post("/Payments", { bookingId });

// Staff and administrators use these endpoints to review payments and safely retry confirmed failures.
export const getPayments = () => api.get("/Payments");

export const retryFailedRefund = (paymentId) =>
    api.post(`/Payments/${paymentId}/refund/retry`);
