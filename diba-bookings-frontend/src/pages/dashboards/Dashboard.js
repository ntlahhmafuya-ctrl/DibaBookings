import AdminDashboard from "./AdminDashboard";
import OrganizerDashboard from "./OrganizerDashboard";
import StaffDashboard from "./StaffDashboard";

function Dashboard() {
    const role = localStorage.getItem("role");

    if (role === "Administrator") return <AdminDashboard />;
    if (role === "Staff") return <StaffDashboard />;
    
    return <OrganizerDashboard />;
}

export default Dashboard;
