import { useState } from "react";
import conferenceCentre from "../../../assets/images/Conference centre.jpg";
import theatreImage from "../../../assets/images/Theatre (2).jpg";
import diningImage from "../../../assets/images/Dining Room.jpg";
import VenueCard from "./VenueCard";
import Empty from "../Empty";

const images = [
    conferenceCentre,
    theatreImage,
    diningImage
];

function VenueBrowser({
    venues,
    query,
    setQuery,
    onOpen
}) {
    const [searchFilters, setSearchFilters] = useState({
        minCapacity: "",
        maxCapacity: "",
        maxPrice: "",
        status: ""
    });

    const handleFilterChange = (field, value) => {
        setSearchFilters((current) => ({
            ...current,
            [field]: value
        }));
    };

    const clearFilters = () => {
        setSearchFilters({
            minCapacity: "",
            maxCapacity: "",
            maxPrice: "",
            status: ""
        });

        setQuery("");
    };

    const filteredVenues = venues.filter((venue) => {
        const search = query.trim().toLowerCase();

        const matchesSearch =
            !search ||
            venue.venueName?.toLowerCase().includes(search) ||
            venue.venueDescription?.toLowerCase().includes(search) ||
            venue.location?.toLowerCase().includes(search);

        const capacity = Number(venue.capacity) || 0;
        const price = Number(venue.price) || 0;

        const matchesMinCapacity =
            !searchFilters.minCapacity ||
            capacity >= Number(searchFilters.minCapacity);

        const matchesMaxCapacity =
            !searchFilters.maxCapacity ||
            capacity <= Number(searchFilters.maxCapacity);

        const matchesMaxPrice =
            !searchFilters.maxPrice ||
            price <= Number(searchFilters.maxPrice);

        const matchesStatus =
            !searchFilters.status ||
            venue.venueStatus?.toLowerCase() ===
                searchFilters.status.toLowerCase();

        return (
            matchesSearch &&
            matchesMinCapacity &&
            matchesMaxCapacity &&
            matchesMaxPrice &&
            matchesStatus
        );
    });

    return (
        <section className="workspace-view">

            <div className="workspace-toolbar">

                <div>
                    <p className="section-kicker">
                        VENUE DIRECTORY
                    </p>

                    <h2>
                        Choose a space that fits.
                    </h2>
                </div>

                <input
                    className="workspace-search"
                    placeholder="Search by venue or location"
                    value={query}
                    onChange={(event) =>
                        setQuery(event.target.value)
                    }
                />

            </div>

            <div className="venue-filters">

                <input
                    type="number"
                    min="0"
                    placeholder="Minimum capacity"
                    value={searchFilters.minCapacity}
                    onChange={(event) =>
                        handleFilterChange(
                            "minCapacity",
                            event.target.value
                        )
                    }
                />

                <input
                    type="number"
                    min="0"
                    placeholder="Maximum capacity"
                    value={searchFilters.maxCapacity}
                    onChange={(event) =>
                        handleFilterChange(
                            "maxCapacity",
                            event.target.value
                        )
                    }
                />

                <input
                    type="number"
                    min="0"
                    placeholder="Maximum price"
                    value={searchFilters.maxPrice}
                    onChange={(event) =>
                        handleFilterChange(
                            "maxPrice",
                            event.target.value
                        )
                    }
                />

                <select
                    value={searchFilters.status}
                    onChange={(event) =>
                        handleFilterChange(
                            "status",
                            event.target.value
                        )
                    }
                >
                    <option value="">
                        All statuses
                    </option>

                    <option value="Available">
                        Available
                    </option>

                    <option value="Unavailable">
                        Unavailable
                    </option>
                </select>

                <button
                    type="button"
                    onClick={clearFilters}
                >
                    Clear filters
                </button>

            </div>

            {filteredVenues.length ? (

                <div className="venue-browser-grid">

                    {filteredVenues.map((venue, index) => (

                        <VenueCard
                            key={venue.venueId}
                            venue={venue}
                            image={
                                images[
                                    index % images.length
                                ]
                            }
                            onOpen={onOpen}
                        />

                    ))}

                </div>

            ) : (

                <Empty
                    title="No venues found"
                    text="Try changing your search or filters."
                />

            )}

        </section>
    );
}

export default VenueBrowser;