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
        <section>

            <section>
                <h2>Good morning, {firstName}</h2>

                <p>
                    Welcome to your booking workspace.
                </p>

                <button
                    type="button"
                    onClick={onFind}
                >
                    Find a Venue
                </button>
            </section>

            <section>
                <p>YOUR SCHEDULE</p>

                <h3>Upcoming bookings</h3>

                {upcoming.length ? (
                    <div>
                        {upcoming
                            .slice(0, 2)
                            .map((booking) => (
                                <BookingCard
                                    key={booking.bookingId}
                                    booking={booking}
                                    onOpen={onBooking}
                                />
                            ))}
                    </div>
                ) : (
                    <Empty
                        title="No bookings yet"
                        text="Find a venue and create your first booking."
                        action="Find a Venue"
                        onAction={onFind}
                    />
                )}
            </section>

            {bookings.length > 0 && (
                <section>
                    <p>ACTIVITY</p>

                    <h3>Recent activity</h3>

                    <div>
                        {bookings
                            .slice(0, 4)
                            .map((booking) => (
                                <div key={booking.bookingId}>

                                    <strong>
                                        {booking.statusName ||
                                            "Pending"}{" "}
                                        booking
                                    </strong>

                                    <p>
                                        {booking.eventName ||
                                            "Event booking"}
                                    </p>

                                    <time>
                                        {dateText(
                                            getDate(booking)
                                        )}
                                    </time>

                                </div>
                            ))}
                    </div>
                </section>
            )}

        </section>
    );
}

export default Home;