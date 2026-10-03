import WelcomePanel from "./WelcomePanel";
import UpcomingBookings from "./UpcomingBookings";
import RecentActivity from "./RecentActivity";

function OrganizerHome({
    firstName,
    bookings,
    upcoming,
    onFind,
    onBooking
}) {
    return (
        <section className="organizer-home">
            <WelcomePanel firstName={firstName} onFind={onFind} />
            <UpcomingBookings
                bookings={upcoming}
                onFind={onFind}
                onBooking={onBooking}
            />
            <RecentActivity bookings={bookings} onBooking={onBooking} />
        </section>
    );
}

export default OrganizerHome;