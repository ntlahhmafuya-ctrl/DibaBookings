import api from "./api";

export const getEvents = () => api.get("/Events");

export const createEvent = (event) =>
    api.post("/Events", event);

export const updateEvent = (eventId, event) =>
    api.put(`/Events/${eventId}`, event);

export const createEventWithBooking = (event) =>
    api.post("/Events/with-booking", event);
