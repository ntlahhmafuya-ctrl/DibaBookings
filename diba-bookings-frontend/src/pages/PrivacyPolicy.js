import { useState } from "react";
import { Link } from "react-router-dom";
import api from "../services/api";

/**
 * PRIVACY NOTICE PAGE
 * Responsibility: explain personal-information practices and provide self-service data export.
 */
function PrivacyPolicy() {
    const [exporting, setExporting] = useState(false);
    const [exportMessage, setExportMessage] = useState("");

    // DATA EXPORT: request the signed-in user's data and download the API response as a JSON file.
    const downloadMyData = async () => {
        if (!localStorage.getItem("token")) {
            setExportMessage("Please sign in first, then return to this page to download your data.");
            return;
        }

        setExporting(true);
        setExportMessage("");

        try {
            const response = await api.get("/Users/me/export");
            const fileContents = JSON.stringify(response.data, null, 2);
            const file = new Blob([fileContents], { type: "application/json" });
            const downloadUrl = window.URL.createObjectURL(file);
            const link = document.createElement("a");

            link.href = downloadUrl;
            link.download = "diba-bookings-personal-data.json";
            document.body.appendChild(link);
            link.click();
            link.remove();
            window.URL.revokeObjectURL(downloadUrl);

            setExportMessage("Your data file has been prepared. Keep it somewhere private and secure.");
        } catch (error) {
            setExportMessage(
                error.response?.status === 401
                    ? "Your session has expired. Please sign in again and retry."
                    : "We could not prepare your data file. Please try again or contact Conference Centre management."
            );
        } finally {
            setExporting(false);
        }
    };
    return (
        <main className="privacy-page">
            <header className="privacy-header">
                <Link className="privacy-brand" to="/">
                    <span className="privacy-brand-mark">D</span>
                    <span>DIBA <strong>Bookings</strong></span>
                </Link>
                <Link className="privacy-back-link" to="/">
                    Back to home
                </Link>
            </header>

            <article className="privacy-document">
                <p className="privacy-eyebrow">DIBA BOOKINGS / YOUR INFORMATION</p>
                <h1>Privacy Notice</h1>
                <p className="privacy-lead">
                    We want you to understand what personal information DIBA
                    Bookings uses, why it is needed, and what choices and rights
                    you have under South Africa's Protection of Personal
                    Information Act (POPIA).
                </p>
                <p className="privacy-updated">Last updated: 10 October 2026</p>

                <div className="privacy-important">
                    <strong>Before public launch</strong>
                    <p>
                        The institution or organisation that is legally
                        responsible for this service, its Information Officer,
                        contact details, and approved retention periods must be
                        confirmed by the Conference Centre management. This
                        notice must be reviewed and approved by that responsible
                        party before DIBA Bookings is used with real users.
                    </p>
                </div>

                <section>
                    <h2>1. Who is responsible for your information?</h2>
                    <p>
                        The organisation operating the DIBA Bookings service
                        must act as the responsible party for personal
                        information processed through the platform. The
                        responsible organisation and its Information Officer
                        contact details are still to be confirmed. We will
                        publish those details here before public launch.
                    </p>
                </section>

                <section>
                    <h2>2. What information may we collect?</h2>
                    <ul>
                        <li>
                            <strong>Account details:</strong> your name, surname,
                            email address, and account credentials.
                        </li>
                        <li>
                            <strong>Event and booking details:</strong> event
                            name and description, event type, expected
                            attendance, selected venue, booking dates and times,
                            and any requirements you submit.
                        </li>
                        <li>
                            <strong>Booking administration:</strong> booking
                            status, acknowledgement of booking responsibilities,
                            and relevant decisions or notes made by authorised
                            staff.
                        </li>
                        <li>
                            <strong>Platform activity:</strong> information
                            needed to operate your account, show notifications,
                            protect the service, and troubleshoot errors.
                        </li>
                    </ul>
                    <p>
                        Please do not enter sensitive personal information
                        unless the platform specifically requests it and the
                        responsible organisation has confirmed that it is
                        necessary.
                    </p>
                </section>

                <section>
                    <h2>3. Why do we use it?</h2>
                    <p>Personal information may be used to:</p>
                    <ul>
                        <li>create and manage your account;</li>
                        <li>process event and venue booking requests;</li>
                        <li>check venue availability and prevent double bookings;</li>
                        <li>allow authorised staff to review, approve, or reject requests;</li>
                        <li>show booking statuses and relevant notifications;</li>
                        <li>provide support, maintain security, and investigate misuse; and</li>
                        <li>meet applicable legal and record-keeping obligations.</li>
                    </ul>
                    <p>
                        The responsible organisation must identify and document
                        the lawful basis and purpose for each use of personal
                        information. Information must not be reused for an
                        unrelated purpose without a lawful basis.
                    </p>
                </section>

                <section>
                    <h2>4. Who can access your information?</h2>
                    <p>
                        Access should be limited to people who need the
                        information for their duties. Depending on their role,
                        this may include authorised Conference Centre staff,
                        administrators, and the event organiser who submitted a
                        booking. Technical service providers may process
                        information where required to host, operate, or maintain
                        the platform, subject to appropriate safeguards.
                    </p>
                    <p>
                        The service operator must confirm its actual hosting,
                        database, email, and payment providers, including where
                        information is stored or accessed, before launch.
                        Information must not be sold to advertisers.
                    </p>
                </section>

                <section>
                    <h2>5. How long do we keep it?</h2>
                    <p>
                        Information should be kept only for as long as needed
                        for the stated purpose, applicable legal obligations,
                        and approved institutional record-keeping requirements.
                        A documented retention and secure-deletion schedule has
                        not yet been confirmed. The responsible organisation
                        must approve that schedule before the service is used
                        with real personal information.
                    </p>
                </section>

                <section>
                    <h2>6. How do we protect it?</h2>
                    <p>
                        The platform should use role-based access, secure
                        authentication, restricted administrative access,
                        protected communications, backups, and procedures for
                        handling security incidents. No online system can
                        promise absolute security. The operator must test these
                        safeguards and ensure that service providers have
                        appropriate security and confidentiality measures in
                        place.
                    </p>
                </section>

                <section className="privacy-data-tools">
                    <h2>Access a copy of your DIBA Bookings data</h2>
                    <p>
                        If you are signed in, you can download a copy of the
                        account details, events, bookings, payment summaries,
                        and notifications currently linked to your account.
                        The download is a JSON file that you can keep or provide
                        when making a privacy request. It does not include your
                        password hash or payment-provider credentials.
                    </p>
                    <button
                        type="button"
                        className="privacy-download-button"
                        onClick={downloadMyData}
                        disabled={exporting}
                    >
                        {exporting ? "Preparing your data…" : "Download my data"}
                    </button>
                    {exportMessage && (
                        <p className="privacy-export-message" role="status">
                            {exportMessage}
                        </p>
                    )}
                </section>

                <section>
                    <h2>7. Your rights and requests</h2>
                    <p>
                        Subject to applicable law, you may ask to access your
                        personal information, correct information that is
                        inaccurate or incomplete, object to certain processing,
                        or request deletion where the law permits. You may also
                        raise a concern about how your information is handled.
                        Some records may need to be retained where there is a
                        lawful reason to do so.
                    </p>
                    <p>
                        Until the responsible organisation publishes its
                        verified privacy contact, please raise requests with
                        the Conference Centre management through its existing
                        official contact channel. Do not send passwords or
                        authentication tokens in a request.
                    </p>
                    <p>
                        You can also find information about your rights and
                        complaints through the{" "}
                        <a
                            href="https://inforegulator.org.za/"
                            target="_blank"
                            rel="noreferrer"
                        >
                            Information Regulator of South Africa
                        </a>.
                    </p>
                </section>

                <section>
                    <h2>8. Browser storage and cookies</h2>
                    <p>
                        The application may store essential sign-in information
                        in your browser so that account features can work.
                        Avoid signing in on a shared or public computer, and
                        close your session when you finish. Any optional
                        analytics, advertising, or non-essential tracking must
                        be assessed separately and should not be enabled
                        without the appropriate transparency and permissions.
                    </p>
                </section>

                <section>
                    <h2>9. Changes to this notice</h2>
                    <p>
                        This notice may be updated when the service or its
                        information practices change. Important changes should
                        be communicated clearly, and the latest version and
                        update date should remain available on this page.
                    </p>
                </section>

                <section>
                    <h2>10. Questions or concerns</h2>
                    <p>
                        The official privacy contact and Information Officer
                        details will be added once confirmed by the
                        organisation responsible for DIBA Bookings. Until then,
                        this page is an initial privacy notice, not confirmation
                        that all POPIA compliance requirements have been met.
                    </p>
                </section>

                <div className="privacy-footer">
                    <Link to="/register">Create an account</Link>
                    <Link to="/">Return to DIBA Bookings</Link>
                </div>
            </article>
        </main>
    );
}

export default PrivacyPolicy;
