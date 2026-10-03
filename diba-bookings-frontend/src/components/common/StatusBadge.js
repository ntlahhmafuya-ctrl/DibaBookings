import { statusClass } from "../../utils/bookingUtils";

function StatusBadge({ status = "Pending" }) {
    const iconByStatus = {
        Approved: "✓ ",
        Rejected: "! ",
        Cancelled: "○ ",
        Completed: "◆ "
    };

    return (
        <span className={`status-pill ${statusClass(status)}`}>
            {iconByStatus[status] || "● "}
            {status}
        </span>
    );
}

export default StatusBadge;
