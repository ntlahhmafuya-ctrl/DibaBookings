import { useCallback, useEffect, useState } from "react";
import { toast } from "react-toastify";

import {
    cancelBooking as cancelBookingRequest,
    createBooking,
    getBookingAvailability,
    getBookings,
    updateBooking
} from "../services/bookingService";
import {
    createEvent,
    createEventWithBooking,
    getEvents,
    updateEvent
} from "../services/eventService";
import {
    getNotifications,
    markNotificationRead
} from "../services/notificationService";
import { createPaymentCheckout } from "../services/paymentService";
import { getVenueFeatures, getVenues } from "../services/venueService";
import {
    bookingStart,
    isFutureBooking,
    overlaps,
    statusName
} from "../utils/bookingUtils";
import { errorText } from "../utils/dashboardUtils";
import { localInput } from "../utils/dateUtils";

const blankEvent = {
    eventName: "",
    eventDescription: "",
    eventType: "",
    eventAttendance: "",
    startDateTime: "",
    endDateTime: "",
    venueId: "",
    acknowledgementAccepted: false
};

const blankBooking = {
    eventId: "",
    venueId: "",
    startDateTime: "",
    endDateTime: "",
    specialRequirements: ""
};

function useOrganizerDashboard() {
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


    const firstName =
        localStorage.getItem("firstName") ||
        "Event organiser";

    const fullName =
        [
            localStorage.getItem("firstName"),
            localStorage.getItem("lastName")
        ]
            .filter(Boolean)
            .join(" ") || firstName;


    const loadData = useCallback(async () => {
        setLoading(true);

        try {
            const [
                venueResponse,
                eventResponse,
                bookingResponse,
                notificationResponse
            ] = await Promise.all([
                getVenues(),
                getEvents(),
                getBookings(),
                getNotifications()
            ]);


            const venueList = venueResponse.data || [];


            /*
             * Load the features belonging to each venue.
             * The backend exposes these through:
             * GET /Venues/{venueId}/features
             */
            const featureResponses = await Promise.all(
                venueList.map((venue) =>
                    getVenueFeatures(venue.venueId).catch(
                        () => ({ data: [] })
                    )
                )
            );


            const venuesWithFeatures = venueList.map(
                (venue, index) => ({
                    ...venue,
                    features:
                        featureResponses[index].data || []
                })
            );


            setVenues(venuesWithFeatures);
            setEvents(eventResponse.data || []);
            setBookings(bookingResponse.data || []);
            setNotifications(notificationResponse.data || []);

        } catch (error) {
            toast.error(
                errorText(
                    error,
                    "We couldn't load your workspace. Please try again."
                )
            );
        } finally {
            setLoading(false);
        }
    }, []);


    useEffect(() => {
        loadData();
    }, [loadData]);


    const goTo = (nextView) => {
        setView(nextView);
        setDrawerOpen(false);
        setActiveVenue(null);
        setActiveBooking(null);
    };


    const openVenue = (venue) => {
        setActiveVenue(venue);
        setView("venue-detail");
        setDrawerOpen(false);
    };


    const openBooking = (booking) => {
        setActiveBooking(booking);
        setView("booking-detail");
        setDrawerOpen(false);
    };


    const availableVenues = venues.filter(
        (venue) =>
            venue.venueStatus?.toLowerCase() === "available"
    );


    const upcoming = bookings
        .filter(
            (booking) =>
                ![
                    "cancelled",
                    "rejected",
                    "completed"
                ].includes(
                    statusName(
                        booking.statusName
                    ).toLowerCase()
                ) &&
                isFutureBooking(booking, events)
        )
        .sort(
            (a, b) =>
                new Date(
                    bookingStart(a, events)
                ) -
                new Date(
                    bookingStart(b, events)
                )
        );


    const unread = notifications.filter(
        (notification) =>
            !notification.isRead
    ).length;


    const startEvent = (
        venue,
        event = null
    ) => {
        setActiveVenue(venue || null);

        setReturnView(
            venue
                ? "venue-detail"
                : "events"
        );

        setEditingEvent(event);


        setEventForm(
            event
                ? {
                      ...event,
                      startDateTime:
                          localInput(
                              event.startDateTime
                          ),
                      endDateTime:
                          localInput(
                              event.endDateTime
                          )
                  }
                : {
                      ...blankEvent,
                      venueId:
                          venue?.venueId || ""
                  }
        );


        setView("event-form");
        setDrawerOpen(false);
    };


    const returnFromForm = () => {
        setView(returnView);
        setDrawerOpen(false);
        setActiveBooking(null);

        if (
            returnView !==
            "venue-detail"
        ) {
            setActiveVenue(null);
        }
    };


    const startBooking = (
        event = null,
        venue = activeVenue
    ) => {
        const source =
            event ||
            events.find(
                (item) =>
                    item.eventId ===
                    bookingForm.eventId
            );


        setEditingBooking(null);


        setBookingForm({
            ...blankBooking,
            eventId:
                source?.eventId || "",
            venueId:
                venue?.venueId ||
                source?.venueId ||
                "",
            startDateTime:
                localInput(
                    source?.startDateTime
                ),
            endDateTime:
                localInput(
                    source?.endDateTime
                )
        });


        setView("booking-form");
        setDrawerOpen(false);
    };


    const saveEvent = async (event) => {
        event.preventDefault();
        setSaving(true);


        const payload = {
            ...eventForm,
            startDateTime:
                new Date(
                    eventForm.startDateTime
                ).toISOString(),
            endDateTime:
                new Date(
                    eventForm.endDateTime
                ).toISOString()
        };


        try {
            const response = editingEvent
                ? await updateEvent(editingEvent.eventId, payload)
                : await createEvent(payload);


            setEvents((current) =>
                editingEvent
                    ? current.map(
                          (item) =>
                              item.eventId ===
                              response.data.eventId
                                  ? response.data
                                  : item
                      )
                    : [
                          ...current,
                          response.data
                      ]
            );


            toast.success(
                editingEvent
                    ? "Event updated successfully."
                    : "Event created successfully."
            );


            if (editingEvent) {
                goTo("events");
            } else {
                startBooking(
                    response.data,
                    venues.find(
                        (venue) =>
                            venue.venueId ===
                            response.data.venueId
                    )
                );
            }

        } catch (error) {
            toast.error(
                errorText(
                    error,
                    "Unable to save the event. Check the details and try again."
                )
            );
        } finally {
            setSaving(false);
        }
    };


const submitEventAndBooking = async (event) => {
    event.preventDefault();

    if (!eventForm.acknowledgementAccepted) {
        toast.error(
            "Please confirm the booking acknowledgement before submitting."
        );
        return;
    }

    if (!eventForm.venueId) {
        toast.error(
            "Choose a venue before submitting."
        );
        return;
    }

    if (
        new Date(eventForm.endDateTime) <=
        new Date(eventForm.startDateTime)
    ) {
        toast.error(
            "End time must be after the start time."
        );
        return;
    }

    setSaving(true);

    try {
        toast.info(
            "Checking venue availability..."
        );

        const availability = await getBookingAvailability({
            venueId: eventForm.venueId,
            startDateTime: new Date(
                eventForm.startDateTime
            ).toISOString(),
            endDateTime: new Date(
                eventForm.endDateTime
            ).toISOString()
        });

        if (!availability.data.available) {
            toast.error(
                availability.data.reason ||
                "This venue is unavailable for the selected period."
            );
            return;
        }

        const response = await createEventWithBooking({
            ...eventForm,
            startDateTime: new Date(
                eventForm.startDateTime
            ).toISOString(),
            endDateTime: new Date(
                eventForm.endDateTime
            ).toISOString()
        });

        setEvents((current) => [
            ...current,
            response.data.event
        ]);

        setBookings((current) => [
            ...current,
            response.data.booking
        ]);

        setActiveBooking(
            response.data.booking
        );

        setActiveVenue(null);
        setView("booking-detail");

        toast.success(
            "Your event and venue booking have been submitted successfully. It is pending staff approval."
        );

    } catch (error) {
        toast.error(
            error.response?.status === 409
                ? "This venue is no longer available for the selected date and time. Please choose another time or venue."
                : errorText(
                    error,
                    "Unable to submit the event and booking."
                )
        );
    } finally {
        setSaving(false);
    }
};

    const saveBooking = async (event) => {
        event.preventDefault();


        const candidate = {
            ...bookingForm,
            startDateTime:
                new Date(
                    bookingForm.startDateTime
                ).toISOString(),
            endDateTime:
                new Date(
                    bookingForm.endDateTime
                ).toISOString()
        };


        if (
            new Date(
                candidate.endDateTime
            ) <=
            new Date(
                candidate.startDateTime
            )
        ) {
            toast.error(
                "End time must be after the start time."
            );
            return;
        }


        if (
            bookings.some(
                (booking) =>
                    overlaps(
                        booking,
                        candidate,
                        editingBooking?.bookingId
                    )
            )
        ) {
            toast.error(
                "This venue is already booked for the selected date and time. Please choose another time or venue."
            );
            return;
        }


        setSaving(true);


        try {
            const response =
                editingBooking
                    ? await updateBooking(editingBooking.bookingId, candidate)
                    : await createBooking(candidate);


            setBookings((current) =>
                editingBooking
                    ? current.map(
                          (booking) =>
                              booking.bookingId ===
                              response.data.bookingId
                                  ? {
                                        ...booking,
                                        ...response.data,
                                        ...candidate
                                    }
                                  : booking
                      )
                    : [
                          ...current,
                          response.data
                      ]
            );


            toast.success(
                editingBooking
                    ? "Booking updated successfully."
                    : "Booking submitted successfully. It is waiting for staff approval."
            );


            goTo("bookings");

        } catch (error) {
            toast.error(
                error.response?.status ===
                    409
                    ? "This venue has just been booked by another user. Please choose another venue or time."
                    : errorText(
                          error,
                          "Unable to submit the booking. Please check the details and try again."
                      )
            );
        } finally {
            setSaving(false);
        }
    };


    const cancelBooking = async (
        booking
    ) => {
        if (
            !window.confirm(
                "Cancel this booking? This action cannot be undone. Organiser cancellations receive a full refund at least 7 days before the event, a 50% refund from 72 hours to less than 7 days before the event, and no refund less than 72 hours before the event. Refunds are subject to payment-provider confirmation."
            )
        ) {
            return;
        }


        setSaving(true);


        try {
            const response = await cancelBookingRequest(booking.bookingId);
            const result = response.data || {};

            setBookings((current) =>
                current.map((item) =>
                    item.bookingId === booking.bookingId
                        ? { ...item, statusName: "Cancelled" }
                        : item
                )
            );

            const refundAmount = Number(result.refundAmount || 0);
            const refundStatus = String(result.refundStatus || "NotRequired");

            if (refundAmount > 0 && refundStatus.toLowerCase() === "succeeded") {
                toast.success(
                    `Booking cancelled. Yoco confirmed your R${refundAmount.toFixed(2)} refund. Your bank may take additional time to show the funds.`
                );
            } else if (refundAmount > 0 && refundStatus.toLowerCase() === "pending") {
                toast.info(
                    `Booking cancelled. Your R${refundAmount.toFixed(2)} refund is being processed. We will update the status when Yoco confirms the outcome.`
                );
            } else if (refundAmount > 0 && refundStatus.toLowerCase() === "needsreview") {
                toast.warning(
                    `Booking cancelled, but the R${refundAmount.toFixed(2)} refund needs staff review. Please do not submit another refund request.`
                );
            } else if (refundStatus.toLowerCase() === "noteligible") {
                toast.info(
                    "Booking cancelled. No refund is due under the current cancellation policy."
                );
            } else {
                toast.success(result.message || "Booking cancelled successfully.");
            }

        } catch (error) {
            toast.error(
                errorText(
                    error,
                    "Could not cancel this booking."
                )
            );
        } finally {
            setSaving(false);
        }
    };


    const editBooking = (
        booking
    ) => {
        setEditingBooking(booking);


        setActiveVenue(
            venues.find(
                (venue) =>
                    venue.venueId ===
                    booking.venueId
            ) || null
        );


        setReturnView(
            "booking-detail"
        );


        setBookingForm({
            eventId:
                booking.eventId,
            venueId:
                booking.venueId,
            startDateTime:
                localInput(
                    booking.startDateTime
                ),
            endDateTime:
                localInput(
                    booking.endDateTime
                ),
            specialRequirements:
                booking.specialRequirements ||
                ""
        });


        setView("booking-form");
    };


    const markRead = async (
        notification
    ) => {
        if (notification.isRead) {
            return;
        }


        try {
            await markNotificationRead(notification.notificationId);


            setNotifications(
                (current) =>
                    current.map(
                        (item) =>
                            item.notificationId ===
                            notification.notificationId
                                ? {
                                      ...item,
                                      isRead: true
                                  }
                                : item
                    )
            );

        } catch (error) {
            toast.error(
                errorText(
                    error,
                    "Could not update notification."
                )
            );
        }
    };


    const payForBooking = async (
        notification
    ) => {
        if (!notification?.bookingId) {
            toast.error(
                "There is no approved booking linked to this notification."
            );
            return;
        }


        try {
            const response = await createPaymentCheckout(
                notification.bookingId
            );


            const checkoutUrl =
                response.data?.checkoutUrl;


            if (!checkoutUrl) {
                toast.error(
                    "Payment is not available for this booking yet."
                );
                return;
            }


            await markRead(
                notification
            );


            window.open(
                checkoutUrl,
                "_blank",
                "noopener,noreferrer"
            );


            toast.success(
                "Payment opened successfully."
            );

        } catch (error) {
            const message =
                error.response?.data
                    ?.message ||
                error.response?.data ||
                "Unable to start the payment flow.";


            toast.error(message);
        }
    };

    const title = {
        dashboard: "Dashboard",
        venues: "Find a Venue",
        bookings: "My Bookings",
        events: "My Events",
        notifications: "Notifications",
        "venue-detail": "Venue Details",
        "booking-detail": "Booking Details",
        "event-form": editingEvent ? "Edit Event" : "Create Booking",
        "booking-form": editingBooking ? "Edit Booking" : "Create Booking"
    }[view];

    return {
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
    };
}

export default useOrganizerDashboard;
