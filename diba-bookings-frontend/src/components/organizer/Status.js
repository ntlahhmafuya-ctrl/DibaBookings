import { statusClass } from "../../utils/dashboardUtils";

function Status({ status = "Pending" }) { return <span className={`status-pill ${statusClass(status)}`}>{status === "Approved" ? "✓ " : status === "Rejected" ? "! " : status === "Cancelled" ? "○ " : status === "Completed" ? "◆ " : "● "}{status}</span>; }

export default Status;
