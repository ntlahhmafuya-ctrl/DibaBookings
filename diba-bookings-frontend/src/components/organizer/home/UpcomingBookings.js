import EmptyState from "../../common/EmptyState";
import BookingCard from "../bookings/BookingCard";

function UpcomingBookings({ bookings, onFind, onBooking }) {
    return (
        <section className="home-section">
            <div className="home-section-heading">
                <div>
                    <p className="section-kicker">YOUR SCHEDULE</p>
                    <h3>Upcoming bookings</h3>
                    <p>Your next scheduled events.</p>
                </div>
                {bookings.length > 0 && (
                    <button
                        type="button"
                        className="text-action"
                        onClick={() => onBooking(bookings[0])}
                    >
                        View bookings
                    </button>
                )}
            </div>

            {bookings.length > 0 ? (
                <div className="home-bookings-grid">
                    {bookings.slice(0, 2).map((booking) => (
                        <BookingCard
                            key={booking.bookingId}
                            booking={booking}
                            onOpen={onBooking}
                        />
                    ))}
                </div>
            ) : (
                <div className="home-empty-card">
                    <EmptyState
                        title="No bookings yet"
                        text="Find a venue and create your first booking."
                        action="Find a Venue"
                        onAction={onFind}
                    />
                </div>
            )}
        </section>
    );
}

export default UpcomingBookings;
