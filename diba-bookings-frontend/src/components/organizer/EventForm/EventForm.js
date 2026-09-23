import { useState } from "react";
import FormIntro from "./FormIntro";
import Field from "./Field";
import FormActions from "./FormActions";
import BookingCalendar from "../BookingCalender";

function EventForm({
    form,
    setForm,
    selectedVenue,
    venues,
    saving,
    editing,
    onSubmit,
    onChangeVenue,
    onCancel
}) {
    const [selectedDate, setSelectedDate] = useState(
        form.startDateTime
            ? new Date(form.startDateTime)
            : null
    );

    const handleDateSelect = (date) => {
        setSelectedDate(date);

        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");

        const startTime = form.startDateTime
            ? new Date(form.startDateTime).toTimeString().slice(0, 5)
            : "09:00";

        const endTime = form.endDateTime
            ? new Date(form.endDateTime).toTimeString().slice(0, 5)
            : "10:00";

        setForm({
            ...form,
            startDateTime: `${year}-${month}-${day}T${startTime}`,
            endDateTime: `${year}-${month}-${day}T${endTime}`
        });
    };

    return (
        <section className="form-view">
            <FormIntro
                title={editing ? "Edit event" : "Create booking"}
                text="Your event details and venue request will be submitted together for staff approval."
            />

            <form
                className="workflow-form"
                onSubmit={onSubmit}
            >
                <div className="form-grid">

                    <Field label="Event name" required>
                        <input
                            required
                            value={form.eventName}
                            onChange={(e) =>
                                setForm({
                                    ...form,
                                    eventName: e.target.value
                                })
                            }
                        />
                    </Field>

                    <Field label="Event type">
                        <input
                            placeholder="Conference, meeting, workshop..."
                            value={form.eventType}
                            onChange={(e) =>
                                setForm({
                                    ...form,
                                    eventType: e.target.value
                                })
                            }
                        />
                    </Field>

                    <Field label="Venue" required>
                        {selectedVenue ? (
                            <div className="selected-venue">
                                <strong>
                                    {selectedVenue.venueName}
                                </strong>

                                <span>
                                    {selectedVenue.location} · Capacity{" "}
                                    {selectedVenue.capacity}
                                </span>

                                <button
                                    type="button"
                                    onClick={() => onChangeVenue(null)}
                                >
                                    Change Venue
                                </button>
                            </div>
                        ) : (
                            <select
                                required
                                value={form.venueId}
                                onChange={(e) => {
                                    const venue = venues.find(
                                        (item) =>
                                            item.venueId === e.target.value
                                    );

                                    setForm({
                                        ...form,
                                        venueId: e.target.value
                                    });

                                    onChangeVenue(venue);
                                }}
                            >
                                <option value="">
                                    Choose a venue
                                </option>

                                {venues.map((venue) => (
                                    <option
                                        key={venue.venueId}
                                        value={venue.venueId}
                                    >
                                        {venue.venueName}
                                    </option>
                                ))}
                            </select>
                        )}
                    </Field>

                    <Field label="Expected attendees">
                        <input
                            value={form.eventAttendance}
                            onChange={(e) =>
                                setForm({
                                    ...form,
                                    eventAttendance: e.target.value
                                })
                            }
                            placeholder="e.g. 50"
                        />
                    </Field>
                </div>

                {!editing && selectedVenue && (
                    <div className="booking-calendar-section">
                        <div className="calendar-intro">
                            <p className="section-kicker">
                                CHECK AVAILABILITY
                            </p>

                            <h3>Select your event date</h3>

                            <p>
                                Choose a date to continue with your venue
                                booking.
                            </p>
                        </div>

<BookingCalendar
    selectedDate={selectedDate}
    onDateSelect={handleDateSelect}
    venueId={form.venueId}
    onTimeSelect={(slot) => {
        const toLocalInput = (date) => {
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, "0");
            const day = String(date.getDate()).padStart(2, "0");
            const hours = String(date.getHours()).padStart(2, "0");
            const minutes = String(date.getMinutes()).padStart(2, "0");

            return `${year}-${month}-${day}T${hours}:${minutes}`;
        };

        setForm({
            ...form,
            startDateTime: toLocalInput(slot.start),
            endDateTime: toLocalInput(slot.end)
        });
    }}
/>
                    </div>
                )}

                <div className="form-grid">

                    <Field label="Start date and time" required>
                        <input
                            required
                            type="datetime-local"
                            value={form.startDateTime}
                            onChange={(e) =>
                                setForm({
                                    ...form,
                                    startDateTime: e.target.value
                                })
                            }
                        />
                    </Field>

                    <Field label="End date and time" required>
                        <input
                            required
                            type="datetime-local"
                            value={form.endDateTime}
                            onChange={(e) =>
                                setForm({
                                    ...form,
                                    endDateTime: e.target.value
                                })
                            }
                        />
                    </Field>
                </div>

                <Field label="Description" required>
                    <textarea
                        required
                        rows="4"
                        value={form.eventDescription}
                        onChange={(e) =>
                            setForm({
                                ...form,
                                eventDescription: e.target.value
                            })
                        }
                    />
                </Field>

                {!editing && (
                    <p className="form-note">
                        This submits the event and venue booking as one
                        action. Status after submission:{" "}
                        <strong>Pending approval</strong>
                    </p>
                )}

                <FormActions
                    saving={saving}
                    submit={
                        editing
                            ? "Save Changes"
                            : "Submit Event & Booking"
                    }
                    onCancel={onCancel}
                />
            </form>
        </section>
    );
}

export default EventForm;