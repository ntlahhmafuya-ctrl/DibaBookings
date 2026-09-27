import { Link } from "react-router-dom";
import conferenceCentre from "../assets/images/Conference centre.jpg";
import lectureTheatre from "../assets/images/Theatre (2).jpg";
import diningRoom from "../assets/images/Dining Room.jpg";
import campusView from "../assets/images/Nelson Mandela University.jpg";

const features = [
    { icon: "⌕", title: "Find a Venue", text: "Explore conference facilities and choose a space that suits your event." },
    { icon: "▣", title: "Manage Events", text: "Create and organise your event information in one clear workspace." },
    { icon: "◷", title: "Track Bookings", text: "See whether your booking is pending, approved, rejected or completed." },
    { icon: "✦", title: "Stay Updated", text: "Keep up with important booking changes and notifications." },
];

const venues = [
    { image: conferenceCentre, title: "Conference Centre", text: "A welcoming setting for conferences, meetings and academic events.", tag: "Conferences" },
    { image: lectureTheatre, title: "Lecture Theatre", text: "A purpose-built room for presentations, talks and larger gatherings.", tag: "Presentations" },
    { image: diningRoom, title: "Dining & Breakout Spaces", text: "Comfortable spaces to connect, recharge and host your guests.", tag: "Hospitality" },
];

const steps = [
    ["01", "Find a venue", "Explore spaces and find a facility that fits your event."],
    ["02", "Create your event", "Add the date, details and expected number of attendees."],
    ["03", "Submit your booking", "Send your request to the conference centre for review."],
    ["04", "Track your booking", "Monitor progress and stay informed as plans come together."],
];

function LandingPage() {
    return (
        <main className="landing-page">
            <nav className="landing-nav" aria-label="Main navigation">
                <a className="landing-brand" href="#top" aria-label="DIBA Bookings home"><span className="brand-mark">D</span><span>DIBA <b>Bookings</b></span></a>
                <div className="landing-links">
                    <a className="active" href="#top">Home</a>
                    <a href="#venues">Venues</a>
                    <a href="#how-it-works">How It Works</a>
                    <a href="#about">About</a>
                </div>
                <div className="landing-account"><Link to="/login">Login</Link><Link className="landing-register" to="/register">Register <span aria-hidden="true">↗</span></Link></div>
            </nav>

            <section className="landing-hero" id="top">
                <img className="hero-image" src={conferenceCentre} alt="Conference centre surrounded by greenery" />
                <div className="hero-overlay" />
                <div className="hero-content">
                    <p className="landing-kicker">DIBA BOOKINGS / VENUE DISCOVERY</p>
                    <h1>Plan your event.<br /><em>Find the right venue.</em></h1>
                    <p className="hero-copy">Discover conference facilities, create events and manage your venue bookings in one place.</p>
                    <div className="hero-actions"><a className="landing-button landing-button-yellow" href="#venues">Find a Venue <span aria-hidden="true">↗</span></a><Link className="landing-button landing-button-light" to="/register">Create an Account</Link></div>
                </div>
                <div className="hero-note"><span>01</span><span className="hero-note-line" /><span>Spaces for ideas, connection and progress</span></div>
            </section>

            <section className="discovery-wrap" aria-label="Venue discovery">
                <div className="discovery-intro"><p className="landing-kicker">START EXPLORING</p><h2>Find a space that<br /><em>brings it together.</em></h2></div>
                <div className="discovery-form">
                    <label><span>⌖</span><small>Where is your event?</small><strong>Select a venue or location</strong></label>
                    <label><span>◷</span><small>Event date</small><strong>Select a date</strong></label>
                    <label><span>♧</span><small>Guests</small><strong>Number of attendees</strong></label>
                    <a className="search-button" href="#venues">Search Venues <span aria-hidden="true">↗</span></a>
                </div>
            </section>

            <section className="landing-section introduction" id="about">
                <div className="section-label">ABOUT DIBA</div>
                <div className="intro-copy"><h2>Everything you need to<br /><em>make an event happen.</em></h2><p>DIBA Bookings brings venue discovery, event planning and booking management into one simple experience for organisers and conference teams.</p><a className="text-link" href="#how-it-works">See how it works <span aria-hidden="true">→</span></a></div>
                <div className="intro-image"><img src={campusView} alt="Aerial view of the campus and surrounding coastline" /><span>One connected space<br /><b>for every event.</b></span></div>
            </section>

            <section className="landing-section feature-section">
                <div className="section-heading"><div><p className="landing-kicker">ONE CLEAR WORKSPACE</p><h2>Built around the way<br /><em>events actually happen.</em></h2></div><p>From the first venue search to the final confirmation, keep every important detail close at hand.</p></div>
                <div className="feature-grid">{features.map((feature) => <article className="feature-card" key={feature.title}><span className="feature-icon" aria-hidden="true">{feature.icon}</span><h3>{feature.title}</h3><p>{feature.text}</p><span className="card-arrow" aria-hidden="true">↗</span></article>)}</div>
            </section>

            <section className="landing-section venue-section" id="venues">
                <div className="section-heading"><div><p className="landing-kicker">THE VENUE COLLECTION</p><h2>Explore our <em>venues.</em></h2></div><p>Purposeful spaces for conversations, presentations and shared moments.</p></div>
                <div className="venue-grid">{venues.map((venue) => <article className="venue-card" key={venue.title}><div className="venue-image"><img src={venue.image} alt={venue.title} /><span>{venue.tag}</span></div><div className="venue-card-body"><h3>{venue.title}</h3><p>{venue.text}</p><Link to="/register">View Venue <span aria-hidden="true">↗</span></Link></div></article>)}</div>
            </section>

            <section className="steps-section" id="how-it-works"><div className="landing-section steps-inner"><div className="section-heading"><div><p className="landing-kicker">A SIMPLE FLOW</p><h2>How DIBA Bookings<br /><em>works.</em></h2></div><p>Good planning starts with a clear next step.</p></div><div className="steps-grid">{steps.map(([number, title, text]) => <article className="step" key={number}><span>{number}</span><div><h3>{title}</h3><p>{text}</p></div></article>)}</div></div></section>

            <section className="landing-section value-section"><div className="value-panel"><div><p className="landing-kicker">WHY DIBA BOOKINGS</p><h2>A simpler way to manage<br /><em>your venue bookings.</em></h2></div><div className="value-list"><span>✓ Easy venue discovery</span><span>✓ Centralised booking management</span><span>✓ Clear booking status</span><span>✓ Event management</span><span>✓ Notifications and updates</span><span>✓ A simple booking process</span></div></div></section>

            <section className="landing-cta"><div><p className="landing-kicker">YOUR NEXT EVENT STARTS HERE</p><h2>Ready to plan<br /><em>what comes next?</em></h2></div><div><p>Create your DIBA Bookings account and start managing your venue bookings.</p><div className="cta-actions"><Link className="landing-button landing-button-yellow" to="/register">Create an Account <span aria-hidden="true">↗</span></Link><Link className="landing-button landing-button-outline" to="/login">Login</Link></div></div></section>

            <footer className="landing-footer"><div className="footer-brand"><a className="landing-brand" href="#top"><span className="brand-mark">D</span><span>DIBA <b>Bookings</b></span></a><p>A clear, connected way to discover venues and manage events.</p></div><div className="footer-links"><div><strong>Navigation</strong><a href="#top">Home</a><a href="#venues">Venues</a><a href="#how-it-works">How It Works</a><a href="#about">About</a></div><div><strong>Account</strong><Link to="/login">Login</Link><Link to="/register">Register</Link></div><div><strong>Support</strong><span>Contact</span><span>Help</span></div></div><div className="footer-bottom"><span>© 2026 DIBA Bookings</span><span>Venue booking, made clearer.</span></div></footer>
        </main>
    );
}

export default LandingPage;