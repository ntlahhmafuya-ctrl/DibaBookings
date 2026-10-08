import { render, screen } from "@testing-library/react";
import Dashboard from "./pages/dashboards/Dashboard";

jest.mock("./pages/dashboards/AdminDashboard", () => {
    const React = require("react");
    return function MockAdminDashboard() {
        return React.createElement("div", null, "Administrator dashboard");
    };
});

jest.mock("./pages/dashboards/OrganizerDashboard", () => {
    const React = require("react");
    return function MockOrganizerDashboard() {
        return React.createElement("div", null, "Organizer dashboard");
    };
});

jest.mock("./pages/dashboards/StaffDashboard", () => {
    const React = require("react");
    return function MockStaffDashboard() {
        return React.createElement("div", null, "Staff dashboard");
    };
});

beforeEach(() => {
    localStorage.clear();
});

test("renders the administrator dashboard for administrator roles", () => {
    localStorage.setItem("role", "Administrator");
    render(<Dashboard />);
    expect(screen.getByText("Administrator dashboard")).toBeInTheDocument();
});

test("renders the staff dashboard for staff roles", () => {
    localStorage.setItem("role", "Staff");
    render(<Dashboard />);
    expect(screen.getByText("Staff dashboard")).toBeInTheDocument();
});

test("defaults non-staff roles to the organizer dashboard", () => {
    localStorage.setItem("role", "Event Organiser");
    render(<Dashboard />);
    expect(screen.getByText("Organizer dashboard")).toBeInTheDocument();
});
