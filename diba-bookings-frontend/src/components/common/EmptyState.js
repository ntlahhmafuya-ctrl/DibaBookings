function EmptyState({ title, text, action, onAction }) {
    return (
        <div className="organizer-empty-state">
            <span className="empty-icon">⌖</span>
            <h4>{title}</h4>
            <p>{text}</p>
            {action && (
                <button
                    className="outline-action"
                    type="button"
                    onClick={onAction}
                >
                    {action}
                </button>
            )}
        </div>
    );
}

export default EmptyState;
