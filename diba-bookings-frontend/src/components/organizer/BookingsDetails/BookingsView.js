import BookingRow from "../BookingsView/BookingRow";
import Empty from "../Empty";

function BookingsView({ bookings, onOpen, onEdit, onCancel, saving }) { return <section className="workspace-view"><div className="workspace-toolbar"><div><p className="section-kicker">YOUR REQUESTS</p><h2>My bookings</h2></div></div>{bookings.length ? <div className="records-list">{bookings.map((booking) => <BookingRow key={booking.bookingId} booking={booking} onOpen={onOpen} onEdit={onEdit} onCancel={onCancel} saving={saving} />)}</div> : <Empty title="No bookings yet" text="Find a venue and create your first booking." />}</section>; }

export default BookingsView;

