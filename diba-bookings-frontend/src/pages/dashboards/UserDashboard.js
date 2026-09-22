import { useEffect, useState } from "react";
import { Badge, Table } from "react-bootstrap";
import { toast } from "react-toastify";
import api from "../../services/api";
import { Loading, RoleShell, Stat } from "./DashboardShared";
import { clearSession, shortId } from "../../utils/dashboardUtils";

function UserDashboard() {
    const [bookings, setBookings] = useState([]);
    const [venues, setVenues] = useState([]);
    const [loading, setLoading] = useState(true);
    const firstName = localStorage.getItem("firstName") || "Staff member";

    const loadData = async () => {
        try {
            const [bookingResponse, venueResponse] = await Promise.all([api.get("/Bookings"), api.get("/Venues")]);
            setBookings(bookingResponse.data);
            setVenues(venueResponse.data);
        } catch (error) { toast.error(error.response?.data || "Could not load staff workspace."); }
        finally { setLoading(false); }
    };

    useEffect(() => { loadData(); }, []);

    const reviewBooking = async (booking, action) => {
        try {
            if (action === "approve") await api.put(`/Bookings/${booking.bookingId}/approve`);
            else await api.put(`/Bookings/${booking.bookingId}/reject`, { reason: window.prompt("Reason for rejection:") || "Administrative decision" });
            toast.success(`Booking ${action}d.`);
            loadData();
        } catch (error) { toast.error(error.response?.data || "Could not update booking."); }
    };

    return <RoleShell title={`Welcome, ${firstName}`} eyebrow="DIBA BOOKINGS / STAFF DESK" subtitle="Review venue requests, maintain venue information, and keep bookings moving." onLogout={clearSession}>
        {loading ? <Loading /> : <><section className="stat-grid"><Stat label="Pending review" value={bookings.filter((booking) => booking.statusName === "Pending").length} tone="amber" /><Stat label="Approved" value={bookings.filter((booking) => booking.statusName === "Approved").length} tone="green" /><Stat label="Venues" value={venues.length} /><Stat label="Available venues" value={venues.filter((venue) => venue.venueStatus === "Available").length} tone="ink" /></section><section className="content-grid"><div className="panel table-panel"><div className="panel-heading"><div><p className="eyebrow">BOOKING QUEUE</p><h2>Requests requiring attention</h2></div></div><Table responsive hover><thead><tr><th>Event</th><th>Organiser</th><th>Venue</th><th>Status</th><th>Actions</th></tr></thead><tbody>{bookings.map((booking) => <tr key={booking.bookingId}><td>{booking.eventName || shortId(booking.eventId)}</td><td>{booking.organiserName || shortId(booking.userId)}</td><td>{booking.venueName || shortId(booking.venueId)}</td><td><Badge bg={booking.statusName === "Pending" ? "warning" : "light"} text="dark">{booking.statusName || "Unknown"}</Badge></td><td>{booking.statusName === "Pending" && <><button className="table-action" onClick={() => reviewBooking(booking, "approve")}>Approve</button><button className="table-action danger" onClick={() => reviewBooking(booking, "reject")}>Reject</button></>}</td></tr>)}</tbody></Table></div><div className="panel"><p className="eyebrow">VENUE DIRECTORY</p><h2>Venues</h2>{venues.map((venue) => <div className="compact-row" key={venue.venueId}><div><strong>{venue.venueName}</strong><p>{venue.location} / capacity {venue.capacity}</p></div><Badge bg={venue.venueStatus === "Available" ? "success" : "secondary"}>{venue.venueStatus}</Badge></div>)}</div></section></>}
    </RoleShell>;
}

export default UserDashboard;
