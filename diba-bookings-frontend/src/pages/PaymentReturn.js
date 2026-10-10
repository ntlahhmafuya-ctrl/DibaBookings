import { useEffect, useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { getPayment } from "../services/paymentService";

const MAX_STATUS_CHECKS = 20;
const STATUS_CHECK_INTERVAL_MS = 3000;

function PaymentReturn() {
    const location = useLocation();
    const paymentId = new URLSearchParams(location.search).get("paymentId");
    const isCancelled = location.pathname === "/payment/cancel";

    const [payment, setPayment] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [timedOut, setTimedOut] = useState(false);

    useEffect(() => {
        if (!paymentId) {
            setError("We could not find a payment reference in this return link.");
            setLoading(false);
            return undefined;
        }

        let active = true;
        let timeoutId;
        let attempts = 0;

        const checkStatus = async () => {
            try {
                const response = await getPayment(paymentId);
                if (!active) return;

                const currentPayment = response.data;
                setPayment(currentPayment);
                setError("");
                setLoading(false);

                const status = String(currentPayment.paymentStatus || "").toLowerCase();
                if (status === "succeeded" || status === "failed") {
                    return;
                }

                attempts += 1;
                if (attempts >= MAX_STATUS_CHECKS) {
                    setTimedOut(true);
                    return;
                }

                timeoutId = window.setTimeout(
                    checkStatus,
                    STATUS_CHECK_INTERVAL_MS
                );
            } catch (requestError) {
                if (!active) return;

                setLoading(false);
                setError(
                    requestError.response?.status === 401
                        ? "Your session has expired. Sign in again, then reopen this payment return page."
                        : requestError.response?.status === 403
                            ? "You are not allowed to view this payment."
                            : requestError.response?.data?.message ||
                              "We could not retrieve the payment status. Please try again."
                );
            }
        };

        checkStatus();

        return () => {
            active = false;
            if (timeoutId) window.clearTimeout(timeoutId);
        };
    }, [paymentId]);

    const status = String(payment?.paymentStatus || "").toLowerCase();
    const succeeded = status === "succeeded";
    const failed = status === "failed";

    let heading = "Checking your payment";
    let message = "We are checking the status recorded by DIBA. This can take a short time.";

    if (succeeded) {
        heading = "Payment confirmed";
        message = "Yoco's verified payment notification has confirmed that your payment succeeded.";
    } else if (failed) {
        heading = "Payment not completed";
        message = "DIBA received confirmation that this payment failed. You can return to your dashboard for assistance.";
    } else if (isCancelled) {
        heading = "You returned from checkout";
        message = "The checkout was cancelled or closed. We are checking the recorded payment status before drawing a conclusion.";
    } else if (timedOut) {
        heading = "Payment confirmation is taking longer";
        message = "The payment is still awaiting confirmation. Do not pay again yet; refresh this page shortly or contact DIBA staff.";
    }

    return (
        <main
            style={{
                minHeight: "100vh",
                display: "grid",
                placeItems: "center",
                padding: "24px",
                background: "#f4f7fa",
                color: "#102b3f"
            }}
        >
            <section
                style={{
                    width: "100%",
                    maxWidth: "560px",
                    background: "#ffffff",
                    padding: "32px",
                    borderRadius: "16px",
                    boxShadow: "0 8px 28px rgba(6, 28, 44, 0.10)"
                }}
            >
                <p style={{ marginTop: 0, color: "#527187", fontWeight: 700 }}>
                    DIBA BOOKINGS
                </p>
                <h1 style={{ marginBottom: "12px" }}>{heading}</h1>
                <p>{message}</p>

                {loading && <p role="status">Loading payment details…</p>}

                {payment && (
                    <dl style={{ margin: "24px 0" }}>
                        <dt style={{ fontWeight: 700 }}>Payment reference</dt>
                        <dd style={{ margin: "4px 0 16px" }}>
                            {payment.referenceNumber || payment.paymentId}
                        </dd>
                        <dt style={{ fontWeight: 700 }}>Amount</dt>
                        <dd style={{ margin: "4px 0 16px" }}>
                            {new Intl.NumberFormat("en-ZA", {
                                style: "currency",
                                currency: "ZAR"
                            }).format(Number(payment.amount || 0))}
                        </dd>
                        <dt style={{ fontWeight: 700 }}>Recorded status</dt>
                        <dd style={{ margin: "4px 0 0", fontWeight: 700 }}>
                            {payment.paymentStatus || "Pending"}
                        </dd>
                    </dl>
                )}

                {error && (
                    <p role="alert" style={{ color: "#a52828" }}>
                        {error}
                    </p>
                )}

                {!succeeded && !failed && !error && !timedOut && (
                    <p role="status">We will check again automatically while the payment is pending.</p>
                )}

                <Link
                    to="/dashboard"
                    style={{
                        display: "inline-block",
                        marginTop: "16px",
                        padding: "12px 18px",
                        borderRadius: "8px",
                        background: "#103a5c",
                        color: "#ffffff",
                        textDecoration: "none",
                        fontWeight: 700
                    }}
                >
                    Return to dashboard
                </Link>
            </section>
        </main>
    );
}

export default PaymentReturn;
