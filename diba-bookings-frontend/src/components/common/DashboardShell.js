import { Button } from "react-bootstrap";
import { Link } from "react-router-dom";

/**
 * DASHBOARD SHELL
 * Responsibility: provide a shared header, profile shortcut, sign-out action, and content area for staff/admin screens.
 */
function DashboardShell({ title, eyebrow, subtitle, onLogout, children }) {
    return (
        <main className="app-shell">
            <header className="topbar">
                <div>
                    <p className="eyebrow">{eyebrow}</p>
                    <h1>{title}</h1>
                    <p className="subhead">{subtitle}</p>
                </div>
                <div className="d-flex align-items-center gap-2">
                    <Link className="btn btn-outline-primary" to="/profile">My profile</Link>
                    <Button variant="outline-dark" onClick={onLogout}>
                        Sign out
                    </Button>
                </div>
            </header>
            {children}
        </main>
    );
}

export default DashboardShell;
