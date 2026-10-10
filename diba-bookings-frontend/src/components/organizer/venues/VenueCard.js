import Status from "../../common/StatusBadge";

function VenueCard({ venue, onOpen }) {
    return (
        <article className="browse-venue-card">

            <div className="venue-card-image">
                {venue.venueImageData && (
                    <img
                        src={venue.venueImageData}
                        alt={`Photo of ${venue.venueName}`}
                        onError={(event) => {
                            event.currentTarget.hidden = true;
                            const placeholder = event.currentTarget.parentElement?.querySelector(".venue-image-placeholder");
                            if (placeholder) placeholder.hidden = false;
                        }}
                    />
                )}
                <div
                    className="venue-image-placeholder"
                    role="img"
                    aria-label={`No photo available for ${venue.venueName}`}
                    hidden={Boolean(venue.venueImageData)}
                >
                    <span>No venue photo</span>
                </div>
            </div>

            <div className="browse-venue-body">

                <div className="venue-card-title">
                    <h3>{venue.venueName}</h3>

                    <Status
                        status={venue.venueStatus}
                    />
                </div>

                <p className="venue-card-description">
                    {venue.venueDescription ||
                        "A flexible venue for meetings, conferences and events."}
                </p>

                <div className="venue-facts">

                    <span>
                        ⌖{" "}
                        {venue.location ||
                            "Location available on request"}
                    </span>

                    <span>
                        ♙ Capacity {venue.capacity}
                    </span>

                    <span>
                        R{" "}
                        {venue.price != null
                            ? Number(
                                  venue.price
                              ).toLocaleString(
                                  "en-ZA",
                                  {
                                      minimumFractionDigits: 2,
                                      maximumFractionDigits: 2
                                  }
                              )
                            : "Price on request"}
                    </span>

                </div>

                <button
                    className="outline-action"
                    type="button"
                    onClick={() => onOpen(venue)}
                >
                    Show More
                    <span>→</span>
                </button>

            </div>

        </article>
    );
}

export default VenueCard;