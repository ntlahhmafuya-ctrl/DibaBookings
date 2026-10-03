import { Spinner } from "react-bootstrap";

function LoadingIndicator({ label = "Loading workspace" }) {
    return (
        <div className="loading">
            <Spinner animation="border" /> {label}
        </div>
    );
}

export default LoadingIndicator;
