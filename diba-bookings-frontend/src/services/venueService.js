import api from "./api";

export const getVenues = () => api.get("/Venues");

export const getVenueFeatures = (venueId) =>
    api.get(`/Venues/${venueId}/features`);
