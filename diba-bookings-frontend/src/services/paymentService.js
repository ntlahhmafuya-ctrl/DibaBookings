import api from "./api";

export const createPaymentCheckout = (bookingId) =>
    api.post("/Payments", { bookingId });
