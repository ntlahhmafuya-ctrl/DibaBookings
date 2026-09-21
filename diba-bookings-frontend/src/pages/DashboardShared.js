import { Button, Spinner } from "react-bootstrap";

export function Stat({ label, value, tone = "blue" }) {
    return <div className={`stat-card stat-${tone}`}><span>{label}</span><strong>{value}</strong></div>;
}

export function RoleShell({ title, eyebrow, subtitle, onLogout, children }) {
    return <main className="app-shell"><header className="topbar"><div><p className="eyebrow">{eyebrow}</p><h1>{title}</h1><p className="subhead">{subtitle}</p></div><Button variant="outline-dark" onClick={onLogout}>Sign out</Button></header>{children}</main>;
}

export function Loading({ label = "Loading workspace" }) {
    return <div className="loading"><Spinner animation="border" /> {label}</div>;
}
