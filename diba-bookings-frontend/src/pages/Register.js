import { useState } from "react";
import { Form, Button, Card, Container, Row, Col } from "react-bootstrap";
import { Link, useNavigate } from "react-router-dom";
import { toast } from "react-toastify";

import { registerUser } from "../services/authService";

function Register() {
    const navigate = useNavigate();

    const [formData, setFormData] = useState({
        firstName: "",
        lastName: "",
        email: "",
        password: "",
    });

    const [loading, setLoading] = useState(false);

    const handleChange = (event) => {
        setFormData({
            ...formData,
            [event.target.name]: event.target.value,
        });
    };

    const handleSubmit = async (event) => {
        event.preventDefault();

        setLoading(true);

        try {
            await registerUser(formData);

            toast.success("Registration successful!");

            navigate("/login");
        } catch (error) {
            const message =
                error.response?.data ||
                "Registration failed. Please try again.";

            toast.error(message);
        } finally {
            setLoading(false);
        }
    };

    return (
        <Container className="mt-5">
            <Row className="justify-content-center">
                <Col md={6} lg={5}>
                    <Card className="shadow">
                        <Card.Body className="p-4">

                            <h2 className="text-center mb-4">
                                Create Account
                            </h2>

                            <Form onSubmit={handleSubmit}>

                                <Form.Group className="mb-3">
                                    <Form.Label>
                                        First Name
                                    </Form.Label>

                                    <Form.Control
                                        type="text"
                                        name="firstName"
                                        value={formData.firstName}
                                        onChange={handleChange}
                                        required
                                    />
                                </Form.Group>

                                <Form.Group className="mb-3">
                                    <Form.Label>
                                        Last Name
                                    </Form.Label>

                                    <Form.Control
                                        type="text"
                                        name="lastName"
                                        value={formData.lastName}
                                        onChange={handleChange}
                                        required
                                    />
                                </Form.Group>

                                <Form.Group className="mb-3">
                                    <Form.Label>
                                        Email
                                    </Form.Label>

                                    <Form.Control
                                        type="email"
                                        name="email"
                                        value={formData.email}
                                        onChange={handleChange}
                                        required
                                    />
                                </Form.Group>

                                <Form.Group className="mb-4">
                                    <Form.Label>
                                        Password
                                    </Form.Label>

                                    <Form.Control
                                        type="password"
                                        name="password"
                                        value={formData.password}
                                        onChange={handleChange}
                                        required
                                    />
                                </Form.Group>

                                <Button
                                    type="submit"
                                    variant="primary"
                                    className="w-100"
                                    disabled={loading}
                                >
                                    {loading
                                        ? "Creating Account..."
                                        : "Register"}
                                </Button>

                            </Form>

                            <p className="registration-privacy-note">
                                We use your account and booking information to
                                operate DIBA Bookings. Read our{" "}
                                <Link to="/privacy">Privacy Notice</Link> to
                                understand how personal information is handled.
                            </p>

                            <div className="text-center mt-3">
                                Already have an account?{" "}
                                <Link to="/login">
                                    Login
                                </Link>
                            </div>

                        </Card.Body>
                    </Card>
                </Col>
            </Row>
        </Container>
    );
}

export default Register;