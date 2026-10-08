import { Button } from "react-bootstrap";

function DashboardShell({ title, eyebrow, subtitle, onLogout, children }) {
    return (
        <main className="app-shell">
            <header className="topbar">
                <div>
                    <p className="eyebrow">{eyebrow}</p>
                    <h1>{title}</h1>
                    <p className="subhead">{subtitle}</p>
                </div>
                <Button variant="outline-dark" onClick={onLogout}>
                    Sign out
                </Button>
            </header>
            {children}
        </main>
    );
}

export default DashboardShell;
