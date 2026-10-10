import { useEffect, useState } from "react";
import { Button, Card, Col, Container, Form, Row, Spinner } from "react-bootstrap";
import { Link, useNavigate } from "react-router-dom";
import { toast } from "react-toastify";
import api from "../services/api";

/**
 * PROFILE PAGE
 * Responsibility: let the signed-in user view and update their own name and email.
 * The API derives the user ID from the authentication token, so this page never
 * submits a user ID that could be changed to target somebody else's account.
 */
function Profile() {
    const navigate = useNavigate();
    const [formData, setFormData] = useState({
        firstName: "",
        lastName: "",
        email: "",
        role: ""
    });
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [errorMessage, setErrorMessage] = useState("");

    // LOAD PROFILE: fetch the authenticated user's current details from the API.
    useEffect(() => {
        const loadProfile = async () => {
            if (!localStorage.getItem("token")) {
                navigate("/login", { replace: true });
                return;
            }

            try {
                const response = await api.get("/Users/me");
                setFormData({
                    firstName: response.data.firstName || "",
                    lastName: response.data.lastName || "",
                    email: response.data.email || "",
                    role: response.data.role || ""
                });
            } catch (error) {
                if (error.response?.status === 401) {
                    setErrorMessage("Your session has expired. Please sign in again.");
                } else {
                    setErrorMessage("We could not load your profile. Please try again.");
                }
            } finally {
                setLoading(false);
            }
        };

        loadProfile();
    }, [navigate]);

    // HANDLE INPUT: keep the displayed form values in React state as the user types.
    const handleChange = (event) => {
        setFormData((current) => ({
            ...current,
            [event.target.name]: event.target.value
        }));
    };

    // SAVE PROFILE: validate basic input, then send only editable profile fields.
    const handleSubmit = async (event) => {
        event.preventDefault();
        setErrorMessage("");

        const firstName = formData.firstName.trim();
        const lastName = formData.lastName.trim();
        const email = formData.email.trim();

        if (!firstName || !lastName || !email) {
            setErrorMessage("First name, last name, and email are required.");
            return;
        }

        setSaving(true);
        try {
            await api.put("/Users/me", { firstName, lastName, email });

            // Keep the visible name/email in sync with the current session's display values.
            localStorage.setItem("firstName", firstName);
            localStorage.setItem("lastName", lastName);
            localStorage.setItem("email", email);

            setFormData((current) => ({ ...current, firstName, lastName, email }));
            toast.success("Your profile has been updated.");
        } catch (error) {
            const data = error.response?.data;
            const message = typeof data === "string"
                ? data
                : data?.message || data?.title || "We could not save your changes. Please try again.";
            setErrorMessage(message);
        } finally {
            setSaving(false);
        }
    };

    return (
        <main className="profile-page">
            <header className="profile-header">
                <Link className="profile-brand" to="/dashboard">DIBA <strong>Bookings</strong></Link>
                <Link to="/dashboard">Back to dashboard</Link>
            </header>

            <Container className="profile-container">
                <Row className="justify-content-center">
                    <Col xs={12} md={9} lg={7}>
                        <Card className="profile-card">
                            <Card.Body>
                                <p className="profile-eyebrow">ACCOUNT / PERSONAL DETAILS</p>
                                <h1>My profile</h1>
                                <p className="profile-intro">
                                    Review and update the name and email address linked to your account.
                                </p>

                                {loading ? (
                                    <div className="profile-loading" role="status">
                                        <Spinner animation="border" size="sm" /> Loading your profile…
                                    </div>
                                ) : (
                                    <Form onSubmit={handleSubmit}>
                                        <Form.Group className="mb-3" controlId="profileFirstName">
                                            <Form.Label>First name</Form.Label>
                                            <Form.Control
                                                name="firstName"
                                                autoComplete="given-name"
                                                maxLength={100}
                                                value={formData.firstName}
                                                onChange={handleChange}
                                                required
                                            />
                                        </Form.Group>

                                        <Form.Group className="mb-3" controlId="profileLastName">
                                            <Form.Label>Last name</Form.Label>
                                            <Form.Control
                                                name="lastName"
                                                autoComplete="family-name"
                                                maxLength={100}
                                                value={formData.lastName}
                                                onChange={handleChange}
                                                required
                                            />
                                        </Form.Group>

                                        <Form.Group className="mb-3" controlId="profileEmail">
                                            <Form.Label>Email address</Form.Label>
                                            <Form.Control
                                                type="email"
                                                name="email"
                                                autoComplete="email"
                                                maxLength={254}
                                                value={formData.email}
                                                onChange={handleChange}
                                                required
                                            />
                                        </Form.Group>

                                        <Form.Group className="mb-4" controlId="profileRole">
                                            <Form.Label>Account role</Form.Label>
                                            <Form.Control value={formData.role} readOnly />
                                            <Form.Text muted>
                                                Your role is managed by an administrator and cannot be changed here.
                                            </Form.Text>
                                        </Form.Group>

                                        {errorMessage && (
                                            <p className="profile-error" role="alert">{errorMessage}</p>
                                        )}

                                        <div className="profile-actions">
                                            <Button type="submit" disabled={saving}>
                                                {saving ? "Saving changes…" : "Save changes"}
                                            </Button>
                                            <Button
                                                type="button"
                                                variant="outline-secondary"
                                                onClick={() => navigate("/dashboard")}
                                                disabled={saving}
                                            >
                                                Cancel
                                            </Button>
                                        </div>
                                    </Form>
                                )}

                                <p className="profile-privacy-link">
                                    Need a copy of your information? Visit the <Link to="/privacy">Privacy Notice</Link>.
                                </p>
                            </Card.Body>
                        </Card>
                    </Col>
                </Row>
            </Container>
        </main>
    );
}

export default Profile;
