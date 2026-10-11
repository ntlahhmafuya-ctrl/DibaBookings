# DIBA Bookings — Frontend/API Integration Checklist

This document maps the React frontend to the ASP.NET Core API on the `integration/diba-readiness` branch. A route existing in code does not, by itself, prove that the complete workflow has been tested against a running API and SQL Server.

## Frontend features mapped to backend routes

| Feature | Frontend entry point | Backend route(s) | Notes |
|---|---|---|---|
| Register and sign in | Login/Register pages and `authService.js` | `POST /api/Authentication/register`, `POST /api/Authentication/login` | JWT is attached by the shared Axios client. |
| Profile | `Profile.js` | `GET /api/Users/me`, `PUT /api/Users/me` | Profile identity is derived by the API from the token. |
| Change password | Profile security section | `POST /api/Account/change-password` | Frontend enforces the backend's 12–72 character new-password range and confirms the new password. |
| Export personal data | Privacy Notice page | `GET /api/Users/me/export` | Download is a JSON file. Users should keep it private. |
| Privacy requests | Privacy Notice page and administrator dashboard | `POST /api/PrivacyRequests`, `GET /api/PrivacyRequests/my`, administrator `GET /api/PrivacyRequests`, `PUT /api/PrivacyRequests/{id}` | Requests are tracked; deletion is not automatic. |
| Venue directory and facilities | Organiser and Staff dashboards | `GET/GET by id/POST/PUT /api/Venues`, `/api/Venues/{venueId}/features` | The API's role rules remain authoritative. |
| Events and acknowledgement | Organiser event form | `GET/POST /api/Events`, `POST /api/Events/with-booking`, `PUT /api/Events/{id}` | Booking acknowledgement is required before creating an event and booking together. |
| Booking workflow | Organiser and Staff dashboards | `GET/POST /api/Bookings`, `GET /api/Bookings/{id}`, availability routes, edit/cancel/approve/reject routes | Backend validation and conflict checks must be tested with the configured SQL Server. |
| Checkout | Organiser notification action | `POST /api/Payments` | Checkout is opened using the redirect URL returned by the API. The browser must never receive the Yoco secret key. |
| Payment/refund operations | Staff/Administrator `/payments` page | `GET /api/Payments`, `POST /api/Payments/{id}/refund/retry` | Only staff/admin can list all payments or retry a provider-confirmed failed refund. A `NeedsReview` refund must be reconciled with Yoco first. |
| Notifications | Organiser notification inbox | `GET /api/Notifications`, `PUT /api/Notifications/{id}/read`, `PUT /api/Notifications/read-all` | Inbox content is scoped to the authenticated user by the API. |
| Admin controls and audit | Administrator dashboard | `/api/Administration`, `/api/Users`, `/api/AuditLogs` | Access depends on the API's role restrictions. |
| Yoco webhooks and email | Backend services | `POST /api/Payments/yoco/webhook` plus notification/email service | Provider callbacks are server-to-server; they are not called directly from the browser. Verify in sandbox. |

## Required deployed Yoco configuration

Set these values in the **backend hosting environment**, not in React and not in a committed settings file:

- `Yoco__SuccessUrl=https://YOUR-FRONTEND-DOMAIN/payment/success`
- `Yoco__CancelUrl=https://YOUR-FRONTEND-DOMAIN/payment/cancel`
- `Yoco__WebhookSecret=whsec_...` using the signing secret for the same Yoco key/mode.
- Register this webhook URL in Yoco for the matching test/live mode: `https://YOUR-API-DOMAIN/api/Payments/yoco/webhook`.

The success/cancel URLs must point to the deployed React application, not the API host or a development-only localhost URL. The return page includes the booking ID and polls DIBA for the saved payment state; a redirect by itself is **not** proof that payment succeeded.

Yoco's Checkout API guidance says each payment must be confirmed via a registered webhook and that API calls must remain server-side. If a payment already succeeded in Yoco but DIBA still says Pending, do not pay again and do not attempt a cancellation/refund until the webhook has been delivered and DIBA has confirmed the result. Check the Yoco webhook delivery log and re-deliver the matching event if the dashboard supports it. A successful payment should change the saved payment to `Succeeded` and create the organiser's payment notification.

## Manual integration test checklist

Use a development/test database and Yoco sandbox credentials only.

- [ ] Sign in as Event Organiser; verify the token is attached to authenticated requests.
- [ ] Try a protected request without a token and verify the UI handles 401 without exposing raw server details.
- [ ] Create an event/booking without the acknowledgement and verify the frontend blocks it; also verify the API rejects a crafted request without acknowledgement.
- [ ] Create a valid booking, then try an overlapping booking and verify the API rejects the conflict.
- [ ] Approve a booking as Staff; verify the organiser receives an in-app notification and can launch checkout.
- [ ] Complete a Yoco sandbox payment; verify the webhook changes payment status and the organiser sees the resulting notification.
- [ ] Simulate failed checkout and timeout cases; verify a pending/uncertain payment is not treated as a confirmed failure.
- [ ] Cancel a paid booking in sandbox; verify the displayed notification matches the backend's refund result and staff can see refund metadata.
- [ ] Verify the Staff/Administrator payments page can list payments, refresh after a refund retry, and only shows the retry button for a refund marked `Failed`.
- [ ] Verify a refund marked `NeedsReview` cannot be retried in the UI and staff are told to reconcile it with Yoco first.
- [ ] Mark one notification read and then mark all notifications read; verify the unread count updates.
- [ ] Change the account password using a valid current password, then test an incorrect current password and a new password shorter than 12 characters.
- [ ] Export personal data and submit a privacy request; verify the requester can see their request status and an Administrator can respond.
- [ ] Verify role restrictions by attempting staff/admin screens as an Event Organiser.
- [ ] Run booking concurrency and payment/webhook tests against the actual configured SQL Server and Yoco sandbox. EF InMemory tests are not proof of SQL Server transaction behaviour.

## Release limitations

- A successful frontend build proves compilation, not end-to-end correctness.
- Refund policy wording and the proposed refund schedule must be formally approved by the responsible organisation before live use.
- Test checkout idempotency, duplicate/delayed webhooks, uncertain refund outcomes, and local database-save failures in Yoco sandbox before accepting real payments.
- Do not use production payment keys or production personal data for these tests.
