import { Button } from "react-bootstrap";
import StatCard from "../common/StatCard";
import { formatDate } from "../../utils/dateUtils";

function AdminOverviewPanel({ overview, onShowAudit, onShowBookings }) {
    return (
        <>
            <section className="stat-grid">
                <StatCard label="Total users" value={overview.totalUsers} />
                <StatCard
                    label="Active users"
                    value={overview.activeUsers}
                    tone="green"
                />
                <StatCard
                    label="Total bookings"
                    value={overview.totalBookings}
                    tone="ink"
                />
                <StatCard
                    label="Pending bookings"
                    value={overview.pendingBookings}
                    tone="amber"
                />
                <StatCard
                    label="Approved bookings"
                    value={overview.approvedBookings}
                    tone="green"
                />
                <StatCard
                    label="Rejected bookings"
                    value={overview.rejectedBookings}
                    tone="red"
                />
                <StatCard
                    label="Cancelled bookings"
                    value={overview.cancelledBookings}
                    tone="red"
                />
                <StatCard
                    label="Total payments"
                    value={overview.totalPayments}
                />
            </section>

            <section className="content-grid">
                <div className="panel">
                    <div className="panel-heading">
                        <div>
                            <p className="eyebrow">LIVE FEED</p>
                            <h2>Recent system activity</h2>
                        </div>
                        <button className="text-button" onClick={onShowAudit}>
                            View all
                        </button>
                    </div>
                    {overview.recentActivity.map((item) => (
                        <div className="activity-row" key={item.auditLogId}>
                            <span className="activity-dot" />
                            <div>
                                <strong>{item.action}</strong>
                                <p>
                                    {item.logDescription ||
                                        `Action by ${item.userName}`}
                                </p>
                            </div>
                            <time>{formatDate(item.timestamp)}</time>
                        </div>
                    ))}
                </div>

                <div className="panel accent-panel">
                    <p className="eyebrow">ADMINISTRATIVE FOCUS</p>
                    <h2>Visibility before intervention.</h2>
                    <p>
                        Monitor the whole system here. Use booking actions only
                        when an authorised administrative decision is required.
                    </p>
                    <Button onClick={onShowBookings}>
                        Review bookings
                    </Button>
                </div>
            </section>
        </>
    );
}

export default AdminOverviewPanel;
