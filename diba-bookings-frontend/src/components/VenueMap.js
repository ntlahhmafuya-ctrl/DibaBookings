import { MapContainer, TileLayer, Marker, Popup, useMap } from "react-leaflet";
import L from "leaflet";
import "leaflet-routing-machine";
import "leaflet-routing-machine/dist/leaflet-routing-machine.css";
import { useEffect, useState } from "react";

import markerIcon2x from "leaflet/dist/images/marker-icon-2x.png";
import markerIcon from "leaflet/dist/images/marker-icon.png";
import markerShadow from "leaflet/dist/images/marker-shadow.png";

const FALLBACK_CENTER = { lat: -26.2041, lng: 28.0473 };

function toNumber(value, fallback) {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : fallback;
}

function getVenuePosition(venue) {
    if (!venue) return FALLBACK_CENTER;

    return {
        lat: toNumber(venue.latitude, FALLBACK_CENTER.lat),
        lng: toNumber(venue.longitude, FALLBACK_CENTER.lng)
    };
}

// Fix Leaflet marker icons in React
delete L.Icon.Default.prototype._getIconUrl;

L.Icon.Default.mergeOptions({
    iconRetinaUrl: markerIcon2x,
    iconUrl: markerIcon,
    shadowUrl: markerShadow
});

function Routing({ userLocation, venueLocation }) {
    const map = useMap();

    useEffect(() => {
        if (!userLocation || !venueLocation || !L.Routing?.control) return;

        const routing = L.Routing.control({
            waypoints: [
                L.latLng(userLocation.lat, userLocation.lng),
                L.latLng(venueLocation.lat, venueLocation.lng)
            ],
            routeWhileDragging: false,
            show: false,
            addWaypoints: false,
            createMarker: () => null
        }).addTo(map);

        return () => {
            if (map && routing) map.removeControl(routing);
        };
    }, [map, userLocation, venueLocation]);

    return null;
}

export default function VenueMap({ venue }) {
    const [userLocation, setUserLocation] = useState(null);
    const [locationError, setLocationError] = useState(false);
    const venuePosition = getVenuePosition(venue);

    useEffect(() => {
        if (!navigator.geolocation) {
            setLocationError(true);
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {
                setUserLocation({
                    lat: position.coords.latitude,
                    lng: position.coords.longitude
                });
            },
            (error) => {
                console.warn("Unable to get user location:", error);
                setLocationError(true);
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 60000
            }
        );
    }, []);

    const hasVenueCoordinates = Number.isFinite(venuePosition.lat) && Number.isFinite(venuePosition.lng);

    if (!hasVenueCoordinates) {
        return (
            <div className="venue-map-container">
                <div style={{ padding: "18px", color: "#6d7d88" }}>
                    Map location is not available for this venue yet.
                </div>
            </div>
        );
    }

    return (
        <div className="venue-map-container">
            <MapContainer
                key={`${venuePosition.lat}-${venuePosition.lng}`}
                center={[venuePosition.lat, venuePosition.lng]}
                zoom={15}
                scrollWheelZoom={false}
                style={{
                    height: "450px",
                    width: "100%"
                }}
            >
                <TileLayer
                    url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
                    attribution="&copy; OpenStreetMap contributors"
                />

                <Marker position={[venuePosition.lat, venuePosition.lng]}>
                    <Popup>
                        <strong>{venue?.venueName || "Venue"}</strong>
                        <br />
                        {venue?.location || "Location details unavailable"}
                    </Popup>
                </Marker>

                {userLocation && (
                    <Marker position={[userLocation.lat, userLocation.lng]}>
                        <Popup>Your Location</Popup>
                    </Marker>
                )}

                {userLocation && !locationError && (
                    <Routing
                        userLocation={userLocation}
                        venueLocation={venuePosition}
                    />
                )}
            </MapContainer>
        </div>
    );
}