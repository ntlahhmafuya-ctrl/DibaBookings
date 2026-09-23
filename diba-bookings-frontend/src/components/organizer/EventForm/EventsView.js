import Status from "../Status";
import Empty from "../Empty";
import { dateText } from "../../../utils/dashboardUtils";

function EventsView({ events, bookings, venues, onEdit, onBook }) { return <section className="workspace-view"><div className="workspace-toolbar"><div><p className="section-kicker">YOUR EVENTS</p><h2>My events</h2></div></div>{events.length ? <div className="records-list">{events.map((event) => { const booking = bookings.find((item) => item.eventId === event.eventId); return <article className="record-row" key={event.eventId}><div><strong>{event.eventName}</strong><p>{venues.find((venue) => venue.venueId === event.venueId)?.venueName || "Venue"} · {dateText(event.startDateTime)} · {event.eventType || "Event"}</p></div><Status status={booking?.statusName || "No booking"} /><div className="record-actions"><button onClick={() => onEdit(venues.find((venue) => venue.venueId === event.venueId), event)}>Edit</button>{!booking && <button onClick={() => onBook(event)}>Book venue</button>}</div></article>; })}</div> : <Empty title="No events yet" text="Create a booking to start planning your venue request." />}</section>; }
export default EventsView;
