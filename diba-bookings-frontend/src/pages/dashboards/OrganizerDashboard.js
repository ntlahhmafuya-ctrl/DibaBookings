import conferenceCentre from "../../assets/images/Conference centre.jpg";
import theatreImage from "../../assets/images/Theatre (2).jpg";
import diningImage from "../../assets/images/Dining Room.jpg";

import LoadingIndicator from "../../components/common/LoadingIndicator";
import OrganizerHome from "../../components/organizer/home/OrganizerHome";
import VenueBrowser from "../../components/organizer/venues/VenueBrowser";
import VenueDetails from "../../components/organizer/venues/VenueDetails";
import EventForm from "../../components/organizer/events/EventForm";
import EventsView from "../../components/organizer/events/EventsView";
import BookingForm from "../../components/organizer/bookings/BookingForm";
import BookingsView from "../../components/organizer/bookings/BookingsView";
import BookingDetails from "../../components/organizer/bookings/BookingDetails";
import NotificationsView from "../../components/organizer/notifications/NotificationsView";
import OrganizerSidebar from "../../components/organizer/navigation/OrganizerSidebar";
import OrganizerTopNav from "../../components/organizer/navigation/OrganizerTopNav";
import useOrganizerDashboard from "../../hooks/useOrganizerDashboard";

const images = [conferenceCentre, theatreImage, diningImage];

function OrganizerDashboard() {
    const {
        view,
        drawerOpen,
        setDrawerOpen,
        bookings,
        upcoming,
        firstName,
        fullName,
        unread,
        loading,
        title,
        goTo,
        openBooking,
        openVenue,
        startEvent,
        startBooking,
        returnFromForm,
        saveEvent,
        submitEventAndBooking,
        saveBooking,
        cancelBooking,
        editBooking,
        markRead,
        payForBooking,
        venues,
        events,
        notifications,
        query,
        setQuery,
        activeVenue,
        activeBooking,
        availableVenues,
        eventForm,
        setEventForm,
        bookingForm,
        setBookingForm,
        editingEvent,
        editingBooking,
        saving,
        setActiveVenue
    } = useOrganizerDashboard();

    if (loading) {
        return (
            <div className="organizer-loading">
                <LoadingIndicator label="Preparing your workspace" />
            </div>
        );
    }

        return (
            <div className="organizer-app">

            <OrganizerSidebar
                view={view}
                unread={unread}
                drawerOpen={drawerOpen}
                setDrawerOpen={setDrawerOpen}
                goTo={goTo}
            />

            <main className="organizer-main compact-main">

                <OrganizerTopNav
                    title={title}
                    firstName={firstName}
                    fullName={fullName}
                    unread={unread}
                    setDrawerOpen={setDrawerOpen}
                    goTo={goTo}
                />

{view === "dashboard" && (
    <OrganizerHome
        firstName={firstName}
        venues={venues}
        query={query}
        setQuery={setQuery}
        onOpenVenue={openVenue}
        onFindVenue={() => goTo("venues")}
    />
)}

                {view === "venues" && (
                    <VenueBrowser
                        venues={venues}
                        query={query}
                        setQuery={setQuery}
                        onOpen={openVenue}
                    />
                )}

                {view === "venue-detail" &&
                    activeVenue && (
                        <VenueDetails
                            venue={activeVenue}
                            image={
                                images[
                                    venues.findIndex(
                                        (venue) =>
                                            venue.venueId ===
                                            activeVenue.venueId
                                    ) %
                                        images.length
                                ]
                            }
                            onBack={() =>
                                goTo("venues")
                            }
                            onBook={() =>
                                startEvent(activeVenue)
                            }
                        />
                    )}

                {view === "event-form" && (
                    <EventForm
                        form={eventForm}
                        setForm={setEventForm}
                        selectedVenue={activeVenue}
                        venues={availableVenues}
                        saving={saving}
                        editing={editingEvent}
                        onSubmit={
                            editingEvent
                                ? saveEvent
                                : submitEventAndBooking
                        }
                        onChangeVenue={(venue) => {
                            setActiveVenue(venue);

                            if (!venue) {
                                setEventForm(
                                    (current) => ({
                                        ...current,
                                        venueId: ""
                                    })
                                );
                            }
                        }}
                        onCancel={returnFromForm}
                    />
                )}

                {view === "booking-form" && (
                    <BookingForm
                        form={bookingForm}
                        setForm={setBookingForm}
                        selectedVenue={activeVenue}
                        events={events}
                        venues={availableVenues}
                        saving={saving}
                        editing={editingBooking}
                        onSubmit={saveBooking}
                        onCancel={returnFromForm}
                    />
                )}

                {view === "bookings" && (
                    <BookingsView
                        bookings={bookings}
                        onOpen={openBooking}
                        onEdit={editBooking}
                        onCancel={cancelBooking}
                        saving={saving}
                    />
                )}

                {view === "booking-detail" &&
                    activeBooking && (
                        <BookingDetails
                            booking={activeBooking}
                            onBack={() =>
                                goTo("bookings")
                            }
                            onEdit={editBooking}
                            onCancel={cancelBooking}
                            saving={saving}
                        />
                    )}

                {view === "events" && (
                    <EventsView
                        events={events}
                        bookings={bookings}
                        venues={venues}
                        onEdit={startEvent}
                        onBook={(event) =>
                            startBooking(
                                event,
                                venues.find(
                                    (venue) =>
                                        venue.venueId ===
                                        event.venueId
                                )
                            )
                        }
                    />
                )}

                {view === "notifications" && (
                    <NotificationsView
                        notifications={notifications}
                        onRead={markRead}
                        onOpenBooking={(id) => {
                            const booking =
                                bookings.find(
                                    (item) =>
                                        item.bookingId ===
                                        id
                                );

                            if (booking) {
                                openBooking(booking);
                            }
                        }}
                        onPay={payForBooking}
                    />
                )}

            </main>
        </div>
        );
}

export default OrganizerDashboard;
