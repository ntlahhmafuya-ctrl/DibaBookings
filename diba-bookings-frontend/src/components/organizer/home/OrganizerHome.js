import Empty from "../Empty";
import BookingCard from "../BookingsDetails/BookingCard";
import { dateText, getDate } from "../../../utils/dashboardUtils";

function Home({
    firstName,
    bookings,
    upcoming,
    onFind,
    onBooking
}) {
    return (
        <section className="organizer-home">

            {/* Welcome section */}
            <section className="welcome-panel">
                <div className="welcome-content">
                    <p className="section-kicker">
                        YOUR WORKSPACE
                    </p>

                    <h2>
                        Good morning, {firstName}
                    </h2>

                    <p>
                        Welcome to your booking workspace.
                        Find a venue, manage your events and
                        keep track of your bookings.
                    </p>
                </div>

                <button
                    type="button"
                    className="primary-action"
                    onClick={onFind}
                >
                    Find a Venue
                </button>
            </section>


            {/* Upcoming bookings */}
            <section className="home-section">

                <div className="home-section-heading">
                    <div>
                        <p className="section-kicker">
                            YOUR SCHEDULE
                        </p>

                        <h3>
                            Upcoming bookings
                        </h3>

                        <p>
                            Your next scheduled events.
                        </p>
                    </div>

                    {upcoming.length > 0 && (
                        <button
                            type="button"
                            className="text-action"
                            onClick={() => onBooking(upcoming[0])}
                        >
                            View bookings
                        </button>
                    )}
                </div>


                {upcoming.length ? (
                    <div className="home-bookings-grid">
                        {upcoming.slice(0, 2).map((booking) => (
                            <BookingCard
                                key={booking.bookingId}
                                booking={booking}
                                onOpen={onBooking}
                            />
                        ))}
                    </div>
                ) : (
                    <div className="home-empty-card">
                        <Empty
                            title="No bookings yet"
                            text="Find a venue and create your first booking."
                            action="Find a Venue"
                            onAction={onFind}
                        />
                    </div>
                )}

            </section>


            {/* Recent activity */}
            {bookings.length > 0 && (
                <section className="home-section">

                    <div className="home-section-heading">
                        <div>
                            <p className="section-kicker">
                                ACTIVITY
                            </p>

                            <h3>
                                Recent activity
                            </h3>

                            <p>
                                A quick overview of your latest bookings.
                            </p>
                        </div>
                    </div>


                    <div className="home-activity-list">

                        {bookings.slice(0, 4).map((booking) => (
                            <button
                                type="button"
                                className="home-activity-item"
                                key={booking.bookingId}
                                onClick={() => onBooking(booking)}
                            >
                                <div className="activity-icon">
                                    ▣
                                </div>

                                <div className="activity-content">
                                    <strong>
                                        {booking.statusName || "Pending"} booking
                                    </strong>

                                    <span>
                                        {booking.eventName || "Event booking"}
                                    </span>
                                </div>

                                <time>
                                    {dateText(getDate(booking))}
                                </time>

                                <span className="activity-arrow">
                                    →
                                </span>
                            </button>
                        ))}

                    </div>

                </section>
            )}

        </section>
    );
}

export default Home;