// components/organizer/VenueBrowser.js

import conferenceCentre from "../../../assets/images/Conference centre.jpg";
import theatreImage from "../../../assets/images/Theatre (2).jpg";
import diningImage from "../../../assets/images/Dining Room.jpg";
import VenueCard from "./VenueCard";
import Empty from "../Empty";

const images = [
    conferenceCentre,
    theatreImage,
    diningImage
];

function VenueBrowser({ venues, query, setQuery, onOpen }) { return <section className="workspace-view"><div className="workspace-toolbar"><div><p className="section-kicker">VENUE DIRECTORY</p><h2>Choose a space that fits.</h2></div><input className="workspace-search" placeholder="Search venues or locations" value={query} onChange={(event) => setQuery(event.target.value)} /></div>{venues.length ? <div className="venue-browser-grid">{venues.map((venue, index) => <VenueCard key={venue.venueId} venue={venue} image={images[index % images.length]} onOpen={onOpen} />)}</div> : <Empty title="No venues found" text="Try another venue name or location." />}</section>; }
export default VenueBrowser;