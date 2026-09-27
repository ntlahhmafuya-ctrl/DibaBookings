function SectionHeading({ kicker, title, action, onClick }) { return <div className="section-heading"><div><p className="section-kicker">{kicker}</p><h3>{title}</h3></div>{action && <button className="link-button" onClick={onClick}>{action} <span>→</span></button>}</div>; }

export default SectionHeading;
