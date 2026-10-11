# DIBA Bookings — Payment Test Notes

This document describes the automated tests around the Yoco HTTP service and the remaining payment tests that must be performed before a live release.

## Run the automated backend tests

From the repository root:

```bash
dotnet restore Backend/DIBA_Backend/DIBA_Backend.Tests/DIBA_Backend.Tests.csproj
dotnet test Backend/DIBA_Backend/DIBA_Backend.Tests/DIBA_Backend.Tests.csproj --configuration Release
```

The test project targets .NET 8 and uses xUnit. `YocoPaymentServiceTests` supplies a fake `HttpMessageHandler`; it captures outbound HTTP requests and returns controlled responses. It does not contact Yoco and does not move money.

## Coverage in YocoPaymentServiceTests

| Scenario | Expected result | What it proves |
|---|---|---|
| Checkout called twice with the same key | Both requests contain the supplied key | DIBA forwards the same idempotency key; it does not prove Yoco deduplicates it |
| Blank checkout idempotency key | Argument exception; no HTTP request | Invalid retry key is rejected locally |
| Checkout provider responds 400, 401, 500, or 503 | HTTP exception | Non-success responses are not treated as successful checkout creation |
| Simulated checkout timeout | Timeout exception propagates from the service | The service does not falsely report success after a timeout |
| Malformed checkout JSON | JSON exception | Invalid provider response cannot be deserialized as a checkout |
| Refund request | Key and refund amount in cents are sent; response is parsed | Outbound refund request shape is correct for the expected contract |
| Missing/blank refund checkout ID | Argument exception; no HTTP request | Refund cannot be sent without a checkout identifier |
| Missing/blank refund idempotency key | Argument exception; no HTTP request | Refund retry must have an idempotency key |
| Zero/negative refund amount | Argument-out-of-range exception; no HTTP request | Invalid refund amount is rejected locally |

The test response uses a simulated refund payload with an `id` property. The controller now stores either the provider's `refundId` or `id` value, so a valid returned identifier is not silently discarded when the provider uses the latter form.

## Existing backend security/workflow tests

The backend test project also contains tests for password-change token invalidation, account deactivation, login rate limiting, booking overlap rejection, and blocking another checkout when an active payment exists. Review the current CI run for the exact result; do not treat a queued, cancelled, or still-running job as a pass.

## Important limitations

The automated tests above are service-level tests with a fake HTTP response. They do **not** establish that:

- Yoco's current sandbox API accepts the exact request fields or supports idempotency in the expected way.
- Simultaneous requests cannot create duplicate checkout records.
- A local SQL Server save failure after provider-side checkout creation is recovered safely.
- A timeout means the provider did or did not create a checkout/refund.
- The webhook signature, replay protection, event ordering, or reconciliation works end to end.
- SQL Server transaction isolation prevents concurrent overlapping bookings.
- A refund policy is legally or operationally approved.

## Manual/sandbox scenarios still required

Use Yoco test credentials and a non-production database. Never use live credentials or real customer payments for these checks.

1. **Duplicate checkout:** submit the same booking payment request twice concurrently. Inspect the provider and database for duplicate checkouts and payment rows.
2. **Provider timeout:** simulate or induce a timeout after Yoco may have accepted the request. Check Yoco's records before retrying. Confirm a retry for the same attempt uses the same key.
3. **Local save failure:** cause the payment-row save to fail after a checkout is created in the sandbox. Retry and verify the provider returns/reuses the same checkout for that key; verify DIBA can reconcile and persist it.
4. **Confirmed failed attempt:** deliver/observe a provider-confirmed failure, then retry. Confirm the new attempt gets a different key and reference.
5. **Duplicate and delayed webhook:** deliver the same signed event more than once and deliver events out of order. Confirm status remains correct and records are not duplicated or incorrectly downgraded.
6. **Invalid webhook:** test an invalid signature, stale timestamp, wrong amount/currency/reference, and replayed event. Confirm it is rejected safely.
7. **Refund retry:** test a refund confirmed failed by Yoco, a pending refund, and an ambiguous timeout. Confirm only a confirmed failure can be retried and an ambiguous outcome becomes `NeedsReview` until reconciled.
8. **SQL Server concurrency:** run overlapping booking requests and duplicate payment requests simultaneously against SQL Server, not EF Core InMemory.

Record the date, environment, test data IDs, expected result, actual result, and evidence for each sandbox scenario. Redact secrets, personal information, and payment credentials from logs/screenshots.

**Release rule:** do not enable live payments until the sandbox cases and production-readiness checklist are reviewed and signed off.
