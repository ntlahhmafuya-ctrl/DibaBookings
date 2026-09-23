import { toast } from "react-toastify";

import { clearSession } from "../../../utils/dashboardUtils";

const navItems = [
    ["⌂", "Dashboard", "dashboard"],
    ["⌖", "Find a Venue", "venues"],
    ["▣", "My Bookings", "bookings"],
    ["◫", "My Events", "events"],
    ["◌", "Notifications", "notifications"]
];

function OrganizerSidebar({
    view,
    unread,
    drawerOpen,
    setDrawerOpen,
    goTo
}) {
    return (
        <>
            {drawerOpen && (
                <button
                    className="organizer-overlay"
                    aria-label="Close navigation"
                    onClick={() => setDrawerOpen(false)}
                />
            )}

            <aside
                className={`organizer-drawer ${
                    drawerOpen ? "open" : ""
                }`}
            >
                <div className="drawer-header">
                    <div className="brand-lockup">
                        <span className="brand-mark">
                            D
                        </span>

                        <div>
                            <strong>DIBA</strong>
                            <span>Bookings</span>
                        </div>
                    </div>

                    <button
                        className="drawer-close"
                        aria-label="Close menu"
                        onClick={() => setDrawerOpen(false)}
                    >
                        ×
                    </button>
                </div>

                <p className="sidebar-label">
                    Workspace
                </p>

                <nav
                    className="dashboard-nav"
                    aria-label="Dashboard navigation"
                >
                    {navItems.map(
                        ([icon, label, key]) => (
                            <button
                                key={key}
                                type="button"
                                className={`nav-item ${
                                    view === key
                                        ? "active"
                                        : ""
                                }`}
                                onClick={() =>
                                    goTo(key)
                                }
                            >
                                <span className="nav-icon">
                                    {icon}
                                </span>

                                <span>{label}</span>

                                {key ===
                                    "notifications" &&
                                    unread > 0 && (
                                        <span className="nav-count">
                                            {unread}
                                        </span>
                                    )}
                            </button>
                        )
                    )}
                </nav>

                <div className="sidebar-bottom">
                    <button
                        type="button"
                        className="nav-item"
                        onClick={() =>
                            toast.info(
                                "Profile settings are not available in the current API."
                            )
                        }
                    >
                        <span className="nav-icon">
                            ◎
                        </span>

                        <span>Profile</span>
                    </button>

                    <button
                        type="button"
                        className="nav-item"
                        onClick={() =>
                            toast.info(
                                "Settings are not available in the current API."
                            )
                        }
                    >
                        <span className="nav-icon">
                            ⚙
                        </span>

                        <span>Settings</span>
                    </button>

                    <button
                        type="button"
                        className="nav-item logout-item"
                        onClick={clearSession}
                    >
                        <span className="nav-icon">
                            ↪
                        </span>

                        <span>Logout</span>
                    </button>
                </div>
            </aside>
        </>
    );
}

export default OrganizerSidebar;