import { useState } from "react";
import {
    Form,
    Button,
    Card,
    Container,
    Row,
    Col,
} from "react-bootstrap";

import { Link, useNavigate } from "react-router-dom";
import { toast } from "react-toastify";

import { loginUser } from "../services/authService";

function Login() {
    const navigate = useNavigate();

    const [formData, setFormData] = useState({
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
            const data = await loginUser(formData);

            // Save login information for now
            localStorage.setItem("token", data.token);
            localStorage.setItem("userId", data.userId);
            localStorage.setItem("firstName", data.firstName);
            localStorage.setItem("lastName", data.lastName);
            localStorage.setItem("email", data.email);
            localStorage.setItem("role", data.role);

            toast.success("Login successful!");

            navigate("/dashboard");

        } catch (error) {
            const message =
                error.response?.data ||
                "Invalid email or password.";

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
                                DIBA Bookings
                            </h2>

                            <h5 className="text-center mb-4">
                                Login
                            </h5>

                            <Form onSubmit={handleSubmit}>

                                <Form.Group className="mb-3">
                                    <Form.Label>
                                        Email
                                    </Form.Label>

                                    <Form.Control
                                        type="email"
                                        name="email"
                                        value={formData.email}
                                        onChange={handleChange}
                                        placeholder="Enter your email"
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
                                        placeholder="Enter your password"
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
                                        ? "Logging in..."
                                        : "Login"}
                                </Button>

                            </Form>

                            <p className="registration-privacy-note">
                                DIBA Bookings uses account and booking information
                                to provide this service. Read the{" "}
                                <Link to="/privacy">Privacy Notice</Link>.
                            </p>

                            <div className="text-center mt-3">
                                Don't have an account?{" "}
                                <Link to="/register">
                                    Register
                                </Link>
                            </div>

                        </Card.Body>
                    </Card>

                </Col>
            </Row>
        </Container>
    );
}

export default Login;