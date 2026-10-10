import api from "./api";

// Venue directory and details.
export const getVenues = () => api.get("/Venues");
export const getVenue = (venueId) => api.get(`/Venues/${venueId}`);
export const createVenue = (venue) => api.post("/Venues", venue);
export const updateVenue = (venueId, venue) =>
    api.put(`/Venues/${venueId}`, venue);

// Facilities are stored as venue features in the API.
export const getVenueFeatures = (venueId) =>
    api.get(`/Venues/${venueId}/features`);
export const createVenueFeature = (venueId, feature) =>
    api.post(`/Venues/${venueId}/features`, feature);
export const updateVenueFeature = (venueId, featureId, feature) =>
    api.put(`/Venues/${venueId}/features/${featureId}`, feature);
export const deleteVenueFeature = (venueId, featureId) =>
    api.delete(`/Venues/${venueId}/features/${featureId}`);
