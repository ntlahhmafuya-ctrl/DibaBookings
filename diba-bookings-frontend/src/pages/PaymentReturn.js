import { useEffect, useState } from "react";
import { Alert, Button, Container, Spinner } from "react-bootstrap";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { getBookingPayment } from "../services/paymentService";

function PaymentReturn({ cancelled = false }) {
    const [searchParams] = useSearchParams();
    const navigate = useNavigate();
    const bookingId = searchParams.get("bookingId");
    const [payment, setPayment] = useState(null);
    const [message, setMessage] = useState(
        cancelled
            ? "You returned from checkout before confirming payment. Your booking has not been cancelled."
            : "Waiting for DIBA to receive and verify payment confirmation from Yoco…"
    );
    const [checking, setChecking] = useState(!cancelled);
    const [timedOut, setTimedOut] = useState(false);
    const [errorMessage, setErrorMessage] = useState("");

    useEffect(() => {
        if (!bookingId && !cancelled) {
            setErrorMessage("The Yoco return link did not include a booking reference. Return to your bookings and ask staff to check the payment before trying again.");
            setChecking(false);
            return undefined;
        }

        if (cancelled || !bookingId || !localStorage.getItem("token")) {
            setChecking(false);
            return undefined;
        }

        let disposed = false;
        let attempts = 0;
        let intervalId;

        const checkPayment = async () => {
            attempts += 1;
            try {
                const response = await getBookingPayment(bookingId);
                if (disposed) return;
                const currentPayment = response.data;
                setPayment(currentPayment);
                setErrorMessage("");

                if (String(currentPayment.paymentStatus).toLowerCase() === "succeeded") {
                    setMessage("Payment confirmed successfully. Returning to your bookings…");
                    setChecking(false);
                    window.clearInterval(intervalId);
                    window.setTimeout(() => {
                        if (!disposed) {
                            navigate(
                                `/dashboard?view=bookings&paymentStatus=succeeded&bookingId=${encodeURIComponent(bookingId)}`,
                                { replace: true }
                            );
                        }
                    }, 1400);
                    return;
                }

                if (String(currentPayment.paymentStatus).toLowerCase() === "failed") {
                    setMessage("Yoco recorded this payment attempt as failed. You can return to your bookings and try again.");
                    setChecking(false);
                    window.clearInterval(intervalId);
                    return;
                }

                if (attempts >= 20) {
                    setTimedOut(true);
                    setChecking(false);
                    setMessage("Yoco returned you to DIBA, but the server has not confirmed the payment yet. Do not pay again while the status is pending.");
                    window.clearInterval(intervalId);
                }
            } catch (error) {
                if (disposed) return;
                if (error.response?.status === 401) {
                    setErrorMessage("Your session has expired. Sign in again to check your payment.");
                    setChecking(false);
                    window.clearInterval(intervalId);
                    return;
                }
                if (error.response?.status === 404) {
                    setErrorMessage("DIBA could not find a saved payment for this booking. Please contact Conference Centre staff before trying to pay again.");
                    setChecking(false);
                    window.clearInterval(intervalId);
                    return;
                }
                if (attempts >= 20) {
                    setTimedOut(true);
                    setChecking(false);
                    setErrorMessage("We could not confirm the payment status. Please do not submit another payment until staff have checked Yoco.");
                    window.clearInterval(intervalId);
                }
            }
        };

        checkPayment();
        intervalId = window.setInterval(checkPayment, 2500);

        return () => {
            disposed = true;
            window.clearInterval(intervalId);
        };
    }, [bookingId, cancelled, navigate]);

    const returnToBookings = () => {
        navigate(
            bookingId
                ? `/dashboard?view=bookings&bookingId=${encodeURIComponent(bookingId)}`
                : "/dashboard?view=bookings",
            { replace: true }
        );
    };

    if (!localStorage.getItem("token")) {
        return (
            <Container className="py-5" style={{ maxWidth: 680 }}>
                <Alert variant="warning">Sign in to view your booking and payment status.</Alert>
                <Button as={Link} to="/login">Sign in</Button>
            </Container>
        );
    }

    return (
        <Container className="py-5" style={{ maxWidth: 760 }}>
            <div className="border rounded-4 p-4 p-md-5 bg-white shadow-sm">
                <p className="text-uppercase text-secondary small mb-2">DIBA Bookings · Payment</p>
                <h1 className="h2 mb-3">
                    {cancelled ? "Checkout cancelled" :
                        String(payment?.paymentStatus).toLowerCase() === "succeeded"
                            ? "Payment successful"
                            : "Payment confirmation"}
                </h1>

                {!cancelled && checking && (
                    <div className="d-flex align-items-center gap-3 mb-3">
                        <Spinner animation="border" role="status" />
                        <span>Checking the payment status with DIBA…</span>
                    </div>
                )}

                <p>{message}</p>

                {payment && (
                    <dl className="row mt-4">
                        <dt className="col-sm-4">Booking reference</dt>
                        <dd className="col-sm-8"><code>{bookingId}</code></dd>
                        <dt className="col-sm-4">Amount</dt>
                        <dd className="col-sm-8">R{Number(payment.amount || 0).toFixed(2)}</dd>
                        <dt className="col-sm-4">Payment status</dt>
                        <dd className="col-sm-8">
                            <span className={String(payment.paymentStatus).toLowerCase() === "succeeded" ? "badge bg-success" : "badge bg-warning text-dark"}>
                                {payment.paymentStatus}
                            </span>
                        </dd>
                    </dl>
                )}

                {errorMessage && <Alert variant="warning" role="alert">{errorMessage}</Alert>}
                {timedOut && <Alert variant="warning">Do not start another payment while this attempt is still pending. Ask staff to check the Yoco transaction and webhook delivery first.</Alert>}

                {!checking && (
                    <div className="d-flex gap-2 flex-wrap mt-4">
                        <Button onClick={returnToBookings}>Return to my bookings</Button>
                        {!cancelled && (
                            <Button variant="outline-secondary" onClick={() => window.location.reload()}>
                                Check status again
                            </Button>
                        )}
                    </div>
                )}
            </div>
        </Container>
    );
}

export default PaymentReturn;
