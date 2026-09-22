function VenueDetails({ venue, image, onBack, onBook }) {
    const available = venue.venueStatus?.toLowerCase() === "available";
    const lat = Number(venue?.latitude ?? venue?.Latitude ?? 0);
    const lng = Number(venue?.longitude ?? venue?.Longitude ?? 0);
    const hasCoordinates = Number.isFinite(lat) && Number.isFinite(lng) && lat !== 0 && lng !== 0;

    return (
        <section className="detail-view">
            <button className="back-link" onClick={onBack}>
                ← Back to venues
            </button>

            <div className="venue-detail-layout">
                <img src={image} alt={venue.venueName} />

                <div className="detail-copy">
                    <Status status={venue.venueStatus} />

                    <h2>{venue.venueName}</h2>

                    <p>
                        {venue.venueDescription ||
                            "A flexible venue for conferences, meetings and campus events."}
                    </p>

                    <dl>
                        <div>
                            <dt>Location</dt>
                            <dd>{venue.location || "Not specified"}</dd>
                        </div>

                        <div>
                            <dt>Capacity</dt>
                            <dd>{venue.capacity} guests</dd>
                        </div>
                    </dl>

                    <h3>Facilities</h3>

                    <div className="feature-chips">
                        {venue.features?.length ? (
                            venue.features.map((feature) => (
                                <span key={feature.venueFeatureId}>
                                    {feature.featureName}
                                </span>
                            ))
                        ) : (
                            <span>Facilities information not listed</span>
                        )}
                    </div>

                    <div className="detail-actions">
                        {available ? (
                            <button
                                className="primary-action"
                                onClick={onBook}
                            >
                                Book This Venue
                            </button>
                        ) : (
                            <button
                                className="disabled-action"
                                disabled
                            >
                                Venue Unavailable
                            </button>
                        )}
                    </div>
                </div>
            </div>

<div className="venue-map-section">
    <div className="section-heading">
        <div>
            <p className="section-kicker">LOCATION</p>
            <h3>Find this venue</h3>
        </div>
    </div>

<div className="map-container">
    {hasCoordinates ? (
        <iframe
            src={`https://www.google.com/maps?q=${lat},${lng}&z=15&output=embed`}
            width="100%"
            height="450"
            style={{ border: 0 }}
            allowFullScreen
            loading="lazy"
            referrerPolicy="strict-origin-when-cross-origin"
            title={venue.venueName || "Venue location"}
        />
    ) : (
        <div style={{ padding: "18px", color: "#6d7d88" }}>Map location is not available for this venue yet.</div>
    )}
</div>

    <div className="google-maps-action">
        <a
            href={hasCoordinates ? `https://www.google.com/maps/dir/?api=1&destination=${lat},${lng}` : "#"}
            target={hasCoordinates ? "_blank" : undefined}
            rel={hasCoordinates ? "noopener noreferrer" : undefined}
            className="google-maps-button"
            aria-disabled={!hasCoordinates}
            onClick={(event) => {
                if (!hasCoordinates) {
                    event.preventDefault();
                }
            }}
        >
            Get Directions in Google Maps
        </a>
    </div>
</div>
        </section>
    );
}