import Status from "../Status";

function VenueCard({ venue, image, onOpen }) {
    return (
        <article className="browse-venue-card">
            <img
                src={image}
                alt={venue.venueName}
            />

            <div className="browse-venue-body">
                <div className="venue-card-title">
                    <h3>{venue.venueName}</h3>

                    <Status
                        status={venue.venueStatus}
                    />
                </div>

                <p>
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
                            ? Number(venue.price).toLocaleString(
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
                    View details <span>↗</span>
                </button>
            </div>
        </article>
    );
}

export default VenueCard;