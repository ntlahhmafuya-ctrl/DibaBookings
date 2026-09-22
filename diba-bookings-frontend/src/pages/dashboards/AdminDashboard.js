import { useEffect, useMemo, useState } from "react";
import { Badge, Button, Form, Modal, Spinner, Table } from "react-bootstrap";
import { toast } from "react-toastify";
import api from "../../services/api";
import { Stat } from "./DashboardShared";
import { clearSession, emptyOverview, formatDate, shortId } from "../../utils/dashboardUtils";

function AdminDashboard() {
    const [overview, setOverview] = useState(emptyOverview);
    const [users, setUsers] = useState([]);
    const [bookings, setBookings] = useState([]);
    const [logs, setLogs] = useState([]);
    const [roles, setRoles] = useState([]);
    const [query, setQuery] = useState("");
    const [tab, setTab] = useState("overview");
    const [loading, setLoading] = useState(true);
    const [showCreate, setShowCreate] = useState(false);
    const [newUser, setNewUser] = useState({ firstName: "", lastName: "", email: "", password: "", roleId: "" });
    const firstName = localStorage.getItem("firstName") || "Administrator";

    const loadData = async () => {
        setLoading(true);
        try {
            const [summary, userResponse, bookingResponse, logResponse, roleResponse] = await Promise.all([
                api.get("/Administration/overview"), api.get("/Users"), api.get("/Bookings"),
                api.get("/AuditLogs"), api.get("/Administration/roles"),
            ]);
            setOverview(summary.data);
            setUsers(userResponse.data);
            setBookings(bookingResponse.data);
            setLogs(logResponse.data);
            setRoles(roleResponse.data);
            setNewUser((current) => ({ ...current, roleId: current.roleId || roleResponse.data[0]?.roleId || "" }));
        } catch (error) {
            toast.error(error.response?.data || "Could not load administrator data.");
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { loadData(); }, []);

    const filteredUsers = useMemo(() => users.filter((user) =>
        `${user.firstName} ${user.lastName} ${user.email} ${user.role}`.toLowerCase().includes(query.toLowerCase())), [users, query]);
    const filteredBookings = useMemo(() => bookings.filter((booking) =>
        `${booking.bookingId} ${booking.organiserName} ${booking.eventName} ${booking.venueName}`.toLowerCase().includes(query.toLowerCase())), [bookings, query]);

    const updateUser = async (user) => {
        try {
            await api.put(`/Administration/users/${user.userId}/status?active=${!user.isActive}`);
            toast.success(`User ${user.isActive ? "deactivated" : "activated"}.`);
            loadData();
        } catch (error) { toast.error(error.response?.data || "Could not update user status."); }
    };

    const changeRole = async (user) => {
        const roleName = window.prompt(`Enter the role name for ${user.firstName} (Administrator, Staff, or Event Organiser):`);
        const selected = roles.find((item) => item.roleName.toLowerCase() === roleName?.toLowerCase());
        if (!selected) return;
        try {
            await api.put(`/Administration/users/${user.userId}/role`, { roleId: selected.roleId });
            toast.success("Role updated.");
            loadData();
        } catch (error) { toast.error(error.response?.data || "Could not update role."); }
    };

    const createUser = async (event) => {
        event.preventDefault();
        try {
            await api.post("/Administration/users", newUser);
            toast.success("User created.");
            setShowCreate(false);
            setNewUser({ firstName: "", lastName: "", email: "", password: "", roleId: roles[0]?.roleId || "" });
            loadData();
        } catch (error) { toast.error(error.response?.data || "Could not create user."); }
    };

    return <main className="app-shell">
        <header className="topbar"><div><p className="eyebrow">DIBA BOOKINGS / CONTROL ROOM</p><h1>Good morning, {firstName}</h1><p className="subhead">A clear view of users, bookings, payments, and system activity.</p></div><Button variant="outline-dark" onClick={clearSession}>Sign out</Button></header>
        <nav className="section-nav" aria-label="Administrator sections">{[["overview", "Overview"], ["users", "Users"], ["bookings", "Booking oversight"], ["audit", "Audit logs"]].map(([key, label]) => <button key={key} className={tab === key ? "active" : ""} onClick={() => setTab(key)}>{label}</button>)}</nav>
        {loading ? <div className="loading"><Spinner animation="border" /> Loading control room</div> : <>
            {tab === "overview" && <><section className="stat-grid"><Stat label="Total users" value={overview.totalUsers} /><Stat label="Active users" value={overview.activeUsers} tone="green" /><Stat label="Total bookings" value={overview.totalBookings} tone="ink" /><Stat label="Pending bookings" value={overview.pendingBookings} tone="amber" /><Stat label="Approved bookings" value={overview.approvedBookings} tone="green" /><Stat label="Rejected bookings" value={overview.rejectedBookings} tone="red" /><Stat label="Cancelled bookings" value={overview.cancelledBookings} tone="red" /><Stat label="Total payments" value={overview.totalPayments} /></section><section className="content-grid"><div className="panel"><div className="panel-heading"><div><p className="eyebrow">LIVE FEED</p><h2>Recent system activity</h2></div><button className="text-button" onClick={() => setTab("audit")}>View all</button></div>{overview.recentActivity.map((item) => <div className="activity-row" key={item.auditLogId}><span className="activity-dot" /><div><strong>{item.action}</strong><p>{item.logDescription || `Action by ${item.userName}`}</p></div><time>{formatDate(item.timestamp)}</time></div>)}</div><div className="panel accent-panel"><p className="eyebrow">ADMINISTRATIVE FOCUS</p><h2>Visibility before intervention.</h2><p>Monitor the whole system here. Use booking actions only when an authorised administrative decision is required.</p><Button onClick={() => setTab("bookings")}>Review bookings</Button></div></section></>}
            {(tab === "users" || tab === "bookings" || tab === "audit") && <section className="panel table-panel"><div className="panel-heading"><div><p className="eyebrow">{tab === "users" ? "USER MANAGEMENT" : tab === "bookings" ? "BOOKING OVERSIGHT" : "ACCOUNTABILITY"}</p><h2>{tab === "users" ? "Users" : tab === "bookings" ? "All bookings" : "Audit logs"}</h2></div>{tab === "users" && <Button onClick={() => setShowCreate(true)}>+ Create user</Button>}</div>{tab !== "audit" && <Form.Control className="search" placeholder={`Search ${tab}...`} value={query} onChange={(event) => setQuery(event.target.value)} />}{tab === "users" && <Table responsive hover><thead><tr><th>Name</th><th>Email</th><th>Role</th><th>Status</th><th>Actions</th></tr></thead><tbody>{filteredUsers.map((user) => <tr key={user.userId}><td>{user.firstName} {user.lastName}</td><td>{user.email}</td><td>{user.role}</td><td><Badge bg={user.isActive ? "success" : "secondary"}>{user.isActive ? "Active" : "Inactive"}</Badge></td><td><button className="table-action" onClick={() => changeRole(user)}>Change role</button><button className="table-action danger" onClick={() => updateUser(user)}>{user.isActive ? "Deactivate" : "Activate"}</button></td></tr>)}</tbody></Table>}{tab === "bookings" && <Table responsive hover><thead><tr><th>Booking</th><th>Organiser</th><th>Event</th><th>Venue</th><th>Submitted</th><th>Status</th></tr></thead><tbody>{filteredBookings.map((booking) => <tr key={booking.bookingId}><td>{shortId(booking.bookingId)}</td><td>{booking.organiserName || shortId(booking.userId)}</td><td>{booking.eventName || shortId(booking.eventId)}</td><td>{booking.venueName || shortId(booking.venueId)}</td><td>{formatDate(booking.bookingDate)}</td><td><Badge bg="light" text="dark">{booking.statusName || shortId(booking.bookingStatusId)}</Badge></td></tr>)}</tbody></Table>}{tab === "audit" && <Table responsive hover><thead><tr><th>Action</th><th>Details</th><th>Performed by</th><th>Date / time</th></tr></thead><tbody>{logs.map((log) => <tr key={log.auditLogId}><td><strong>{log.action}</strong></td><td>{log.logDescription || "-"}</td><td>{log.userName || shortId(log.userId)}</td><td>{formatDate(log.timestamp)}</td></tr>)}</tbody></Table>}</section>}
        </>}
        <Modal show={showCreate} onHide={() => setShowCreate(false)}><Form onSubmit={createUser}><Modal.Header closeButton><Modal.Title>Create user</Modal.Title></Modal.Header><Modal.Body><div className="form-row"><Form.Control placeholder="First name" required value={newUser.firstName} onChange={(event) => setNewUser({ ...newUser, firstName: event.target.value })} /><Form.Control placeholder="Last name" required value={newUser.lastName} onChange={(event) => setNewUser({ ...newUser, lastName: event.target.value })} /></div><Form.Control className="mb-3" type="email" placeholder="Email" required value={newUser.email} onChange={(event) => setNewUser({ ...newUser, email: event.target.value })} /><Form.Control className="mb-3" type="password" placeholder="Temporary password" required value={newUser.password} onChange={(event) => setNewUser({ ...newUser, password: event.target.value })} /><Form.Select value={newUser.roleId} onChange={(event) => setNewUser({ ...newUser, roleId: event.target.value })}>{roles.map((item) => <option key={item.roleId} value={item.roleId}>{item.roleName}</option>)}</Form.Select></Modal.Body><Modal.Footer><Button variant="light" onClick={() => setShowCreate(false)}>Cancel</Button><Button type="submit">Create user</Button></Modal.Footer></Form></Modal>
    </main>;
}

export default AdminDashboard;
