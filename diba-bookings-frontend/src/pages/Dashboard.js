import AdminDashboard from "./AdminDashboard";
import OrganizerDashboard from "./OrganizerDashboard";
import UserDashboard from "./UserDashboard";

function Dashboard() {
    const role = localStorage.getItem("role");

    if (role === "Administrator") return <AdminDashboard />;
    if (role === "Staff") return <UserDashboard />;
    
    return <OrganizerDashboard />;
}

export default Dashboard;
