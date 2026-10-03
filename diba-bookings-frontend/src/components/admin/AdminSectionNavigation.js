const sections = [
    ["overview", "Overview"],
    ["users", "Users"],
    ["bookings", "Booking oversight"],
    ["audit", "Audit logs"]
];

function AdminSectionNavigation({ activeSection, onSelect }) {
    return (
        <nav className="section-nav" aria-label="Administrator sections">
            {sections.map(([key, label]) => (
                <button
                    key={key}
                    className={activeSection === key ? "active" : ""}
                    onClick={() => onSelect(key)}
                >
                    {label}
                </button>
            ))}
        </nav>
    );
}

export default AdminSectionNavigation;
