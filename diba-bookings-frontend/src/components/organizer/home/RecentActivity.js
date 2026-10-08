import { dateText, getDate } from "../../../utils/dateUtils";

function RecentActivity({ bookings, onBooking }) {
    if (bookings.length === 0) {
        return null;
    }

    return (
        <section className="home-section">
            <div className="home-section-heading">
                <div>
                    <p className="section-kicker">ACTIVITY</p>
                    <h3>Recent activity</h3>
                    <p>A quick overview of your latest bookings.</p>
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
                        <div className="activity-icon">▣</div>
                        <div className="activity-content">
                            <strong>
                                {booking.statusName || "Pending"} booking
                            </strong>
                            <span>
                                {booking.eventName || "Event booking"}
                            </span>
                        </div>
                        <time>{dateText(getDate(booking))}</time>
                        <span className="activity-arrow">→</span>
                    </button>
                ))}
            </div>
        </section>
    );
}

export default RecentActivity;
