import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import api from "../services/api";
import conferenceCentre from "../images/Conference centre.jpg";
import theatreImage from "../images/Theatre (2).jpg";
import diningImage from "../images/Dining Room.jpg";
import { Loading } from "./DashboardShared";
import { clearSession, formatDate, shortId } from "./dashboardUtils";

const images = [conferenceCentre, theatreImage, diningImage];
const navItems = [["⌂", "Dashboard", "dashboard"], ["⌖", "Find a Venue", "venues"], ["▣", "My Bookings", "bookings"], ["◫", "My Events", "events"], ["◌", "Notifications", "notifications"]];
const blankEvent = { eventName: "", eventDescription: "", eventType: "", eventAttendance: "", startDateTime: "", endDateTime: "", venueId: "" };
const blankBooking = { eventId: "", venueId: "", startDateTime: "", endDateTime: "", specialRequirements: "" };

const statusName = (status = "Pending") => String(status || "Pending").trim();
const statusClass = (status = "Pending") => statusName(status).toLowerCase().replace(/\s/g, "-");
const getDate = (item) => item.startDateTime || item.bookingDate || item.date;
const bookingStart = (booking, events) => booking.startDateTime || events.find((event) => event.eventId === booking.eventId)?.startDateTime;
const isFutureBooking = (booking, events) => {
    const start = new Date(bookingStart(booking, events));
    return !Number.isNaN(start.getTime()) && start >= new Date();
};
const dateText = (value) => value ? new Date(value).toLocaleDateString([], { day: "2-digit", month: "short", year: "numeric" }) : "-";
const timeText = (value) => value ? new Date(value).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }) : "-";
const localInput = (value) => value ? new Date(value).toISOString().slice(0, 16) : "";
const errorText = (error, fallback) => error.response?.data?.message || error.response?.data || fallback;
const overlaps = (booking, candidate, excludeId) => booking.bookingId !== excludeId && ["Pending", "Approved"].includes(booking.statusName) && booking.venueId === candidate.venueId && new Date(booking.startDateTime) < new Date(candidate.endDateTime) && new Date(booking.endDateTime) > new Date(candidate.startDateTime);

function OrganizerDashboard() {
    const [view, setView] = useState("dashboard");
    const [drawerOpen, setDrawerOpen] = useState(false);
    const [venues, setVenues] = useState([]);
    const [events, setEvents] = useState([]);
    const [bookings, setBookings] = useState([]);
    const [notifications, setNotifications] = useState([]);
    const [activeVenue, setActiveVenue] = useState(null);
    const [activeBooking, setActiveBooking] = useState(null);
    const [returnView, setReturnView] = useState("events");
    const [editingEvent, setEditingEvent] = useState(null);
    const [editingBooking, setEditingBooking] = useState(null);
    const [eventForm, setEventForm] = useState(blankEvent);
    const [bookingForm, setBookingForm] = useState(blankBooking);
    const [query, setQuery] = useState("");
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const firstName = localStorage.getItem("firstName") || "Event organiser";
    const fullName = [localStorage.getItem("firstName"), localStorage.getItem("lastName")].filter(Boolean).join(" ") || firstName;

    const loadData = useCallback(async () => {
        setLoading(true);
        try {
            const [venueResponse, eventResponse, bookingResponse, notificationResponse] = await Promise.all([api.get("/Venues"), api.get("/Events"), api.get("/Bookings"), api.get("/Notifications")]);
            const venueList = venueResponse.data || [];
            const featureResponses = await Promise.all(venueList.map((venue) => api.get(`/Venues/${venue.venueId}/features`).catch(() => ({ data: [] }))));
            setVenues(venueList.map((venue, index) => ({ ...venue, features: featureResponses[index].data || [] })));
            setEvents(eventResponse.data || []);
            setBookings(bookingResponse.data || []);
            setNotifications(notificationResponse.data || []);
        } catch (error) {
            toast.error(errorText(error, "We couldn't load your workspace. Please try again."));
        } finally { setLoading(false); }
    }, []);

    useEffect(() => { loadData(); }, [loadData]);

    const goTo = (nextView) => { setView(nextView); setDrawerOpen(false); setActiveVenue(null); setActiveBooking(null); };
    const openVenue = (venue) => { setActiveVenue(venue); setView("venue-detail"); setDrawerOpen(false); };
    const openBooking = (booking) => { setActiveBooking(booking); setView("booking-detail"); setDrawerOpen(false); };
    const availableVenues = venues.filter((venue) => venue.venueStatus?.toLowerCase() === "available");
    const filteredVenues = useMemo(() => venues.filter((venue) => `${venue.venueName} ${venue.location} ${venue.venueDescription}`.toLowerCase().includes(query.toLowerCase())), [venues, query]);
    const upcoming = bookings.filter((booking) => !["cancelled", "rejected", "completed"].includes(statusName(booking.statusName).toLowerCase()) && isFutureBooking(booking, events)).sort((a, b) => new Date(bookingStart(a, events)) - new Date(bookingStart(b, events)));
    const pending = bookings.filter((booking) => statusName(booking.statusName).toLowerCase() === "pending").length;
    const approved = bookings.filter((booking) => statusName(booking.statusName).toLowerCase() === "approved").length;
    const unread = notifications.filter((notification) => !notification.isRead).length;

    const startEvent = (venue, event = null) => {
        setActiveVenue(venue || null); setReturnView(venue ? "venue-detail" : "events"); setEditingEvent(event);
        setEventForm(event ? { ...event, startDateTime: localInput(event.startDateTime), endDateTime: localInput(event.endDateTime) } : { ...blankEvent, venueId: venue?.venueId || "" });
        setView("event-form"); setDrawerOpen(false);
    };

    const returnFromForm = () => {
        setView(returnView);
        setDrawerOpen(false);
        setActiveBooking(null);
        if (returnView !== "venue-detail") setActiveVenue(null);
    };

    const startBooking = (event = null, venue = activeVenue) => {
        const source = event || events.find((item) => item.eventId === bookingForm.eventId);
        setEditingBooking(null);
        setBookingForm({ ...blankBooking, eventId: source?.eventId || "", venueId: venue?.venueId || source?.venueId || "", startDateTime: localInput(source?.startDateTime), endDateTime: localInput(source?.endDateTime) });
        setView("booking-form"); setDrawerOpen(false);
    };

    const saveEvent = async (event) => {
        event.preventDefault(); setSaving(true);
        const payload = { ...eventForm, startDateTime: new Date(eventForm.startDateTime).toISOString(), endDateTime: new Date(eventForm.endDateTime).toISOString() };
        try {
            const response = editingEvent ? await api.put(`/Events/${editingEvent.eventId}`, payload) : await api.post("/Events", payload);
            setEvents((current) => editingEvent ? current.map((item) => item.eventId === response.data.eventId ? response.data : item) : [...current, response.data]);
            toast.success(editingEvent ? "Event updated successfully." : "Event created successfully.");
            editingEvent ? goTo("events") : startBooking(response.data, venues.find((venue) => venue.venueId === response.data.venueId));
        } catch (error) { toast.error(errorText(error, "Unable to save the event. Check the details and try again.")); }
        finally { setSaving(false); }
    };

    const submitEventAndBooking = async (event) => {
        event.preventDefault();
        if (!eventForm.venueId) { toast.error("Choose a venue before submitting."); return; }
        if (new Date(eventForm.endDateTime) <= new Date(eventForm.startDateTime)) { toast.error("End time must be after the start time."); return; }
        setSaving(true);
        try {
            toast.info("Checking venue availability...");
            const availability = await api.get("/Bookings/availability", { params: { venueId: eventForm.venueId, startDateTime: new Date(eventForm.startDateTime).toISOString(), endDateTime: new Date(eventForm.endDateTime).toISOString() } });
            if (!availability.data.available) { toast.error(availability.data.reason || "This venue is unavailable for the selected period."); return; }
            const response = await api.post("/Events/with-booking", { ...eventForm, startDateTime: new Date(eventForm.startDateTime).toISOString(), endDateTime: new Date(eventForm.endDateTime).toISOString() });
            setEvents((current) => [...current, response.data.event]);
            setBookings((current) => [...current, response.data.booking]);
            setActiveBooking(response.data.booking);
            setActiveVenue(null);
            setView("booking-detail");
            toast.success("Your event and venue booking have been submitted successfully. It is pending staff approval.");
        } catch (error) {
            toast.error(error.response?.status === 409 ? "This venue is no longer available for the selected date and time. Please choose another time or venue." : errorText(error, "Unable to submit the event and booking."));
        } finally { setSaving(false); }
    };

    const saveBooking = async (event) => {
        event.preventDefault();
        const candidate = { ...bookingForm, startDateTime: new Date(bookingForm.startDateTime).toISOString(), endDateTime: new Date(bookingForm.endDateTime).toISOString() };
        if (new Date(candidate.endDateTime) <= new Date(candidate.startDateTime)) { toast.error("End time must be after the start time."); return; }
        if (bookings.some((booking) => overlaps(booking, candidate, editingBooking?.bookingId))) { toast.error("This venue is already booked for the selected date and time. Please choose another time or venue."); return; }
        setSaving(true);
        try {
            const response = editingBooking ? await api.put(`/Bookings/${editingBooking.bookingId}`, candidate) : await api.post("/Bookings", candidate);
            setBookings((current) => editingBooking ? current.map((booking) => booking.bookingId === response.data.bookingId ? { ...booking, ...response.data, ...candidate } : booking) : [...current, response.data]);
            toast.success(editingBooking ? "Booking updated successfully." : "Booking submitted successfully. It is waiting for staff approval.");
            goTo("bookings");
        } catch (error) {
            toast.error(error.response?.status === 409 ? "This venue has just been booked by another user. Please choose another venue or time." : errorText(error, "Unable to submit the booking. Please check the details and try again."));
        } finally { setSaving(false); }
    };

    const cancelBooking = async (booking) => {
        if (!window.confirm("Cancel this booking? This action cannot be undone.")) return;
        setSaving(true);
        try { await api.put(`/Bookings/${booking.bookingId}/cancel`); setBookings((current) => current.map((item) => item.bookingId === booking.bookingId ? { ...item, statusName: "Cancelled" } : item)); toast.success("Booking cancelled successfully."); }
        catch (error) { toast.error(errorText(error, "Could not cancel this booking.")); }
        finally { setSaving(false); }
    };

    const editBooking = (booking) => { setEditingBooking(booking); setActiveVenue(venues.find((venue) => venue.venueId === booking.venueId) || null); setReturnView("booking-detail"); setBookingForm({ eventId: booking.eventId, venueId: booking.venueId, startDateTime: localInput(booking.startDateTime), endDateTime: localInput(booking.endDateTime), specialRequirements: booking.specialRequirements || "" }); setView("booking-form"); };
    const markRead = async (notification) => { if (notification.isRead) return; try { await api.put(`/Notifications/${notification.notificationId}/read`); setNotifications((current) => current.map((item) => item.notificationId === notification.notificationId ? { ...item, isRead: true } : item)); } catch (error) { toast.error(errorText(error, "Could not update notification.")); } };

    if (loading) return <div className="organizer-loading"><Loading label="Preparing your workspace" /></div>;
    const title = { dashboard: "Dashboard", venues: "Find a Venue", bookings: "My Bookings", events: "My Events", notifications: "Notifications", "venue-detail": "Venue Details", "booking-detail": "Booking Details", "event-form": editingEvent ? "Edit Event" : "Create Event", "booking-form": editingBooking ? "Edit Booking" : "Book a Venue" }[view];

    return <div className="organizer-app">
        {drawerOpen && <button className="organizer-overlay" aria-label="Close navigation" onClick={() => setDrawerOpen(false)} />}
        <aside className={`organizer-drawer ${drawerOpen ? "open" : ""}`}><div className="drawer-header"><div className="brand-lockup"><span className="brand-mark">D</span><div><strong>DIBA</strong><span>Bookings</span></div></div><button className="drawer-close" aria-label="Close menu" onClick={() => setDrawerOpen(false)}>×</button></div><p className="sidebar-label">Workspace</p><nav className="dashboard-nav" aria-label="Dashboard navigation">{navItems.map(([icon, label, key]) => <button key={key} className={`nav-item ${view === key ? "active" : ""}`} onClick={() => goTo(key)}><span className="nav-icon">{icon}</span><span>{label}</span>{key === "notifications" && unread > 0 && <span className="nav-count">{unread}</span>}</button>)}</nav><div className="sidebar-bottom"><button className="nav-item" onClick={() => toast.info("Profile settings are not available in the current API.")}><span className="nav-icon">◎</span><span>Profile</span></button><button className="nav-item" onClick={() => toast.info("Settings are not available in the current API.")}><span className="nav-icon">⚙</span><span>Settings</span></button><button className="nav-item logout-item" onClick={clearSession}><span className="nav-icon">↪</span><span>Logout</span></button></div></aside>
        <main className="organizer-main compact-main"><header className="organizer-header"><div className="header-title"><button className="menu-button" aria-label="Open menu" onClick={() => setDrawerOpen(true)}>☰</button><div><p className="header-kicker">DIBA BOOKINGS / ORGANISER DESK</p><h1>{title}</h1></div></div><div className="header-user"><button className="notification-button" aria-label="Open notifications" onClick={() => goTo("notifications")}>◌{unread > 0 && <i>{unread}</i>}</button><span className="avatar">{firstName.charAt(0).toUpperCase()}</span><div><strong>{fullName}</strong><span>Event Organiser</span></div></div></header>
            {view === "dashboard" && <Home firstName={firstName} events={events} bookings={bookings} upcoming={upcoming} pending={pending} approved={approved} unread={unread} onFind={() => goTo("venues")} onBooking={openBooking} onNotifications={() => goTo("notifications")} />}
            {view === "venues" && <VenueBrowser venues={filteredVenues} query={query} setQuery={setQuery} onOpen={openVenue} />}
            {view === "venue-detail" && activeVenue && <VenueDetails venue={activeVenue} image={images[venues.findIndex((venue) => venue.venueId === activeVenue.venueId) % images.length]} onBack={() => goTo("venues")} onBook={() => startEvent(activeVenue)} onCreateEvent={() => startEvent(activeVenue)} />}
            {view === "event-form" && <EventForm form={eventForm} setForm={setEventForm} selectedVenue={activeVenue} venues={availableVenues} saving={saving} editing={editingEvent} onSubmit={editingEvent ? saveEvent : submitEventAndBooking} onChangeVenue={(venue) => { setActiveVenue(venue); if (!venue) setEventForm((current) => ({ ...current, venueId: "" })); }} onCancel={returnFromForm} />}
            {view === "booking-form" && <BookingForm form={bookingForm} setForm={setBookingForm} selectedVenue={activeVenue} events={events} venues={availableVenues} saving={saving} editing={editingBooking} onSubmit={saveBooking} onCancel={returnFromForm} />}
            {view === "bookings" && <BookingsView bookings={bookings} onOpen={openBooking} onEdit={editBooking} onCancel={cancelBooking} saving={saving} />}
            {view === "booking-detail" && activeBooking && <BookingDetails booking={activeBooking} onBack={() => goTo("bookings")} onEdit={editBooking} onCancel={cancelBooking} saving={saving} />}
            {view === "events" && <EventsView events={events} bookings={bookings} venues={venues} onCreate={() => startEvent()} onEdit={startEvent} onBook={(event) => startBooking(event, venues.find((venue) => venue.venueId === event.venueId))} />}
            {view === "notifications" && <NotificationsView notifications={notifications} onRead={markRead} onOpenBooking={(id) => { const booking = bookings.find((item) => item.bookingId === id); if (booking) openBooking(booking); }} />}
        </main>
    </div>;
}

function Home({ firstName, events, bookings, upcoming, pending, approved, unread, onFind, onBooking, onNotifications }) { return <><section className="compact-welcome"><div><p className="welcome-eyebrow">YOUR BOOKING SPACE</p><h2>Good morning, {firstName}</h2><p>{events.length} {events.length === 1 ? "event" : "events"} in your workspace.</p></div><button className="primary-action" onClick={onFind}>⌖ Find a Venue</button></section><section className="summary-grid compact-summary"><SummaryCard icon="◷" label="Upcoming" value={upcoming.length} tone="blue" /><SummaryCard icon="…" label="Pending" value={pending} tone="yellow" /><SummaryCard icon="✓" label="Approved" value={approved} tone="green" /></section><section className="dashboard-grid compact-grid"><div className="dashboard-column"><SectionHeading kicker="YOUR SCHEDULE" title="Upcoming bookings" action="View bookings" onClick={() => document.querySelector(".menu-button")?.click()} />{upcoming.length ? <div className="booking-list">{upcoming.slice(0, 2).map((booking) => <BookingCard key={booking.bookingId} booking={booking} onOpen={onBooking} />)}</div> : <Empty title="No bookings yet" text="Find a venue and create your first booking." action="Find a Venue" onAction={onFind} />}<SectionHeading kicker="RECENT ACTIVITY" title="Booking activity" action="Notifications" onClick={onNotifications} /><div className="activity-list">{bookings.slice(0, 4).map((booking) => <div className="activity-item" key={booking.bookingId}><span className={`activity-icon ${statusClass(booking.statusName)}`}>{booking.statusName === "Approved" ? "✓" : booking.statusName === "Rejected" ? "!" : "•"}</span><div><strong>{booking.statusName || "Pending"} booking</strong><p>{booking.eventName || "Event booking"}</p></div><time>{dateText(getDate(booking))}</time></div>)}</div></div><aside className="dashboard-rail compact-rail"><div className="updates-panel"><SectionHeading kicker="STAY IN THE LOOP" title="Notifications" action="Open" onClick={onNotifications} /><div className="update-summary"><strong>{unread ? `${unread} unread update${unread === 1 ? "" : "s"}` : "You're all caught up"}</strong><p>Important booking updates will appear here.</p></div></div><div className="venue-prompt compact-prompt"><div className="venue-prompt-content"><p className="section-kicker">READY TO PLAN?</p><h3>Find a space for your next event.</h3><button onClick={onFind}>Explore venues <span>→</span></button></div></div></aside></section></>; }
function VenueBrowser({ venues, query, setQuery, onOpen }) { return <section className="workspace-view"><div className="workspace-toolbar"><div><p className="section-kicker">VENUE DIRECTORY</p><h2>Choose a space that fits.</h2></div><input className="workspace-search" placeholder="Search venues or locations" value={query} onChange={(event) => setQuery(event.target.value)} /></div>{venues.length ? <div className="venue-browser-grid">{venues.map((venue, index) => <VenueCard key={venue.venueId} venue={venue} image={images[index % images.length]} onOpen={onOpen} />)}</div> : <Empty title="No venues found" text="Try another venue name or location." />}</section>; }
function VenueCard({ venue, image, onOpen }) { return <article className="browse-venue-card"><img src={image} alt={venue.venueName} /><div className="browse-venue-body"><div className="venue-card-title"><h3>{venue.venueName}</h3><Status status={venue.venueStatus} /></div><p>{venue.venueDescription || "A flexible venue for meetings, conferences and events."}</p><div className="venue-facts"><span>⌖ {venue.location || "Location available on request"}</span><span>♙ Capacity {venue.capacity}</span></div><button className="outline-action" onClick={() => onOpen(venue)}>View details <span>↗</span></button></div></article>; }
function VenueDetails({ venue, image, onBack, onBook, onCreateEvent }) { const available = venue.venueStatus?.toLowerCase() === "available"; return <section className="detail-view"><button className="back-link" onClick={onBack}>← Back to venues</button><div className="venue-detail-layout"><img src={image} alt={venue.venueName} /><div className="detail-copy"><Status status={venue.venueStatus} /><h2>{venue.venueName}</h2><p>{venue.venueDescription || "A flexible venue for conferences, meetings and campus events."}</p><dl><div><dt>Location</dt><dd>{venue.location || "Not specified"}</dd></div><div><dt>Capacity</dt><dd>{venue.capacity} guests</dd></div></dl><h3>Facilities</h3><div className="feature-chips">{venue.features?.length ? venue.features.map((feature) => <span key={feature.venueFeatureId}>{feature.featureName}</span>) : <span>Facilities information not listed</span>}</div><div className="detail-actions">{available ? <button className="primary-action" onClick={onBook}>Book This Venue</button> : <button className="disabled-action" disabled>Venue Unavailable</button>}<button className="outline-action" onClick={onCreateEvent}>Create an event here</button></div></div></div></section>; }
function EventForm({ form, setForm, selectedVenue, venues, saving, editing, onSubmit, onChangeVenue, onCancel }) { return <section className="form-view"><FormIntro title={editing ? "Edit event" : "Create event and booking"} text="Your event and venue request will be submitted together for staff approval." /><form className="workflow-form" onSubmit={onSubmit}><div className="form-grid"><Field label="Event name" required><input required value={form.eventName} onChange={(e) => setForm({ ...form, eventName: e.target.value })} /></Field><Field label="Event type"><input placeholder="Conference, meeting, workshop..." value={form.eventType} onChange={(e) => setForm({ ...form, eventType: e.target.value })} /></Field><Field label="Venue" required>{selectedVenue ? <div className="selected-venue"><strong>{selectedVenue.venueName}</strong><span>{selectedVenue.location} · Capacity {selectedVenue.capacity}</span><button type="button" onClick={() => onChangeVenue(null)}>Change Venue</button></div> : <select required value={form.venueId} onChange={(e) => { const venue = venues.find((item) => item.venueId === e.target.value); setForm({ ...form, venueId: e.target.value }); onChangeVenue(venue); }}><option value="">Choose a venue</option>{venues.map((venue) => <option key={venue.venueId} value={venue.venueId}>{venue.venueName}</option>)}</select>}</Field><Field label="Expected attendees"><input value={form.eventAttendance} onChange={(e) => setForm({ ...form, eventAttendance: e.target.value })} placeholder="e.g. 50" /></Field><Field label="Start date and time" required><input required type="datetime-local" value={form.startDateTime} onChange={(e) => setForm({ ...form, startDateTime: e.target.value })} /></Field><Field label="End date and time" required><input required type="datetime-local" value={form.endDateTime} onChange={(e) => setForm({ ...form, endDateTime: e.target.value })} /></Field></div><Field label="Description" required><textarea required rows="4" value={form.eventDescription} onChange={(e) => setForm({ ...form, eventDescription: e.target.value })} /></Field>{!editing && <p className="form-note">This submits the event and venue booking as one action. Status after submission: <strong>Pending approval</strong></p>}<FormActions saving={saving} submit={editing ? "Save Changes" : "Submit Event & Booking"} onCancel={onCancel} /></form></section>; }
function BookingForm({ form, setForm, selectedVenue, events, venues, saving, editing, onSubmit, onCancel }) { const event = events.find((item) => item.eventId === form.eventId); return <section className="form-view"><FormIntro title="Edit booking" text="The venue and time are checked again before changes are saved." /><div className="booking-summary"><strong>{event?.eventName || "Event"}</strong><span>{selectedVenue?.venueName || venues.find((venue) => venue.venueId === form.venueId)?.venueName || "Venue"}</span><span>{form.startDateTime ? `${dateText(form.startDateTime)} · ${timeText(form.startDateTime)} - ${timeText(form.endDateTime)}` : "Date and time to be selected"}</span></div><form className="workflow-form" onSubmit={onSubmit}><div className="form-grid"><Field label="Event" required><select required value={form.eventId} onChange={(e) => setForm({ ...form, eventId: e.target.value })}>{events.map((item) => <option key={item.eventId} value={item.eventId}>{item.eventName}</option>)}</select></Field><Field label="Venue" required><div className="selected-venue"><strong>{selectedVenue?.venueName || "Selected venue"}</strong><span>{selectedVenue?.location || "Venue context retained"}</span></div></Field><Field label="Start date and time" required><input required type="datetime-local" value={form.startDateTime} onChange={(e) => setForm({ ...form, startDateTime: e.target.value })} /></Field><Field label="End date and time" required><input required type="datetime-local" value={form.endDateTime} onChange={(e) => setForm({ ...form, endDateTime: e.target.value })} /></Field></div><Field label="Special requirements"><textarea rows="3" value={form.specialRequirements} onChange={(e) => setForm({ ...form, specialRequirements: e.target.value })} placeholder="Accessibility, room layout, equipment..." /></Field><p className="form-note">Approved bookings cannot be edited. Pending bookings remain editable.</p><FormActions saving={saving} submit="Save Changes" onCancel={onCancel} /></form></section>; }
function BookingsView({ bookings, onOpen, onEdit, onCancel, saving }) { return <section className="workspace-view"><div className="workspace-toolbar"><div><p className="section-kicker">YOUR REQUESTS</p><h2>My bookings</h2></div></div>{bookings.length ? <div className="records-list">{bookings.map((booking) => <BookingRow key={booking.bookingId} booking={booking} onOpen={onOpen} onEdit={onEdit} onCancel={onCancel} saving={saving} />)}</div> : <Empty title="No bookings yet" text="Find a venue and create your first booking." />}</section>; }
function BookingRow({ booking, onOpen, onEdit, onCancel, saving }) { const status = booking.statusName || "Pending"; return <article className="record-row"><div className="record-main"><span className={`status-dot ${statusClass(status)}`} /><div><strong>{booking.eventName || "Untitled event"}</strong><p>{booking.venueName || "Venue"} · {dateText(booking.startDateTime)} · {timeText(booking.startDateTime)} - {timeText(booking.endDateTime)}</p></div></div><Status status={status} /><div className="record-actions"><button onClick={() => onOpen(booking)}>View Details</button>{status === "Pending" && <button onClick={() => onEdit(booking)}>Edit</button>}{!["Cancelled", "Rejected", "Completed"].includes(status) && <button className="danger-link" disabled={saving} onClick={() => onCancel(booking)}>Cancel</button>}</div></article>; }
function BookingDetails({ booking, onBack, onEdit, onCancel, saving }) { const status = booking.statusName || "Pending"; return <section className="detail-view"><button className="back-link" onClick={onBack}>← Back to bookings</button><div className="booking-detail-panel"><div className="detail-panel-heading"><div><p className="section-kicker">BOOKING REFERENCE</p><h2>{shortId(booking.bookingId)}</h2></div><Status status={status} /></div><div className="booking-detail-grid"><Detail label="Event" value={booking.eventName} /><Detail label="Venue" value={booking.venueName} /><Detail label="Date" value={dateText(booking.startDateTime)} /><Detail label="Time" value={`${timeText(booking.startDateTime)} - ${timeText(booking.endDateTime)}`} /><Detail label="Created" value={formatDate(booking.bookingDate)} /><Detail label="Status" value={status} /></div>{status === "Rejected" && booking.adminNotes && <div className="notice rejected-notice"><strong>Reason from Conference Centre Staff</strong><p>{booking.adminNotes}</p></div>}<div className="booking-timeline"><h3>Booking timeline</h3><p>✓ Booking submitted <small>{formatDate(booking.bookingDate)}</small></p><p>{status === "Pending" ? "• Booking waiting for review" : "✓ Booking reviewed"}</p></div><div className="detail-actions">{status === "Pending" && <button className="outline-action" onClick={() => onEdit(booking)}>Edit booking</button>}{!["Cancelled", "Rejected", "Completed"].includes(status) && <button className="danger-button" disabled={saving} onClick={() => onCancel(booking)}>Cancel booking</button>}</div></div></section>; }
function EventsView({ events, bookings, venues, onCreate, onEdit, onBook }) { return <section className="workspace-view"><div className="workspace-toolbar"><div><p className="section-kicker">YOUR EVENTS</p><h2>My events</h2></div><button className="primary-action" onClick={onCreate}>+ Create Event</button></div>{events.length ? <div className="records-list">{events.map((event) => { const booking = bookings.find((item) => item.eventId === event.eventId); return <article className="record-row" key={event.eventId}><div><strong>{event.eventName}</strong><p>{venues.find((venue) => venue.venueId === event.venueId)?.venueName || "Venue"} · {dateText(event.startDateTime)} · {event.eventType || "Event"}</p></div><Status status={booking?.statusName || "No booking"} /><div className="record-actions"><button onClick={() => onEdit(venues.find((venue) => venue.venueId === event.venueId), event)}>Edit</button>{!booking && <button onClick={() => onBook(event)}>Book venue</button>}</div></article>; })}</div> : <Empty title="No events yet" text="Create an event to start planning your venue booking." action="Create Event" onAction={onCreate} />}</section>; }
function NotificationsView({ notifications, onRead, onOpenBooking }) { return <section className="workspace-view"><div className="workspace-toolbar"><div><p className="section-kicker">STAY INFORMED</p><h2>Notifications</h2></div></div>{notifications.length ? <div className="notifications-list">{notifications.map((notification) => <button className={`notification-row ${notification.isRead ? "read" : "unread"}`} key={notification.notificationId} onClick={() => { onRead(notification); if (notification.bookingId) onOpenBooking(notification.bookingId); }}><span className="notification-bullet">{notification.isRead ? "✓" : "•"}</span><span><strong>{notification.notificationType}</strong><p>{notification.message}</p><small>{formatDate(notification.dateCreated)}</small></span></button>)}</div> : <Empty title="You're all caught up" text="You don't have any new notifications." />}</section>; }
function SummaryCard({ icon, label, value, tone }) { return <div className={`organizer-summary-card ${tone}`}><span className="summary-icon">{icon}</span><div><strong>{value}</strong><span>{label}</span></div></div>; }
function BookingCard({ booking, onOpen }) { return <article className="booking-card compact-booking"><div className="booking-card-body"><div className="booking-card-top"><div><p className="booking-label">{booking.eventName || "Untitled event"}</p><h4>{booking.venueName || "Venue details pending"}</h4></div><Status status={booking.statusName} /></div><div className="booking-meta"><span>▣ {dateText(getDate(booking))}</span><span>◷ {timeText(booking.startDateTime)} - {timeText(booking.endDateTime)}</span></div><div className="booking-card-footer"><button className="outline-action" onClick={() => onOpen(booking)}>View booking</button></div></div></article>; }
function Status({ status = "Pending" }) { return <span className={`status-pill ${statusClass(status)}`}>{status === "Approved" ? "✓ " : status === "Rejected" ? "! " : status === "Cancelled" ? "○ " : status === "Completed" ? "◆ " : "● "}{status}</span>; }
function SectionHeading({ kicker, title, action, onClick }) { return <div className="section-heading"><div><p className="section-kicker">{kicker}</p><h3>{title}</h3></div>{action && <button className="link-button" onClick={onClick}>{action} <span>→</span></button>}</div>; }
function FormIntro({ title, text }) { return <div className="form-intro"><p className="section-kicker">BOOKING WORKFLOW</p><h2>{title}</h2><p>{text}</p></div>; }
function Field({ label, required, children }) { return <label className="field"><span>{label}{required && " *"}</span>{children}</label>; }
function FormActions({ saving, submit, onCancel }) { return <div className="form-actions"><button type="button" className="outline-action" onClick={onCancel}>Cancel</button><button type="submit" className="primary-action" disabled={saving}>{saving ? "Saving..." : submit}</button></div>; }
function Detail({ label, value }) { return <div><dt>{label}</dt><dd>{value || "-"}</dd></div>; }
function Empty({ title, text, action, onAction }) { return <div className="organizer-empty-state"><span className="empty-icon">⌖</span><h4>{title}</h4><p>{text}</p>{action && <button className="outline-action" onClick={onAction}>{action}</button>}</div>; }

export default OrganizerDashboard;
