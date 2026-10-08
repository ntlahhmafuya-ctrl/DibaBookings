function StatCard({ label, value, tone = "blue" }) {
    return (
        <div className={`stat-card stat-${tone}`}>
            <span>{label}</span>
            <strong>{value}</strong>
        </div>
    );
}

export default StatCard;
