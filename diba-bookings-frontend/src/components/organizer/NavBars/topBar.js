function OrganizerTopNav({
    title,
    firstName,
    fullName,
    unread,
    setDrawerOpen,
    goTo
}) {
    return (
        <header className="organizer-header">
            <div className="header-title">
                <button
                    type="button"
                    className="menu-button"
                    aria-label="Open menu"
                    onClick={() => setDrawerOpen(true)}
                >
                    ☰
                </button>

                <div>
                    <h1>{title}</h1>
                </div>
            </div>

            <div className="header-user">
                <button
                    type="button"
                    className="notification-button"
                    aria-label="Open notifications"
                    onClick={() =>
                        goTo("notifications")
                    }
                >
                    ◌

                    {unread > 0 && (
                        <i>{unread}</i>
                    )}
                </button>

                <span className="avatar">
                    {firstName
                        .charAt(0)
                        .toUpperCase()}
                </span>

                <div>
                    <strong>{fullName}</strong>

                    <span>
                        Event Organiser
                    </span>
                </div>
            </div>
        </header>
    );
}

export default OrganizerTopNav;