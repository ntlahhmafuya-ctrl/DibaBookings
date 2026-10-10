import { Badge, Button, Form, Table } from "react-bootstrap";
import { formatDate } from "../../utils/dateUtils";
import { shortId } from "../../utils/dashboardUtils";

const sectionTitles = {
    users: ["USER MANAGEMENT", "Users"],
    bookings: ["BOOKING OVERSIGHT", "All bookings"],
    audit: ["ACCOUNTABILITY", "Audit logs"],
    privacy: ["DATA RIGHTS", "Privacy requests"]
};

/**
 * ADMINISTRATOR RECORDS PANEL
 * Responsibility: display searchable user and booking records, audit logs, and the privacy-request review queue.
 */
function AdminRecordsPanel({
    activeSection,
    query,
    onQueryChange,
    users,
    bookings,
    logs,
    privacyRequests = [],
    onUpdatePrivacyRequest,
    onCreateUser,
    onChangeRole,
    onUpdateUser
}) {
    const [eyebrow, title] = sectionTitles[activeSection];

    return (
        <section className="panel table-panel">
            <div className="panel-heading">
                <div>
                    <p className="eyebrow">{eyebrow}</p>
                    <h2>{title}</h2>
                </div>
                {activeSection === "users" && (
                    <Button onClick={onCreateUser}>+ Create user</Button>
                )}
            </div>

            {activeSection !== "audit" && activeSection !== "privacy" && (
                <Form.Control
                    className="search"
                    placeholder={`Search ${activeSection}...`}
                    value={query}
                    onChange={(event) => onQueryChange(event.target.value)}
                />
            )}

            {activeSection === "users" && (
                <Table responsive hover>
                    <thead>
                        <tr>
                            <th>Name</th>
                            <th>Email</th>
                            <th>Role</th>
                            <th>Status</th>
                            <th>Actions</th>
                        </tr>
                    </thead>
                    <tbody>
                        {users.map((user) => (
                            <tr key={user.userId}>
                                <td>
                                    {user.firstName} {user.lastName}
                                </td>
                                <td>{user.email}</td>
                                <td>{user.role}</td>
                                <td>
                                    <Badge
                                        bg={
                                            user.isActive
                                                ? "success"
                                                : "secondary"
                                        }
                                    >
                                        {user.isActive ? "Active" : "Inactive"}
                                    </Badge>
                                </td>
                                <td>
                                    <button
                                        className="table-action"
                                        onClick={() => onChangeRole(user)}
                                    >
                                        Change role
                                    </button>
                                    <button
                                        className="table-action danger"
                                        onClick={() => onUpdateUser(user)}
                                    >
                                        {user.isActive
                                            ? "Deactivate"
                                            : "Activate"}
                                    </button>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </Table>
            )}

            {activeSection === "bookings" && (
                <Table responsive hover>
                    <thead>
                        <tr>
                            <th>Booking</th>
                            <th>Organiser</th>
                            <th>Event</th>
                            <th>Venue</th>
                            <th>Submitted</th>
                            <th>Status</th>
                        </tr>
                    </thead>
                    <tbody>
                        {bookings.map((booking) => (
                            <tr key={booking.bookingId}>
                                <td>{shortId(booking.bookingId)}</td>
                                <td>
                                    {booking.organiserName ||
                                        shortId(booking.userId)}
                                </td>
                                <td>
                                    {booking.eventName ||
                                        shortId(booking.eventId)}
                                </td>
                                <td>
                                    {booking.venueName ||
                                        shortId(booking.venueId)}
                                </td>
                                <td>{formatDate(booking.bookingDate)}</td>
                                <td>
                                    <Badge bg="light" text="dark">
                                        {booking.statusName ||
                                            shortId(booking.bookingStatusId)}
                                    </Badge>
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </Table>
            )}


            {activeSection === "privacy" && (
                <Table responsive hover>
                    <thead>
                        <tr>
                            <th>Requester</th>
                            <th>Request</th>
                            <th>Details</th>
                            <th>Status</th>
                            <th>Submitted</th>
                            <th>Response</th>
                            <th>Action</th>
                        </tr>
                    </thead>
                    <tbody>
                        {privacyRequests.map((request) => (
                            <tr key={request.privacyRequestId}>
                                <td>
                                    <strong>{request.requesterName}</strong>
                                    <br />
                                    <small>{request.requesterEmail}</small>
                                </td>
                                <td>{request.requestType}</td>
                                <td>{request.description}</td>
                                <td>
                                    <Badge bg={request.status === "Resolved" ? "success" : request.status === "Rejected" ? "danger" : "secondary"}>
                                        {request.status}
                                    </Badge>
                                </td>
                                <td>{formatDate(request.submittedAtUtc)}</td>
                                <td>{request.response || "—"}</td>
                                <td>
                                    <button
                                        className="table-action"
                                        onClick={() => onUpdatePrivacyRequest(request)}
                                    >
                                        Review
                                    </button>
                                </td>
                            </tr>
                        ))}
                        {privacyRequests.length === 0 && (
                            <tr><td colSpan={7}>No privacy requests have been submitted.</td></tr>
                        )}
                    </tbody>
                </Table>
            )}

            {activeSection === "audit" && (
                <Table responsive hover>
                    <thead>
                        <tr>
                            <th>Action</th>
                            <th>Details</th>
                            <th>Performed by</th>
                            <th>Date / time</th>
                        </tr>
                    </thead>
                    <tbody>
                        {logs.map((log) => (
                            <tr key={log.auditLogId}>
                                <td>
                                    <strong>{log.action}</strong>
                                </td>
                                <td>{log.logDescription || "-"}</td>
                                <td>
                                    {log.userName || shortId(log.userId)}
                                </td>
                                <td>{formatDate(log.timestamp)}</td>
                            </tr>
                        ))}
                    </tbody>
                </Table>
            )}
        </section>
    );
}

export default AdminRecordsPanel;
