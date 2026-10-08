function WelcomePanel({ firstName, onFind }) {
    return (
        <section className="welcome-panel">
            <div className="welcome-content">
                <p className="section-kicker">YOUR WORKSPACE</p>
                <h2>Good morning, {firstName}</h2>
                <p>
                    Welcome to your booking workspace. Find a venue, manage
                    your events and keep track of your bookings.
                </p>
            </div>
            <button
                type="button"
                className="primary-action"
                onClick={onFind}
            >
                Find a Venue
            </button>
        </section>
    );
}

export default WelcomePanel;
