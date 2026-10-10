import { useEffect, useMemo, useState } from "react";
import { toast } from "react-toastify";

import AdminOverviewPanel from "../../components/admin/AdminOverviewPanel";
import AdminRecordsPanel from "../../components/admin/AdminRecordsPanel";
import AdminSectionNavigation from "../../components/admin/AdminSectionNavigation";
import CreateUserModal from "../../components/admin/CreateUserModal";
import DashboardShell from "../../components/common/DashboardShell";
import LoadingIndicator from "../../components/common/LoadingIndicator";
import {
    createUser,
    getAdminOverview,
    getAuditLogs,
    getRoles,
    getUsers,
    getPrivacyRequests,
    updatePrivacyRequest,
    updateUserRole,
    updateUserStatus
} from "../../services/adminService";
import { getBookings } from "../../services/bookingService";
import { emptyOverview } from "../../utils/dashboardUtils";
import { clearSession } from "../../utils/sessionUtils";

/**
 * ADMINISTRATOR DASHBOARD
 * Responsibility: load system overview, users, bookings, audit history, and privacy requests for authorised administrators.
 */
function AdminDashboard() {
    const [overview, setOverview] = useState(emptyOverview);
    const [users, setUsers] = useState([]);
    const [bookings, setBookings] = useState([]);
    const [logs, setLogs] = useState([]);
    const [privacyRequests, setPrivacyRequests] = useState([]);
    const [roles, setRoles] = useState([]);
    const [query, setQuery] = useState("");
    const [activeSection, setActiveSection] = useState("overview");
    const [loading, setLoading] = useState(true);
    const [showCreate, setShowCreate] = useState(false);
    const [newUser, setNewUser] = useState({
        firstName: "",
        lastName: "",
        email: "",
        password: "",
        roleId: ""
    });
    const firstName = localStorage.getItem("firstName") || "Administrator";

    const loadData = async () => {
        setLoading(true);
        try {
            const [
                summary,
                userResponse,
                bookingResponse,
                logResponse,
                roleResponse,
                privacyResponse
            ] = await Promise.all([
                getAdminOverview(),
                getUsers(),
                getBookings(),
                getAuditLogs(),
                getRoles(),
                getPrivacyRequests()
            ]);
            setOverview(summary.data);
            setUsers(userResponse.data);
            setBookings(bookingResponse.data);
            setLogs(logResponse.data);
            setRoles(roleResponse.data);
            setPrivacyRequests(privacyResponse.data);
            setNewUser((current) => ({
                ...current,
                roleId:
                    current.roleId || roleResponse.data[0]?.roleId || ""
            }));
        } catch (error) {
            toast.error(
                error.response?.data || "Could not load administrator data."
            );
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadData();
    }, []);

    const filteredUsers = useMemo(
        () =>
            users.filter((user) =>
                `${user.firstName} ${user.lastName} ${user.email} ${user.role}`
                    .toLowerCase()
                    .includes(query.toLowerCase())
            ),
        [users, query]
    );

    const filteredBookings = useMemo(
        () =>
            bookings.filter((booking) =>
                `${booking.bookingId} ${booking.organiserName} ${booking.eventName} ${booking.venueName}`
                    .toLowerCase()
                    .includes(query.toLowerCase())
            ),
        [bookings, query]
    );

    const updateUser = async (user) => {
        try {
            await updateUserStatus(user.userId, !user.isActive);
            toast.success(
                `User ${user.isActive ? "deactivated" : "activated"}.`
            );
            loadData();
        } catch (error) {
            toast.error(error.response?.data || "Could not update user status.");
        }
    };

    const changeRole = async (user) => {
        const roleName = window.prompt(
            `Enter the role name for ${user.firstName} (Administrator, Staff, or Event Organiser):`
        );
        const selected = roles.find(
            (item) =>
                item.roleName.toLowerCase() === roleName?.toLowerCase()
        );
        if (!selected) return;

        try {
            await updateUserRole(user.userId, selected.roleId);
            toast.success("Role updated.");
            loadData();
        } catch (error) {
            toast.error(error.response?.data || "Could not update role.");
        }
    };

    const handleCreateUser = async (event) => {
        event.preventDefault();
        try {
            await createUser(newUser);
            toast.success("User created.");
            setShowCreate(false);
            setNewUser({
                firstName: "",
                lastName: "",
                email: "",
                password: "",
                roleId: roles[0]?.roleId || ""
            });
            loadData();
        } catch (error) {
            toast.error(error.response?.data || "Could not create user.");
        }
    };


    // REVIEW PRIVACY REQUEST: let an Administrator update status and record a response for the requester.
    const reviewPrivacyRequest = async (request) => {
        const status = window.prompt(
            "Enter status: Submitted, In Review, Need More Information, Resolved, or Rejected",
            request.status
        );
        if (!status) return;

        const response = window.prompt(
            "Enter a response for the requester (optional):",
            request.response || ""
        );
        if (response === null) return;

        try {
            await updatePrivacyRequest(request.privacyRequestId, { status, response });
            toast.success("Privacy request updated.");
            await loadData();
        } catch (error) {
            toast.error(error.response?.data || "Could not update privacy request.");
        }
    };

    const updateNewUserField = (field, value) => {
        setNewUser((current) => ({
            ...current,
            [field]: value
        }));
    };

    return (
        <DashboardShell
            title={`Good morning, ${firstName}`}
            eyebrow="DIBA BOOKINGS / CONTROL ROOM"
            subtitle="A clear view of users, bookings, payments, and system activity."
            onLogout={clearSession}
        >
            <AdminSectionNavigation
                activeSection={activeSection}
                onSelect={setActiveSection}
            />

            {loading ? (
                <LoadingIndicator label="Loading control room" />
            ) : activeSection === "overview" ? (
                <AdminOverviewPanel
                    overview={overview}
                    onShowAudit={() => setActiveSection("audit")}
                    onShowBookings={() => setActiveSection("bookings")}
                />
            ) : (
                <AdminRecordsPanel
                    activeSection={activeSection}
                    query={query}
                    onQueryChange={setQuery}
                    users={filteredUsers}
                    bookings={filteredBookings}
                    logs={logs}
                    privacyRequests={privacyRequests}
                    onUpdatePrivacyRequest={reviewPrivacyRequest}
                    onCreateUser={() => setShowCreate(true)}
                    onChangeRole={changeRole}
                    onUpdateUser={updateUser}
                />
            )}

            <CreateUserModal
                show={showCreate}
                onHide={() => setShowCreate(false)}
                newUser={newUser}
                onChange={updateNewUserField}
                roles={roles}
                onSubmit={handleCreateUser}
            />
        </DashboardShell>
    );
}

export default AdminDashboard;
