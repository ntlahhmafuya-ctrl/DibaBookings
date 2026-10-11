import { useCallback, useEffect, useState } from "react";
import { Alert, Badge, Button, Container, Spinner, Table } from "react-bootstrap";
import { Link, useNavigate } from "react-router-dom";
import { toast } from "react-toastify";
import { getPayments, retryFailedRefund } from "../services/paymentService";

function errorText(error, fallback) {
    const data = error.response?.data;
    if (typeof data === "string") return data;
    if (data?.message) return data.message;
    if (data?.title) return data.title;
    if (data?.errors) return Object.values(data.errors).flat().join(" ");
    return fallback;
}

function formatDate(value) {
    if (!value) return "—";
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? "—" : date.toLocaleString();
}

function statusVariant(value) {
    const status = String(value || "").toLowerCase();
    if (["succeeded", "paid", "resolved"].includes(status)) return "success";
    if (["failed", "rejected", "noteligible"].includes(status)) return "danger";
    if (["needsreview", "need more information"].includes(status)) return "warning";
    if (["pending", "submitted"].includes(status)) return "info";
    return "secondary";
}

function Status({ value }) {
    return <Badge bg={statusVariant(value)}>{value || "Not recorded"}</Badge>;
}

/**
 * PAYMENT OPERATIONS
 * Staff/Administrators can review API payment and refund states. A refund can only
 * be retried when the backend says Yoco confirmed the previous refund failed.
 * NeedsReview is deliberately not retryable from this screen.
 */
function PaymentManagement() {
    const navigate = useNavigate();
    const role = localStorage.getItem("role");
    const canReviewPayments = role === "Administrator" || role === "Staff";
    const [payments, setPayments] = useState([]);
    const [loading, setLoading] = useState(true);
    const [errorMessage, setErrorMessage] = useState("");
    const [retryingId, setRetryingId] = useState("");

    const loadPayments = useCallback(async () => {
        setLoading(true);
        setErrorMessage("");
        try {
            const response = await getPayments();
            setPayments(Array.isArray(response.data) ? response.data : []);
        } catch (error) {
            if (error.response?.status === 401) {
                setErrorMessage("Your session has expired. Please sign in again.");
            } else if (error.response?.status === 403) {
                setErrorMessage("Your account is not permitted to view payment records.");
            } else {
                setErrorMessage(errorText(error, "Could not load payment records."));
            }
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        if (!localStorage.getItem("token")) {
            navigate("/login", { replace: true });
            return;
        }
        if (canReviewPayments) loadPayments();
        else setLoading(false);
    }, [canReviewPayments, loadPayments, navigate]);

    const handleRetry = async (payment) => {
        if (!window.confirm(
            "Retry this refund only if Yoco's dashboard confirms the previous refund failed. Continue?"
        )) return;

        setRetryingId(payment.paymentId);
        setErrorMessage("");
        try {
            const response = await retryFailedRefund(payment.paymentId);
            const message = response.data?.message || "Refund retry request submitted.";
            toast.success(message);
            await loadPayments();
        } catch (error) {
            setErrorMessage(errorText(
                error,
                "The refund could not be retried. Check the payment record and Yoco before trying again."
            ));
        } finally {
            setRetryingId("");
        }
    };

    if (!canReviewPayments) {
        return (
            <Container className="py-5">
                <Alert variant="warning">Payment records are available to Staff and Administrators only.</Alert>
                <Link className="btn btn-outline-primary" to="/dashboard">Back to dashboard</Link>
            </Container>
        );
    }

    return (
        <Container fluid className="py-4 px-3 px-lg-5">
            <div className="d-flex align-items-start justify-content-between gap-3 flex-wrap mb-4">
                <div>
                    <p className="text-uppercase text-secondary small mb-1">Finance operations</p>
                    <h1 className="h2 mb-2">Payments and refunds</h1>
                    <p className="text-secondary mb-0">
                        Review provider outcomes. A refund marked “NeedsReview” must be reconciled with Yoco before any retry.
                    </p>
                </div>
                <div className="d-flex gap-2">
                    <Button variant="outline-primary" onClick={loadPayments} disabled={loading}>
                        Refresh records
                    </Button>
                    <Link className="btn btn-outline-secondary" to="/dashboard">Back to dashboard</Link>
                </div>
            </div>

            {errorMessage && <Alert variant="danger" role="alert">{errorMessage}</Alert>}

            {loading ? (
                <div className="py-5 text-center">
                    <Spinner animation="border" role="status" />
                    <p className="mt-2 mb-0">Loading payment records…</p>
                </div>
            ) : (
                <div className="bg-white border rounded p-2 p-md-3">
                    <Table responsive hover className="align-middle mb-0">
                        <thead>
                            <tr>
                                <th>Payment / reference</th>
                                <th>Booking</th>
                                <th>Amount</th>
                                <th>Payment status</th>
                                <th>Refund amount</th>
                                <th>Refund status</th>
                                <th>Refund details</th>
                                <th>Action</th>
                            </tr>
                        </thead>
                        <tbody>
                            {payments.map((payment) => {
                                const canRetry = String(payment.paymentStatus).toLowerCase() === "succeeded"
                                    && Number(payment.refundAmount) > 0
                                    && String(payment.refundStatus).toLowerCase() === "failed";
                                const needsReview = String(payment.refundStatus).toLowerCase() === "needsreview";
                                return (
                                    <tr key={payment.paymentId}>
                                        <td>
                                            <div className="fw-semibold">{payment.referenceNumber || payment.paymentId}</div>
                                            <small className="text-secondary">{formatDate(payment.paymentDate)}</small>
                                        </td>
                                        <td><code>{payment.bookingId}</code></td>
                                        <td>R{Number(payment.amount || 0).toFixed(2)}</td>
                                        <td><Status value={payment.paymentStatus} /></td>
                                        <td>{payment.refundAmount == null ? "—" : `R${Number(payment.refundAmount).toFixed(2)}`}</td>
                                        <td><Status value={payment.refundStatus} /></td>
                                        <td>
                                            {payment.refundReason && <div>{payment.refundReason}</div>}
                                            {payment.refundFailureReason && <small className="text-danger d-block">{payment.refundFailureReason}</small>}
                                            {payment.refundRequestedAtUtc && <small className="text-secondary d-block">Requested: {formatDate(payment.refundRequestedAtUtc)}</small>}
                                            {payment.refundProcessedAtUtc && <small className="text-secondary d-block">Processed: {formatDate(payment.refundProcessedAtUtc)}</small>}
                                            {payment.yocoRefundId && <small className="text-secondary d-block">Yoco refund: {payment.yocoRefundId}</small>}
                                            {needsReview && <small className="text-warning-emphasis d-block fw-semibold">Check Yoco first; do not retry automatically.</small>}
                                        </td>
                                        <td>
                                            {canRetry ? (
                                                <Button
                                                    size="sm"
                                                    variant="outline-danger"
                                                    disabled={retryingId === payment.paymentId}
                                                    onClick={() => handleRetry(payment)}
                                                >
                                                    {retryingId === payment.paymentId ? "Retrying…" : "Retry failed refund"}
                                                </Button>
                                            ) : "—"}
                                        </td>
                                    </tr>
                                );
                            })}
                            {payments.length === 0 && (
                                <tr><td colSpan={8} className="text-center text-secondary py-4">No payment records were found.</td></tr>
                            )}
                        </tbody>
                    </Table>
                </div>
            )}
        </Container>
    );
}

export default PaymentManagement;
