# DIBA Bookings — Production Readiness

This checklist records deployment requirements. A green build is necessary but does not prove the application is safe or ready for live bookings.

## 1. Secrets and configuration

Configure these in the deployment platform's secret/environment settings, not in source control:

- `ConnectionStrings__DbConn`: production SQL Server connection string using TLS and a least-privilege database account.
- `Jwt__Key`: random signing secret of at least 32 UTF-8 bytes.
- `Jwt__Issuer` and `Jwt__Audience`: exact values expected by the deployed API.
- `Yoco__SecretKey` and `Yoco__WebhookSecret`: live credentials from the correct Yoco environment.
- `Yoco__SuccessUrl` and `Yoco__CancelUrl`: HTTPS URLs on the deployed frontend.
- `Cors__AllowedOrigins__0`: deployed frontend origin, e.g. `https://your-real-domain.example`.
- `Email__Enabled=true`, `Email__SmtpHost`, `Email__Port`, `Email__EnableSsl=true`, `Email__FromAddress`, and (if required by the provider) `Email__Username` / `Email__Password`.

Do not deploy with localhost URLs, demo credentials, a missing JWT key, or wildcard CORS. Email is disabled by default until a real provider is configured.

## 2. Database release procedure

1. Back up the production database and test restoring the backup.
2. Review pending EF Core migrations and confirm existing data will be preserved.
3. Check for duplicate user email addresses before applying the unique-email migration.
4. Check for more than one non-failed payment per booking before applying the active-payment constraint.
5. Apply migrations as a controlled deployment step from the backend project directory:
   `dotnet ef database update --project Backend/DIBA_Backend/DIBA_Backend/DIBA_Backend.csproj`
6. Verify migration history, reference roles/statuses, and venue data. Demo users and sample bookings must never be seeded into production.
7. Deploy the API and verify `/health`, login, registration, booking operations, and payment webhooks in the correct environment.

The application seeds demo users and sample venue/event data only in Development. Production database migrations are not automatically run at API startup.

## 3. Payments and refunds

- A browser redirect to a success page is not proof of payment. Only a signature-verified Yoco webhook can change payment status.
- Verify the webhook URL, secret, timestamp tolerance, duplicate delivery handling, amount/currency/reference checks, and replay handling with Yoco's test environment before enabling live payments.
- Reconcile failed or uncertain refund requests with Yoco before retrying. Do not retry a refund marked `NeedsReview` without checking Yoco's records.
- Current cancellation logic is a proposed policy: organiser cancellation 7 or more days before the event = full refund; 72 hours to less than 7 days = 50%; less than 72 hours = no refund; DIBA-initiated cancellation = full refund. The responsible organisation must formally approve this policy and display it to users before production.
- Test payment failures, delayed/duplicate webhooks, webhook signature failures, duplicate checkout attempts, partial refunds, and Yoco timeouts.

## 4. Privacy, email, and operational controls

- Complete and approve `docs/privacy-policy-draft.md` before collecting real personal information. Replace every placeholder and confirm the responsible legal entity, Information Officer/contact, purposes, lawful basis, recipients, retention periods, cross-border processing, and user-rights procedure.
- Configure a real SMTP provider and verify delivery, rejected credentials, timeouts, bounce handling, and access to delivery logs. Do not assume an in-app notification means an email was delivered.
- Agree and document retention periods for bookings, payments, audit logs, privacy requests, and email-delivery logs. Do not delete financial/audit records automatically without an approved retention rule.
- Restrict production database access, enforce TLS, back up data, monitor application/database failures, rotate credentials, and review access to personal and payment data.
- Keep API Swagger disabled outside Development unless it is explicitly secured for an operational need.

## 5. Release verification

- CI must pass backend restore/build and frontend dependency installation/build.
- Run functional and authorization tests for every role, including attempts to access another organiser's booking, payment, profile, and notifications.
- Run concurrency tests for overlapping bookings and duplicate payment requests against SQL Server.
- Complete an end-to-end test using Yoco test credentials and the real webhook endpoint.
- Record the release commit, migration version, test results, rollback procedure, and release approver.

**Status:** This checklist is not a certification. Live launch remains blocked until configuration, migrations, provider tests, privacy/legal decisions, and end-to-end checks are completed.
