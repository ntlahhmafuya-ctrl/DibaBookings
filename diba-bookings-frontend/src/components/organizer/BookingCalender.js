import { useEffect, useState } from "react";
import api from "../../services/api";

function BookingCalendar({
    selectedDate,
    onDateSelect,
    venueId,
    onTimeSelect
}) {
    const [currentMonth, setCurrentMonth] = useState(
        selectedDate || new Date()
    );

    const [bookings, setBookings] = useState([]);
    const [loading, setLoading] = useState(false);
    const [availabilityError, setAvailabilityError] = useState("");
    const [selectedSlot, setSelectedSlot] = useState(null);

    const year = currentMonth.getFullYear();
    const month = currentMonth.getMonth();

    const firstDay = new Date(year, month, 1).getDay();
    const daysInMonth = new Date(year, month + 1, 0).getDate();

    const days = [];

    for (let i = 0; i < firstDay; i++) {
        days.push(null);
    }

    for (let day = 1; day <= daysInMonth; day++) {
        days.push(day);
    }

    const monthName = currentMonth.toLocaleDateString([], {
        month: "long",
        year: "numeric"
    });

    const formatDateForApi = (date) => {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, "0");
        const day = String(date.getDate()).padStart(2, "0");

        return `${year}-${month}-${day}`;
    };

    const loadAvailability = async (date) => {
        if (!venueId || !date) {
            setBookings([]);
            return;
        }

        setLoading(true);
        setAvailabilityError("");
        setSelectedSlot(null);

        try {
            const response = await api.get(
                `/Bookings/venue/${venueId}/availability`,
                {
                    params: {
                        date: formatDateForApi(date)
                    }
                }
            );

            setBookings(response.data || []);
        } catch (error) {
            console.error(
                "Unable to load venue availability:",
                error
            );

            setBookings([]);

            setAvailabilityError(
                error.response?.data ||
                "Unable to load venue availability."
            );
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        if (selectedDate && venueId) {
            loadAvailability(selectedDate);
        }
    }, [selectedDate, venueId]);

    const goToPreviousMonth = () => {
        setCurrentMonth(new Date(year, month - 1, 1));
    };

    const goToNextMonth = () => {
        setCurrentMonth(new Date(year, month + 1, 1));
    };

    const handleDateClick = (day) => {
        if (!day) return;

        const date = new Date(year, month, day);

        onDateSelect(date);
    };

    const isSelected = (day) => {
        if (!selectedDate || !day) return false;

        return (
            selectedDate.getFullYear() === year &&
            selectedDate.getMonth() === month &&
            selectedDate.getDate() === day
        );
    };

    const formatTime = (value) => {
        return new Date(value).toLocaleTimeString([], {
            hour: "2-digit",
            minute: "2-digit"
        });
    };

    const isSlotBooked = (slotStart, slotEnd) => {
        return bookings.some((booking) => {
            const bookingStart = new Date(
                booking.startDateTime
            );

            const bookingEnd = new Date(
                booking.endDateTime
            );

            return (
                slotStart < bookingEnd &&
                slotEnd > bookingStart
            );
        });
    };

    const getSlotStatus = (slotStart, slotEnd) => {
        const booking = bookings.find((item) => {
            const bookingStart = new Date(
                item.startDateTime
            );

            const bookingEnd = new Date(
                item.endDateTime
            );

            return (
                slotStart < bookingEnd &&
                slotEnd > bookingStart
            );
        });

        return booking?.statusName || null;
    };

    const createTimeSlots = () => {
        if (!selectedDate) return [];

        const slots = [];

        for (let hour = 8; hour < 18; hour++) {
            const start = new Date(selectedDate);
            start.setHours(hour, 0, 0, 0);

            const end = new Date(selectedDate);
            end.setHours(hour + 1, 0, 0, 0);

            slots.push({
                start,
                end
            });
        }

        return slots;
    };

    const handleTimeSelect = (slot) => {
        const status = getSlotStatus(
            slot.start,
            slot.end
        );

        if (status) {
            return;
        }

        setSelectedSlot(slot);

        if (onTimeSelect) {
            onTimeSelect(slot);
        }
    };

    const selectedDateText = selectedDate
        ? selectedDate.toLocaleDateString([], {
              weekday: "long",
              day: "numeric",
              month: "long",
              year: "numeric"
          })
        : "";

    const timeSlots = createTimeSlots();

    return (
        <div className="booking-calendar">

            <div className="calendar-header">
                <button
                    type="button"
                    onClick={goToPreviousMonth}
                    aria-label="Previous month"
                >
                    ←
                </button>

                <h3>{monthName}</h3>

                <button
                    type="button"
                    onClick={goToNextMonth}
                    aria-label="Next month"
                >
                    →
                </button>
            </div>

            <div className="calendar-weekdays">
                {[
                    "Sun",
                    "Mon",
                    "Tue",
                    "Wed",
                    "Thu",
                    "Fri",
                    "Sat"
                ].map((day) => (
                    <span key={day}>{day}</span>
                ))}
            </div>

            <div className="calendar-grid">
                {days.map((day, index) => (
                    <button
                        key={index}
                        type="button"
                        disabled={!day}
                        className={
                            day && isSelected(day)
                                ? "calendar-day selected"
                                : "calendar-day"
                        }
                        onClick={() =>
                            handleDateClick(day)
                        }
                    >
                        {day}
                    </button>
                ))}
            </div>

            {selectedDate && (
                <div className="calendar-availability">

                    <div className="availability-heading">
                        <p className="section-kicker">
                            VENUE AVAILABILITY
                        </p>

                        <h4>{selectedDateText}</h4>

                        <p>
                            Select an available one-hour
                            time slot.
                        </p>
                    </div>

                    {loading && (
                        <p className="availability-message">
                            Checking availability...
                        </p>
                    )}

                    {!loading && availabilityError && (
                        <p className="availability-message error">
                            {availabilityError}
                        </p>
                    )}

                    {!loading &&
                        !availabilityError && (
                            <div className="time-slot-list">

                                {timeSlots.map((slot) => {
                                    const booked =
                                        isSlotBooked(
                                            slot.start,
                                            slot.end
                                        );

                                    const status =
                                        getSlotStatus(
                                            slot.start,
                                            slot.end
                                        );

                                    const selected =
                                        selectedSlot &&
                                        selectedSlot.start.getTime() ===
                                            slot.start.getTime();

                                    return (
                                        <button
                                            key={slot.start.toISOString()}
                                            type="button"
                                            disabled={booked}
                                            className={[
                                                "time-slot",
                                                booked
                                                    ? "booked"
                                                    : "available",
                                                selected
                                                    ? "selected"
                                                    : ""
                                            ]
                                                .filter(Boolean)
                                                .join(" ")}
                                            onClick={() =>
                                                handleTimeSelect(
                                                    slot
                                                )
                                            }
                                        >
                                            <span className="time-slot-time">
                                                {formatTime(
                                                    slot.start
                                                )}{" "}
                                                –{" "}
                                                {formatTime(
                                                    slot.end
                                                )}
                                            </span>

                                            <span className="time-slot-status">
                                                {status ||
                                                    (selected
                                                        ? "Selected"
                                                        : "Available")}
                                            </span>
                                        </button>
                                    );
                                })}

                            </div>
                        )}
                </div>
            )}
        </div>
    );
}

export default BookingCalendar;