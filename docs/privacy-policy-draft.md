# DIBA Bookings Privacy Notice — Draft for Review

> **Do not publish this draft as-is.** The organisation operating DIBA Bookings must complete every bracketed field, confirm actual data flows, and obtain appropriate privacy/legal review before using the system with real people.

**Last updated:** [insert date]  
**Responsible organisation:** [insert full legal name and address]  
**Information Officer / privacy contact:** [insert name or role and monitored email address]  
**Privacy enquiries:** [insert contact details]

## 1. Information we collect

Depending on the features used, DIBA Bookings may process:

- Account details: first name, last name, email address, password hash, assigned role, and account status.
- Event and booking details: event title/type, attendance information, venue, dates/times, special requirements, acknowledgement of booking requirements, and staff decisions/reasons.
- Payment and refund references: amount, status, provider checkout/refund identifiers, timestamps, and refund outcome. DIBA should not store card numbers or card security codes.
- Service records: in-app notifications, audit events, email-delivery status/error metadata, and privacy requests submitted by users.
- Technical/operational data: security and application logs needed to protect and operate the service.

Do not put unnecessary sensitive personal information in event descriptions or special-requirement fields.

## 2. Why we use information

The organisation must confirm the lawful basis and purpose for each activity. Intended purposes include creating and managing user accounts, processing venue bookings, communicating booking decisions, recording acknowledgement of booking requirements, processing/reconciling payments and refunds, responding to privacy requests, preventing misuse, and maintaining security/audit records.

## 3. Who may receive information

Access should be limited by role and business need. Information may be processed by authorised staff and service providers needed to operate the system, including the hosting/database provider, email provider, and Yoco for checkout/payment/refund processing. Confirm the actual providers, their roles, and any international data transfers before publication.

## 4. Security

DIBA Bookings uses password hashing, authenticated API access, role-based controls, and operational audit records. These measures reduce risk but cannot guarantee absolute security. The organisation must also maintain secure hosting, secret management, backups, access reviews, patching, monitoring, and incident-response procedures.

## 5. Retention

**Retention periods are not yet approved.** The responsible organisation must set and publish retention periods for accounts, booking records, payment/refund records, privacy requests, audit logs, and email-delivery logs. Records must not be retained indefinitely by default or deleted where a legal/financial obligation requires retention. Define a secure deletion or de-identification process after the approved period.

## 6. Your choices and privacy rights

Subject to applicable law and legitimate retention obligations, users may request access to their information, correction, deletion, objection/restriction where applicable, or further information about processing. The application provides a privacy-request workflow; submitting a request does not automatically delete data. Requests are reviewed by an authorised administrator and the responsible organisation must respond within applicable legal timeframes.

Submit a request using [insert in-app navigation or contact process]. Insert any verified escalation/contact process here.

## 7. Browser storage and security

The current frontend stores a bearer token in browser storage. Browser storage can be exposed if malicious script runs on the site. Before production, review this design and consider a secure, HttpOnly, SameSite cookie or another suitable token-handling architecture, alongside strong cross-site scripting prevention.

## 8. Changes and contact

This notice may be updated when processing changes. The organisation must publish the effective date and provide a way for users to contact the privacy team.

**Required sign-off before publication:** responsible organisation, Information Officer/contact, lawful basis/purpose review, service-provider list, cross-border-transfer review, retention schedule, rights-request procedure, security incident procedure, and final legal review.
