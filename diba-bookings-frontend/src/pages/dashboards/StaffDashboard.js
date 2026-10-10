import { useEffect, useState } from "react";
import { Badge, Button, Form, Modal, Table } from "react-bootstrap";
import { toast } from "react-toastify";

import DashboardShell from "../../components/common/DashboardShell";
import LoadingIndicator from "../../components/common/LoadingIndicator";
import StatCard from "../../components/common/StatCard";
import {
    approveBooking,
    cancelBooking,
    getBookings,
    rejectBooking
} from "../../services/bookingService";
import { getPayments, retryFailedRefund } from "../../services/paymentService";
import {
    createVenue,
    createVenueFeature,
    deleteVenueFeature,
    getVenueFeatures,
    getVenues,
    updateVenue,
    updateVenueFeature
} from "../../services/venueService";
import { shortId } from "../../utils/dashboardUtils";
import { clearSession } from "../../utils/sessionUtils";

const emptyVenueForm = {
    venueName: "",
    venueDescription: "",
    capacity: "",
    price: "",
    location: "",
    latitude: "",
    longitude: "",
    venueStatus: "Available",
    venueImageData: ""
};

const emptyFeatureForm = {
    featureName: "",
    featureDescription: "",
    featureStatus: "Available"
};

function errorMessage(error, fallback) {
    const data = error.response?.data;
    if (typeof data === "string") return data;
    if (data?.message) return data.message;
    if (data?.title) return data.title;
    if (data?.errors) {
        return Object.values(data.errors).flat().join(" ");
    }
    return fallback;
}

function StaffDashboard() {
    const [bookings, setBookings] = useState([]);
    const [payments, setPayments] = useState([]);
    const [venues, setVenues] = useState([]);
    const [activeScreen, setActiveScreen] = useState("overview");
    const [bookingSearch, setBookingSearch] = useState("");
    const [bookingStatusFilter, setBookingStatusFilter] = useState("All");
    const [selectedBooking, setSelectedBooking] = useState(null);
    const [bookingDetailsOpen, setBookingDetailsOpen] = useState(false);
    const [retryingPaymentId, setRetryingPaymentId] = useState(null);
    const [selectedVenueImage, setSelectedVenueImage] = useState(null);
    const [loading, setLoading] = useState(true);
    const [savingVenue, setSavingVenue] = useState(false);
    const [venueModalOpen, setVenueModalOpen] = useState(false);
    const [editingVenue, setEditingVenue] = useState(null);
    const [venueForm, setVenueForm] = useState(emptyVenueForm);
    const [facilityModalOpen, setFacilityModalOpen] = useState(false);
    const [facilityVenue, setFacilityVenue] = useState(null);
    const [facilities, setFacilities] = useState([]);
    const [facilitiesLoading, setFacilitiesLoading] = useState(false);
    const [savingFacility, setSavingFacility] = useState(false);
    const [editingFacility, setEditingFacility] = useState(null);
    const [facilityForm, setFacilityForm] = useState(emptyFeatureForm);
    const firstName = localStorage.getItem("firstName") || "Staff member";

    const loadData = async () => {
        try {
            const [bookingResponse, venueResponse] = await Promise.all([
                getBookings(),
                getVenues()
            ]);
            setBookings(Array.isArray(bookingResponse.data) ? bookingResponse.data : []);
            setVenues(Array.isArray(venueResponse.data) ? venueResponse.data : []);

            // Payment visibility is important for staff operations, but a temporary
            // payment endpoint issue should not hide the booking and venue workspace.
            try {
                const paymentResponse = await getPayments();
                setPayments(Array.isArray(paymentResponse.data) ? paymentResponse.data : []);
            } catch (paymentError) {
                setPayments([]);
                toast.error(errorMessage(paymentError, "Could not load payment and refund information."));
            }
        } catch (error) {
            toast.error(errorMessage(error, "Could not load staff workspace."));
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadData();
    }, []);

    const reviewBooking = async (booking, action) => {
        try {
            if (action === "approve") {
                await approveBooking(booking.bookingId);
            } else {
                const reason = window.prompt("Reason for rejection:");
                if (reason === null) return;
                if (!reason.trim()) {
                    toast.error("Enter a reason before rejecting this booking.");
                    return;
                }
                await rejectBooking(booking.bookingId, reason.trim());
            }
            toast.success(`Booking ${action === "approve" ? "approved" : "rejected"}.`);
            await loadData();
        } catch (error) {
            toast.error(errorMessage(error, "Could not update booking."));
        }
    };

    const openBookingDetails = (booking) => {
        setSelectedBooking(booking);
        setBookingDetailsOpen(true);
    };

    const cancelBookingAsStaff = async (booking) => {
        const confirmed = window.confirm(
            `Cancel the booking for "${booking.eventName || "this event"}"? DIBA-initiated cancellations receive a full refund when a successful payment exists. This action cannot be undone.`
        );
        if (!confirmed) return;

        try {
            await cancelBooking(booking.bookingId);
            toast.success("Booking cancelled. Check the refund status in the booking details.");
            setBookingDetailsOpen(false);
            setSelectedBooking(null);
            await loadData();
        } catch (error) {
            toast.error(errorMessage(error, "Could not cancel this booking."));
        }
    };

    const retryRefund = async (payment) => {
        if (!window.confirm(`Retry the failed refund of R${Number(payment.refundAmount || 0).toFixed(2)}? Only retry when Yoco has confirmed that the previous attempt failed.`)) return;
        setRetryingPaymentId(payment.paymentId);
        try {
            await retryFailedRefund(payment.paymentId);
            toast.success("Refund retry submitted. Refresh the booking details to see its latest status.");
            await loadData();
        } catch (error) {
            toast.error(errorMessage(error, "Could not retry the refund."));
        } finally {
            setRetryingPaymentId(null);
        }
    };

    const filteredBookings = bookings.filter((booking) => {
        const query = bookingSearch.trim().toLowerCase();
        const matchesQuery = !query || [
            booking.bookingId, booking.eventName, booking.organiserName,
            booking.venueName, booking.statusName, booking.specialRequirements
        ].some((value) => String(value || "").toLowerCase().includes(query));
        const matchesStatus = bookingStatusFilter === "All" ||
            String(booking.statusName || "Unknown").toLowerCase() === bookingStatusFilter.toLowerCase();
        return matchesQuery && matchesStatus;
    });

    const formatDateTime = (value) => value
        ? new Date(value).toLocaleString(undefined, { dateStyle: "medium", timeStyle: "short" })
        : "Not recorded";

    const openCreateVenue = () => {
        setEditingVenue(null);
        setVenueForm({ ...emptyVenueForm });
        setVenueModalOpen(true);
    };

    const openEditVenue = (venue) => {
        setEditingVenue(venue);
        setVenueForm({
            venueName: venue.venueName || "",
            venueDescription: venue.venueDescription || "",
            capacity: String(venue.capacity ?? ""),
            price: String(venue.price ?? ""),
            location: venue.location || "",
            latitude: String(venue.latitude ?? ""),
            longitude: String(venue.longitude ?? ""),
            venueStatus: venue.venueStatus || "Available",
            venueImageData: venue.venueImageData || ""
        });
        setVenueModalOpen(true);
    };

    const closeVenueModal = () => {
        if (!savingVenue) setVenueModalOpen(false);
    };

    const saveVenue = async (event) => {
        event.preventDefault();

        const capacity = Number(venueForm.capacity);
        const price = Number(venueForm.price);
        const latitude = Number(venueForm.latitude);
        const longitude = Number(venueForm.longitude);

        if (!venueForm.venueName.trim() || !venueForm.location.trim()) {
            toast.error("Venue name and location are required.");
            return;
        }
        if (!Number.isInteger(capacity) || capacity < 1) {
            toast.error("Capacity must be a whole number greater than zero.");
            return;
        }
        if (!Number.isFinite(price) || price < 0) {
            toast.error("Price must be zero or greater.");
            return;
        }
        if (!Number.isFinite(latitude) || latitude < -90 || latitude > 90) {
            toast.error("Latitude must be between -90 and 90.");
            return;
        }
        if (!Number.isFinite(longitude) || longitude < -180 || longitude > 180) {
            toast.error("Longitude must be between -180 and 180.");
            return;
        }

        const payload = {
            venueName: venueForm.venueName.trim(),
            venueDescription: venueForm.venueDescription.trim(),
            capacity,
            price,
            location: venueForm.location.trim(),
            latitude,
            longitude,
            venueStatus: venueForm.venueStatus,
            venueImageData: venueForm.venueImageData || null
        };

        setSavingVenue(true);
        try {
            if (editingVenue) {
                await updateVenue(editingVenue.venueId, payload);
                toast.success("Venue updated successfully.");
            } else {
                await createVenue(payload);
                toast.success("Venue added successfully.");
            }
            setVenueModalOpen(false);
            await loadData();
        } catch (error) {
            toast.error(errorMessage(error, "Could not save venue."));
        } finally {
            setSavingVenue(false);
        }
    };

    const openFacilities = async (venue) => {
        setFacilityVenue(venue);
        setFacilities([]);
        setEditingFacility(null);
        setFacilityForm({ ...emptyFeatureForm });
        setFacilityModalOpen(true);
        setFacilitiesLoading(true);
        try {
            const response = await getVenueFeatures(venue.venueId);
            setFacilities(Array.isArray(response.data) ? response.data : []);
        } catch (error) {
            toast.error(errorMessage(error, "Could not load venue facilities."));
        } finally {
            setFacilitiesLoading(false);
        }
    };

    const resetFacilityForm = () => {
        setEditingFacility(null);
        setFacilityForm({ ...emptyFeatureForm });
    };

    const saveFacility = async (event) => {
        event.preventDefault();
        if (!facilityVenue) return;
        if (!facilityForm.featureName.trim()) {
            toast.error("Facility name is required.");
            return;
        }

        const payload = {
            featureName: facilityForm.featureName.trim(),
            featureDescription: facilityForm.featureDescription.trim(),
            featureStatus: facilityForm.featureStatus
        };

        setSavingFacility(true);
        try {
            if (editingFacility) {
                await updateVenueFeature(
                    facilityVenue.venueId,
                    editingFacility.venueFeatureId,
                    payload
                );
                toast.success("Facility updated.");
            } else {
                await createVenueFeature(facilityVenue.venueId, payload);
                toast.success("Facility added.");
            }
            const response = await getVenueFeatures(facilityVenue.venueId);
            setFacilities(Array.isArray(response.data) ? response.data : []);
            resetFacilityForm();
        } catch (error) {
            toast.error(errorMessage(error, "Could not save facility."));
        } finally {
            setSavingFacility(false);
        }
    };

    const editFacility = (facility) => {
        setEditingFacility(facility);
        setFacilityForm({
            featureName: facility.featureName || "",
            featureDescription: facility.featureDescription || "",
            featureStatus: facility.featureStatus || "Available"
        });
    };

    const removeFacility = async (facility) => {
        if (!facilityVenue) return;
        if (!window.confirm(`Remove "${facility.featureName}" from this venue?`)) return;

        try {
            await deleteVenueFeature(facilityVenue.venueId, facility.venueFeatureId);
            setFacilities((current) =>
                current.filter((item) => item.venueFeatureId !== facility.venueFeatureId)
            );
            if (editingFacility?.venueFeatureId === facility.venueFeatureId) {
                resetFacilityForm();
            }
            toast.success("Facility removed.");
        } catch (error) {
            toast.error(errorMessage(error, "Could not remove facility."));
        }
    };

    return (
        <DashboardShell
            title={`Welcome, ${firstName}`}
            eyebrow="DIBA BOOKINGS / STAFF DESK"
            subtitle="Review venue requests, maintain venue information, and keep bookings moving."
            onLogout={clearSession}
        >
            {loading ? (
                <LoadingIndicator />
            ) : (
                <>
                    <nav className="d-flex flex-wrap gap-2 mb-4" aria-label="Staff workspace sections">
                        <Button
                            variant={activeScreen === "overview" ? "primary" : "outline-primary"}
                            onClick={() => setActiveScreen("overview")}
                        >
                            Overview
                        </Button>
                        <Button
                            variant={activeScreen === "bookings" ? "primary" : "outline-primary"}
                            onClick={() => setActiveScreen("bookings")}
                        >
                            Booking requests
                        </Button>
                        <Button
                            variant={activeScreen === "venues" ? "primary" : "outline-primary"}
                            onClick={() => setActiveScreen("venues")}
                        >
                            Venue management
                        </Button>
                    </nav>

                    {activeScreen === "overview" && (
                        <>
                            <section className="stat-grid">
                                <StatCard
                                    label="Pending review"
                                    value={bookings.filter((booking) => booking.statusName === "Pending").length}
                                    tone="amber"
                                />
                                <StatCard
                                    label="Approved"
                                    value={bookings.filter((booking) => booking.statusName === "Approved").length}
                                    tone="green"
                                />
                                <StatCard label="Venues" value={venues.length} />
                                <StatCard
                                    label="Available venues"
                                    value={venues.filter((venue) => venue.venueStatus === "Available").length}
                                    tone="ink"
                                />
                            </section>
                            <div className="panel mt-4">
                                <p className="eyebrow">STAFF WORKSPACE</p>
                                <h2>What would you like to do?</h2>
                                <p>Choose one area at a time to keep your workspace focused and easy to use.</p>
                                <div className="d-flex flex-wrap gap-2">
                                    <Button onClick={() => setActiveScreen("bookings")}>Review booking requests</Button>
                                    <Button variant="outline-primary" onClick={() => setActiveScreen("venues")}>Manage venues and facilities</Button>
                                </div>
                            </div>
                        </>
                    )}

                    {activeScreen === "bookings" && (
                        <section className="content-grid">
                        <div className="panel table-panel">
                            <div className="panel-heading">
                                <div>
                                    <p className="eyebrow">BOOKING QUEUE</p>
                                    <h2>Requests requiring attention</h2>
                                </div>
                            </div>
                            <div className="row g-2 mb-3">
                                <div className="col-md-8">
                                    <Form.Control
                                        aria-label="Search bookings"
                                        placeholder="Search event, organiser, venue or booking ID"
                                        value={bookingSearch}
                                        onChange={(event) => setBookingSearch(event.target.value)}
                                    />
                                </div>
                                <div className="col-md-4">
                                    <Form.Select
                                        aria-label="Filter bookings by status"
                                        value={bookingStatusFilter}
                                        onChange={(event) => setBookingStatusFilter(event.target.value)}
                                    >
                                        {["All", "Pending", "Approved", "Rejected", "Cancelled", "Completed"].map((status) => (
                                            <option key={status} value={status}>{status === "All" ? "All booking statuses" : status}</option>
                                        ))}
                                    </Form.Select>
                                </div>
                            </div>
                            <Table responsive hover>
                                <thead>
                                    <tr>
                                        <th>Event</th>
                                        <th>Organiser</th>
                                        <th>Venue</th>
                                        <th>Status</th>
                                        <th>Actions</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {filteredBookings.map((booking) => (
                                        <tr key={booking.bookingId}>
                                            <td>{booking.eventName || shortId(booking.eventId)}</td>
                                            <td>{booking.organiserName || shortId(booking.userId)}</td>
                                            <td>{booking.venueName || shortId(booking.venueId)}</td>
                                            <td>
                                                <Badge
                                                    bg={booking.statusName === "Pending" ? "warning" : "light"}
                                                    text="dark"
                                                >
                                                    {booking.statusName || "Unknown"}
                                                </Badge>
                                            </td>
                                            <td className="text-nowrap">
                                                <Button size="sm" variant="outline-primary" className="me-1 mb-1" onClick={() => openBookingDetails(booking)}>
                                                    View details
                                                </Button>
                                                {booking.statusName === "Pending" && (
                                                    <>
                                                        <Button size="sm" variant="success" className="me-1 mb-1" onClick={() => reviewBooking(booking, "approve")}>
                                                            Approve
                                                        </Button>
                                                        <Button size="sm" variant="outline-danger" className="me-1 mb-1" onClick={() => reviewBooking(booking, "reject")}>
                                                            Reject
                                                        </Button>
                                                    </>
                                                )}
                                                {["Pending", "Approved"].includes(booking.statusName) && (
                                                    <Button size="sm" variant="danger" className="mb-1" onClick={() => cancelBookingAsStaff(booking)}>
                                                        Cancel
                                                    </Button>
                                                )}
                                            </td>
                                        </tr>
                                    ))}
                                    {filteredBookings.length === 0 && (
                                        <tr><td colSpan={5}>No bookings match the current search or status filter.</td></tr>
                                    )}
                                </tbody>
                            </Table>
                        </div>
                        </section>
                    )}

                    {activeScreen === "venues" && (
                        <section className="content-grid">
                        <div className="panel">
                            <div className="panel-heading">
                                <div>
                                    <p className="eyebrow">VENUE MANAGEMENT</p>
                                    <h2>Venues</h2>
                                </div>
                                <Button variant="primary" onClick={openCreateVenue}>Add venue</Button>
                            </div>
                            {venues.length === 0 ? (
                                <p>No venues have been added yet. Select “Add venue” to create the first one.</p>
                            ) : venues.map((venue) => (
                                <div className="compact-row" key={venue.venueId}>
                                    <div style={{ width: "160px", flexShrink: 0 }}>
                                        {venue.venueImageData ? (
                                            <button
                                                type="button"
                                                onClick={() => setSelectedVenueImage(venue)}
                                                title={`View photo of ${venue.venueName}`}
                                                style={{ display: "block", padding: 0, border: 0, background: "transparent", cursor: "zoom-in" }}
                                            >
                                                <img
                                                    src={venue.venueImageData}
                                                    alt={`Photo of ${venue.venueName}`}
                                                    style={{ width: "160px", height: "105px", objectFit: "cover", borderRadius: "8px" }}
                                                />
                                                <span className="small text-primary">View full image</span>
                                            </button>
                                        ) : (
                                            <div
                                                className="d-flex align-items-center justify-content-center bg-light text-muted rounded"
                                                style={{ width: "160px", height: "105px", fontSize: "0.85rem", textAlign: "center", padding: "8px" }}
                                            >
                                                No venue image uploaded
                                            </div>
                                        )}
                                    </div>
                                    <div className="flex-grow-1">
                                        <strong>{venue.venueName}</strong>
                                        <p>{venue.location} / capacity {venue.capacity}</p>
                                        <p>Price: {Number(venue.price || 0).toLocaleString(undefined, { style: "currency", currency: "ZAR" })}</p>
                                        <div className="d-flex flex-wrap gap-2 mt-2">
                                            <Button size="sm" variant="outline-primary" onClick={() => openEditVenue(venue)}>
                                                Edit venue
                                            </Button>
                                            <Button size="sm" variant="outline-secondary" onClick={() => openFacilities(venue)}>
                                                Manage facilities
                                            </Button>
                                        </div>
                                    </div>
                                    <Badge bg={venue.venueStatus === "Available" ? "success" : "secondary"}>
                                        {venue.venueStatus || "Unknown"}
                                    </Badge>
                                </div>
                            ))}
                        </div>
                        </section>
                    )}

                </>
            )}

            <Modal show={bookingDetailsOpen} onHide={() => setBookingDetailsOpen(false)} size="lg" centered scrollable>
                <Modal.Header closeButton>
                    <Modal.Title>Booking details</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    {selectedBooking && (() => {
                        const bookingPayment = payments
                            .filter((payment) => payment.bookingId === selectedBooking.bookingId)
                            .sort((a, b) => new Date(b.paymentDate || 0) - new Date(a.paymentDate || 0))[0];
                        return (
                            <>
                                <div className="d-flex justify-content-between align-items-start gap-3 mb-3">
                                    <div>
                                        <h5 className="mb-1">{selectedBooking.eventName || "Unnamed event"}</h5>
                                        <p className="text-muted mb-0">Booking ID: {selectedBooking.bookingId}</p>
                                    </div>
                                    <Badge bg={selectedBooking.statusName === "Approved" ? "success" : selectedBooking.statusName === "Pending" ? "warning" : "secondary"} text={selectedBooking.statusName === "Pending" ? "dark" : undefined}>
                                        {selectedBooking.statusName || "Unknown"}
                                    </Badge>
                                </div>
                                <h6>Event and venue</h6>
                                <Table bordered size="sm">
                                    <tbody>
                                        <tr><th>Venue</th><td>{selectedBooking.venueName || "Not recorded"}</td></tr>
                                        <tr><th>Start</th><td>{formatDateTime(selectedBooking.startDateTime)}</td></tr>
                                        <tr><th>End</th><td>{formatDateTime(selectedBooking.endDateTime)}</td></tr>
                                        <tr><th>Requested on</th><td>{formatDateTime(selectedBooking.bookingDate)}</td></tr>
                                        <tr><th>Organiser</th><td>{selectedBooking.organiserName || "Not recorded"}</td></tr>
                                        <tr><th>Organiser user ID</th><td>{selectedBooking.userId || "Not recorded"}</td></tr>
                                        <tr><th>Special requirements</th><td style={{ whiteSpace: "pre-wrap" }}>{selectedBooking.specialRequirements || "None provided"}</td></tr>
                                        <tr><th>Staff/admin notes</th><td style={{ whiteSpace: "pre-wrap" }}>{selectedBooking.adminNotes || "No notes recorded"}</td></tr>
                                        <tr><th>Responsibility acknowledgement</th><td>{selectedBooking.acknowledgementAccepted ? `Accepted (${formatDateTime(selectedBooking.acknowledgementAcceptedAt)})` : "Not recorded"}</td></tr>
                                    </tbody>
                                </Table>
                                <h6 className="mt-4">Payment and refund</h6>
                                {!bookingPayment ? (
                                    <p className="text-muted">No payment record is linked to this booking.</p>
                                ) : (
                                    <>
                                        <Table bordered size="sm">
                                            <tbody>
                                                <tr><th>Payment status</th><td>{bookingPayment.paymentStatus || "Unknown"}</td></tr>
                                                <tr><th>Amount paid</th><td>R{Number(bookingPayment.amount || 0).toFixed(2)}</td></tr>
                                                <tr><th>Payment date</th><td>{formatDateTime(bookingPayment.paymentDate)}</td></tr>
                                                <tr><th>Reference</th><td>{bookingPayment.referenceNumber || "Not recorded"}</td></tr>
                                                <tr><th>Refund amount</th><td>{bookingPayment.refundAmount == null ? "Not applicable" : `R${Number(bookingPayment.refundAmount).toFixed(2)}`}</td></tr>
                                                <tr><th>Refund status</th><td>{bookingPayment.refundStatus || "Not requested"}</td></tr>
                                                <tr><th>Refund reason</th><td>{bookingPayment.refundReason || "Not recorded"}</td></tr>
                                                <tr><th>Refund requested</th><td>{formatDateTime(bookingPayment.refundRequestedAtUtc)}</td></tr>
                                                <tr><th>Refund processed</th><td>{formatDateTime(bookingPayment.refundProcessedAtUtc)}</td></tr>
                                                {bookingPayment.refundFailureReason && <tr><th>Staff follow-up</th><td>{bookingPayment.refundFailureReason}</td></tr>}
                                            </tbody>
                                        </Table>
                                        {bookingPayment.refundStatus === "Failed" && (
                                            <Button variant="warning" disabled={retryingPaymentId === bookingPayment.paymentId} onClick={() => retryRefund(bookingPayment)}>
                                                {retryingPaymentId === bookingPayment.paymentId ? "Submitting retry…" : "Retry failed refund"}
                                            </Button>
                                        )}
                                        {bookingPayment.refundStatus === "NeedsReview" && (
                                            <p className="alert alert-warning mb-0">This refund must be reconciled with Yoco before another attempt. Do not retry it automatically.</p>
                                        )}
                                    </>
                                )}
                                <p className="small text-muted mt-3 mb-0">Staff actions are recorded by the backend. Cancellation is final and DIBA-initiated cancellations receive a full refund when a successful payment exists.</p>
                            </>
                        );
                    })()}
                </Modal.Body>
                <Modal.Footer>
                    {selectedBooking && selectedBooking.statusName === "Pending" && (
                        <>
                            <Button variant="success" onClick={() => { setBookingDetailsOpen(false); reviewBooking(selectedBooking, "approve"); }}>Approve booking</Button>
                            <Button variant="outline-danger" onClick={() => { setBookingDetailsOpen(false); reviewBooking(selectedBooking, "reject"); }}>Reject booking</Button>
                        </>
                    )}
                    {selectedBooking && ["Pending", "Approved"].includes(selectedBooking.statusName) && (
                        <Button variant="danger" onClick={() => cancelBookingAsStaff(selectedBooking)}>Cancel booking</Button>
                    )}
                    <Button variant="secondary" onClick={() => setBookingDetailsOpen(false)}>Close</Button>
                </Modal.Footer>
            </Modal>

            <Modal show={venueModalOpen} onHide={closeVenueModal} size="lg" centered>
                <Form onSubmit={saveVenue}>
                    <Modal.Header closeButton>
                        <Modal.Title>{editingVenue ? "Edit venue" : "Add venue"}</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <p className="text-muted">Fields marked required must be completed. Coordinates must be valid latitude and longitude values.</p>
                        <Form.Group className="mb-3" controlId="venueName">
                            <Form.Label>Venue name *</Form.Label>
                            <Form.Control required maxLength={150} value={venueForm.venueName}
                                onChange={(event) => setVenueForm({ ...venueForm, venueName: event.target.value })} />
                        </Form.Group>
                        <Form.Group className="mb-3" controlId="venueImage">
                            <Form.Label>Venue photo</Form.Label>
                            <Form.Control
                                type="file"
                                accept="image/jpeg,image/png,image/webp"
                                onChange={(event) => {
                                    const file = event.target.files?.[0];
                                    if (!file) return;
                                    if (file.size > 2 * 1024 * 1024) {
                                        toast.error("Choose an image smaller than 2 MB.");
                                        event.target.value = "";
                                        return;
                                    }
                                    const reader = new FileReader();
                                    reader.onload = () => setVenueForm((current) => ({
                                        ...current,
                                        venueImageData: typeof reader.result === "string" ? reader.result : current.venueImageData
                                    }));
                                    reader.onerror = () => toast.error("Could not read the selected image.");
                                    reader.readAsDataURL(file);
                                }}
                            />
                            <Form.Text muted>JPG, PNG or WebP. Maximum size: 2 MB. Leave empty to keep the current photo when editing.</Form.Text>
                            {venueForm.venueImageData && (
                                <div className="mt-3">
                                    <img src={venueForm.venueImageData} alt="Venue preview"
                                        style={{ width: "100%", maxWidth: "420px", maxHeight: "220px", objectFit: "cover", borderRadius: "8px" }} />
                                    <div className="mt-2">
                                        <Button type="button" size="sm" variant="outline-danger"
                                            onClick={() => setVenueForm((current) => ({ ...current, venueImageData: "" }))}>
                                            Remove photo
                                        </Button>
                                    </div>
                                </div>
                            )}
                        </Form.Group>
                        <Form.Group className="mb-3" controlId="venueDescription">
                            <Form.Label>Description</Form.Label>
                            <Form.Control as="textarea" rows={3} maxLength={2000} value={venueForm.venueDescription}
                                onChange={(event) => setVenueForm({ ...venueForm, venueDescription: event.target.value })} />
                        </Form.Group>
                        <div className="row">
                            <Form.Group className="col-md-6 mb-3" controlId="venueCapacity">
                                <Form.Label>Capacity *</Form.Label>
                                <Form.Control type="number" required min="1" step="1" value={venueForm.capacity}
                                    onChange={(event) => setVenueForm({ ...venueForm, capacity: event.target.value })} />
                            </Form.Group>
                            <Form.Group className="col-md-6 mb-3" controlId="venuePrice">
                                <Form.Label>Price (ZAR) *</Form.Label>
                                <Form.Control type="number" required min="0" step="0.01" value={venueForm.price}
                                    onChange={(event) => setVenueForm({ ...venueForm, price: event.target.value })} />
                            </Form.Group>
                        </div>
                        <Form.Group className="mb-3" controlId="venueLocation">
                            <Form.Label>Location *</Form.Label>
                            <Form.Control required maxLength={250} value={venueForm.location}
                                onChange={(event) => setVenueForm({ ...venueForm, location: event.target.value })} />
                        </Form.Group>
                        <div className="row">
                            <Form.Group className="col-md-4 mb-3" controlId="venueLatitude">
                                <Form.Label>Latitude *</Form.Label>
                                <Form.Control type="number" required min="-90" max="90" step="any" value={venueForm.latitude}
                                    onChange={(event) => setVenueForm({ ...venueForm, latitude: event.target.value })} />
                            </Form.Group>
                            <Form.Group className="col-md-4 mb-3" controlId="venueLongitude">
                                <Form.Label>Longitude *</Form.Label>
                                <Form.Control type="number" required min="-180" max="180" step="any" value={venueForm.longitude}
                                    onChange={(event) => setVenueForm({ ...venueForm, longitude: event.target.value })} />
                            </Form.Group>
                            <Form.Group className="col-md-4 mb-3" controlId="venueStatus">
                                <Form.Label>Operational status *</Form.Label>
                                <Form.Select value={venueForm.venueStatus}
                                    onChange={(event) => setVenueForm({ ...venueForm, venueStatus: event.target.value })}>
                                    <option value="Available">Available</option>
                                    <option value="Unavailable">Unavailable</option>
                                    <option value="Maintenance">Maintenance</option>
                                </Form.Select>
                            </Form.Group>
                        </div>
                    </Modal.Body>
                    <Modal.Footer>
                        <Button variant="secondary" type="button" onClick={closeVenueModal} disabled={savingVenue}>Cancel</Button>
                        <Button variant="primary" type="submit" disabled={savingVenue}>
                            {savingVenue ? "Saving..." : editingVenue ? "Save changes" : "Add venue"}
                        </Button>
                    </Modal.Footer>
                </Form>
            </Modal>

            <Modal show={facilityModalOpen} onHide={() => !savingFacility && setFacilityModalOpen(false)} size="lg" centered>
                <Modal.Header closeButton>
                    <Modal.Title>Facilities — {facilityVenue?.venueName}</Modal.Title>
                </Modal.Header>
                <Modal.Body>
                    <Form onSubmit={saveFacility} className="mb-4">
                        <h5>{editingFacility ? "Edit facility" : "Add a facility"}</h5>
                        <Form.Group className="mb-3" controlId="facilityName">
                            <Form.Label>Facility name *</Form.Label>
                            <Form.Control required maxLength={150} value={facilityForm.featureName}
                                onChange={(event) => setFacilityForm({ ...facilityForm, featureName: event.target.value })} />
                        </Form.Group>
                        <Form.Group className="mb-3" controlId="facilityDescription">
                            <Form.Label>Description</Form.Label>
                            <Form.Control as="textarea" rows={2} maxLength={1000} value={facilityForm.featureDescription}
                                onChange={(event) => setFacilityForm({ ...facilityForm, featureDescription: event.target.value })} />
                        </Form.Group>
                        <Form.Group className="mb-3" controlId="facilityStatus">
                            <Form.Label>Status *</Form.Label>
                            <Form.Select value={facilityForm.featureStatus}
                                onChange={(event) => setFacilityForm({ ...facilityForm, featureStatus: event.target.value })}>
                                <option value="Available">Available</option>
                                <option value="Unavailable">Unavailable</option>
                            </Form.Select>
                        </Form.Group>
                        <div className="d-flex gap-2">
                            <Button type="submit" disabled={savingFacility}>
                                {savingFacility ? "Saving..." : editingFacility ? "Save facility" : "Add facility"}
                            </Button>
                            {editingFacility && <Button type="button" variant="outline-secondary" onClick={resetFacilityForm}>Cancel edit</Button>}
                        </div>
                    </Form>
                    <hr />
                    <h5>Current facilities</h5>
                    {facilitiesLoading ? <p>Loading facilities...</p> : facilities.length === 0 ? (
                        <p>No facilities are recorded for this venue yet.</p>
                    ) : (
                        <Table responsive hover>
                            <thead><tr><th>Facility</th><th>Status</th><th>Actions</th></tr></thead>
                            <tbody>
                                {facilities.map((facility) => (
                                    <tr key={facility.venueFeatureId}>
                                        <td>
                                            <strong>{facility.featureName}</strong>
                                            {facility.featureDescription && <div className="text-muted small">{facility.featureDescription}</div>}
                                        </td>
                                        <td><Badge bg={facility.featureStatus === "Available" ? "success" : "secondary"}>{facility.featureStatus}</Badge></td>
                                        <td className="text-nowrap">
                                            <Button size="sm" variant="outline-primary" className="me-2" onClick={() => editFacility(facility)}>Edit</Button>
                                            <Button size="sm" variant="outline-danger" onClick={() => removeFacility(facility)}>Remove</Button>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </Table>
                    )}
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" onClick={() => setFacilityModalOpen(false)}>Close</Button>
                </Modal.Footer>
            </Modal>

            <Modal
                show={Boolean(selectedVenueImage)}
                onHide={() => setSelectedVenueImage(null)}
                centered
                size="lg"
            >
                <Modal.Header closeButton>
                    <Modal.Title>{selectedVenueImage?.venueName || "Venue image"}</Modal.Title>
                </Modal.Header>
                <Modal.Body className="text-center">
                    {selectedVenueImage?.venueImageData && (
                        <img
                            src={selectedVenueImage.venueImageData}
                            alt={`Full image of ${selectedVenueImage.venueName}`}
                            style={{ maxWidth: "100%", maxHeight: "70vh", objectFit: "contain", borderRadius: "8px" }}
                        />
                    )}
                </Modal.Body>
            </Modal>
        </DashboardShell>
    );
}

export default StaffDashboard;
