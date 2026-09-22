import { useCallback, useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";
import api from "../../services/api";
import conferenceCentre from "../../assets/images/Conference centre.jpg";
import theatreImage from "../../assets/images/Theatre (2).jpg";
import diningImage from "../../assets/images/Dining Room.jpg";
import { Loading } from "../../components/DashboardShared";
import Home from "../../components/organizer/Home";

import { clearSession, formatDate, shortId, statusName, statusClass, getDate, bookingStart, isFutureBooking, dateText, timeText, localInput, errorText, overlaps } from "../../utils/dashboardUtils";

const images = [conferenceCentre, theatreImage, diningImage];
const navItems = [["⌂", "Dashboard", "dashboard"], ["⌖", "Find a Venue", "venues"], ["▣", "My Bookings", "bookings"], ["◫", "My Events", "events"], ["◌", "Notifications", "notifications"]];
const blankEvent = { eventName: "", eventDescription: "", eventType: "", eventAttendance: "", startDateTime: "", endDateTime: "", venueId: "" };
const blankBooking = { eventId: "", venueId: "", startDateTime: "", endDateTime: "", specialRequirements: "" };

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
    const payForBooking = async (notification) => {
        if (!notification?.bookingId) {
            toast.error("There is no approved booking linked to this notification.");
            return;
        }

        try {
            const response = await api.post("/Payments", { bookingId: notification.bookingId });
            const checkoutUrl = response.data?.checkoutUrl;

            if (!checkoutUrl) {
                toast.error("Payment is not available for this booking yet.");
                return;
            }

            await markRead(notification);
            window.open(checkoutUrl, "_blank", "noopener,noreferrer");
            toast.success("Payment opened successfully.");
        } catch (error) {
            const message = error.response?.data?.message || error.response?.data || "Unable to start the payment flow.";
            toast.error(message);
        }
    };

    if (loading) return <div className="organizer-loading"><Loading label="Preparing your workspace" /></div>;
    const title = { dashboard: "Dashboard", venues: "Find a Venue", bookings: "My Bookings", events: "My Events", notifications: "Notifications", "venue-detail": "Venue Details", "booking-detail": "Booking Details", "event-form": editingEvent ? "Edit Event" : "Create Booking", "booking-form": editingBooking ? "Edit Booking" : "Create Booking" }[view];

    return <div className="organizer-app">
        {drawerOpen && <button className="organizer-overlay" aria-label="Close navigation" onClick={() => setDrawerOpen(false)} />}
        <aside className={`organizer-drawer ${drawerOpen ? "open" : ""}`}><div className="drawer-header"><div className="brand-lockup"><span className="brand-mark">D</span><div><strong>DIBA</strong><span>Bookings</span></div></div><button className="drawer-close" aria-label="Close menu" onClick={() => setDrawerOpen(false)}>×</button></div><p className="sidebar-label">Workspace</p><nav className="dashboard-nav" aria-label="Dashboard navigation">{navItems.map(([icon, label, key]) => <button key={key} className={`nav-item ${view === key ? "active" : ""}`} onClick={() => goTo(key)}><span className="nav-icon">{icon}</span><span>{label}</span>{key === "notifications" && unread > 0 && <span className="nav-count">{unread}</span>}</button>)}</nav><div className="sidebar-bottom"><button className="nav-item" onClick={() => toast.info("Profile settings are not available in the current API.")}><span className="nav-icon">◎</span><span>Profile</span></button><button className="nav-item" onClick={() => toast.info("Settings are not available in the current API.")}><span className="nav-icon">⚙</span><span>Settings</span></button><button className="nav-item logout-item" onClick={clearSession}><span className="nav-icon">↪</span><span>Logout</span></button></div></aside>
        <main className="organizer-main compact-main"><header className="organizer-header"><div className="header-title"><button className="menu-button" aria-label="Open menu" onClick={() => setDrawerOpen(true)}>☰</button><div><p className="header-kicker">DIBA BOOKINGS / ORGANISER DESK</p><h1>{title}</h1></div></div><div className="header-user"><button className="notification-button" aria-label="Open notifications" onClick={() => goTo("notifications")}>◌{unread > 0 && <i>{unread}</i>}</button><span className="avatar">{firstName.charAt(0).toUpperCase()}</span><div><strong>{fullName}</strong><span>Event Organiser</span></div></div></header>
            {view === "dashboard" && <Home firstName={firstName} events={events} bookings={bookings} upcoming={upcoming} pending={pending} approved={approved} unread={unread} onFind={() => goTo("venues")} onBooking={openBooking} onNotifications={() => goTo("notifications")} />}
            {view === "venues" && <VenueBrowser venues={filteredVenues} query={query} setQuery={setQuery} onOpen={openVenue} />}
            {view === "venue-detail" && activeVenue && <VenueDetails venue={activeVenue} image={images[venues.findIndex((venue) => venue.venueId === activeVenue.venueId) % images.length]} onBack={() => goTo("venues")} onBook={() => startEvent(activeVenue)} />}
            {view === "event-form" && <EventForm form={eventForm} setForm={setEventForm} selectedVenue={activeVenue} venues={availableVenues} saving={saving} editing={editingEvent} onSubmit={editingEvent ? saveEvent : submitEventAndBooking} onChangeVenue={(venue) => { setActiveVenue(venue); if (!venue) setEventForm((current) => ({ ...current, venueId: "" })); }} onCancel={returnFromForm} />}
            {view === "booking-form" && <BookingForm form={bookingForm} setForm={setBookingForm} selectedVenue={activeVenue} events={events} venues={availableVenues} saving={saving} editing={editingBooking} onSubmit={saveBooking} onCancel={returnFromForm} />}
            {view === "bookings" && <BookingsView bookings={bookings} onOpen={openBooking} onEdit={editBooking} onCancel={cancelBooking} saving={saving} />}
            {view === "booking-detail" && activeBooking && <BookingDetails booking={activeBooking} onBack={() => goTo("bookings")} onEdit={editBooking} onCancel={cancelBooking} saving={saving} />}
            {view === "events" && <EventsView events={events} bookings={bookings} venues={venues} onEdit={startEvent} onBook={(event) => startBooking(event, venues.find((venue) => venue.venueId === event.venueId))} />}
            {view === "notifications" && <NotificationsView notifications={notifications} onRead={markRead} onOpenBooking={(id) => { const booking = bookings.find((item) => item.bookingId === id); if (booking) openBooking(booking); }} onPay={payForBooking} />}
        </main>
    </div>;
}

export default OrganizerDashboard;