import { useEffect, useState } from "react";
import { Badge, Button, Form, Modal, Table } from "react-bootstrap";
import { toast } from "react-toastify";

import DashboardShell from "../../components/common/DashboardShell";
import LoadingIndicator from "../../components/common/LoadingIndicator";
import StatCard from "../../components/common/StatCard";
import {
    approveBooking,
    getBookings,
    rejectBooking
} from "../../services/bookingService";
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
    const [venues, setVenues] = useState([]);
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

                    <section className="content-grid">
                        <div className="panel table-panel">
                            <div className="panel-heading">
                                <div>
                                    <p className="eyebrow">BOOKING QUEUE</p>
                                    <h2>Requests requiring attention</h2>
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
                                    {bookings.map((booking) => (
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
                                            <td>
                                                {booking.statusName === "Pending" && (
                                                    <>
                                                        <button
                                                            type="button"
                                                            className="table-action"
                                                            onClick={() => reviewBooking(booking, "approve")}
                                                        >
                                                            Approve
                                                        </button>
                                                        <button
                                                            type="button"
                                                            className="table-action danger"
                                                            onClick={() => reviewBooking(booking, "reject")}
                                                        >
                                                            Reject
                                                        </button>
                                                    </>
                                                )}
                                            </td>
                                        </tr>
                                    ))}
                                    {bookings.length === 0 && (
                                        <tr><td colSpan={5}>No booking requests found.</td></tr>
                                    )}
                                </tbody>
                            </Table>
                        </div>

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
                </>
            )}

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
        </DashboardShell>
    );
}

export default StaffDashboard;
