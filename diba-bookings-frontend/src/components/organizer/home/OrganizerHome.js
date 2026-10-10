import { useMemo } from "react";

import EmptyState from "../../common/EmptyState";
import VenueCard from "../venues/VenueCard";

function OrganizerHome({
    firstName,
    venues,
    query,
    setQuery,
    onOpenVenue,
    onFindVenue
}) {
    const greeting = useMemo(() => {
        const hour = new Date().getHours();

        if (hour < 12) {
            return "Good morning";
        }

        if (hour < 18) {
            return "Good afternoon";
        }

        return "Good evening";
    }, []);

    const filteredVenues = useMemo(() => {
        const search = query.trim().toLowerCase();

        if (!search) {
            return venues;
        }

        const requestedCapacity = /^\d+$/.test(search)
            ? Number(search)
            : null;

        return venues.filter((venue) => {
            const venueName =
                venue.venueName?.toLowerCase() || "";

            const description =
                venue.venueDescription?.toLowerCase() || "";

            const location =
                venue.location?.toLowerCase() || "";

            const capacity =
                Number(venue.capacity) || 0;

            if (requestedCapacity !== null) {
                return capacity >= requestedCapacity;
            }

            return (
                venueName.includes(search) ||
                description.includes(search) ||
                location.includes(search)
            );
        });
    }, [venues, query]);

    return (
        <main className="organizer-home">

            {/* =====================================================
                WORKSPACE INTRODUCTION
               ===================================================== */}

            <section className="home-introduction">

                <div className="home-section-label">
                    YOUR WORKSPACE
                </div>

                <div className="home-introduction-content">
                    <div>
                        <h1>
                            {greeting},{" "}
                            <em>{firstName}</em>
                        </h1>

                        <p>
                            Find the right space for what
                            you're planning.
                        </p>
                    </div>

                    <div className="home-introduction-note">
                        <span className="home-note-line" />
                        <span>
                            Spaces for ideas, connection
                            and progress.
                        </span>
                    </div>
                </div>

            </section>


            {/* =====================================================
                VENUE DISCOVERY
               ===================================================== */}

            <section className="home-discovery">

                <div className="home-discovery-heading">

                    <div>
                        <p className="landing-kicker">
                            FIND A SPACE
                        </p>

                        <h2>
                            Search for a venue that
                            <br />
                            <em>fits your event.</em>
                        </h2>
                    </div>

                    <p>
                        Search by venue name, location
                        or the number of attendees.
                    </p>

                </div>


                <div className="home-discovery-form">

                    <div className="home-search-field">

                        <span
                            className="home-search-icon"
                            aria-hidden="true"
                        >
                            ⌕
                        </span>

                        <div>
                            <small>Search venues</small>

                            <input
                                type="search"
                                value={query}
                                onChange={(event) =>
                                    setQuery(event.target.value)
                                }
                                placeholder="Venue, location or attendees"
                                aria-label="Search venues"
                            />
                        </div>

                        {query && (
                            <button
                                type="button"
                                className="home-search-clear"
                                onClick={() => setQuery("")}
                                aria-label="Clear venue search"
                            >
                                
                            </button>
                        )}

                    </div>


                    <button
                        type="button"
                        className="home-advanced-link"
                        onClick={onFindVenue}
                    >
                        Advanced search
                        <span aria-hidden="true">↗</span>
                    </button>

                </div>

            </section>


            {/* =====================================================
                VENUE COLLECTION
               ===================================================== */}

            <section className="home-venue-section">

                <div className="home-section-heading">

                    <div>
                        <p className="landing-kicker">
                            THE VENUE COLLECTION
                        </p>

                        <h2>
                            Explore our <em>venues.</em>
                        </h2>
                    </div>

                    <p>
                        Purposeful spaces for conferences,
                        meetings, presentations and events.
                    </p>

                </div>


                <div className="home-venue-grid">

                    {filteredVenues.length > 0 ? (
                        filteredVenues.map((venue) => {

                            return (
                                <VenueCard
                                    key={venue.venueId}
                                    venue={venue}
                                    onOpen={onOpenVenue}
                                />
                            );
                        })
                    ) : (
                        <div className="home-empty">

                            <EmptyState
                                title="No venues found"
                                text="Try another venue name, location or attendee number."
                            />

                        </div>
                    )}

                </div>

            </section>

        </main>
    );
}

export default OrganizerHome;