# DIBA Bookings: Brevo email setup

DIBA Bookings uses Brevo's transactional email API. Email delivery remains disabled until a sender and API key are configured.

## 1. Create and verify the sender

1. Sign in to Brevo and open **Settings → Senders, Domains, IPs → Senders**.
2. Add a sender with the display name **DIBA Bookings**.
3. Use an email address you control and can verify.
4. Follow Brevo's verification instructions.

A domain you own (for example, `bookings@yourdomain.co.za`) is the best long-term sender. Brevo may allow a verified personal address for initial testing, but a free mailbox domain cannot be authenticated as a sending domain. For production, use a domain you control and complete Brevo's domain authentication (including the DNS records it provides). Do not use the SMTP login address as the sender.

## 2. Create an API key

1. In Brevo, open **SMTP & API** / **API Keys**.
2. Create an API key for this application.
3. Store it as a secret. Never commit it to Git, put it in `appsettings.json`, or paste it into an issue or pull request.

## 3. Local development

From the backend project directory, use .NET User Secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "Email:Brevo:ApiKey" "PASTE_YOUR_BREVO_API_KEY_HERE"
dotnet user-secrets set "Email:FromAddress" "YOUR_VERIFIED_SENDER_ADDRESS"
dotnet user-secrets set "Email:FromName" "DIBA Bookings"
dotnet user-secrets set "Email:Provider" "Brevo"
dotnet user-secrets set "Email:Enabled" "true"
```

Run these commands in the directory containing `DIBA_Backend.csproj`, or add `--project path\to\DIBA_Backend.csproj` to each command. Replace the placeholders locally; do not commit the real values.

## 4. Deployment environment variables

Set these as private environment variables in the backend hosting provider:

- `Email__Enabled=true`
- `Email__Provider=Brevo`
- `Email__FromName=DIBA Bookings`
- `Email__FromAddress=YOUR_VERIFIED_SENDER_ADDRESS`
- `Email__Brevo__ApiKey=YOUR_BREVO_API_KEY`

Keep the key in the host's secret/environment-variable settings, not in source control. Restart/redeploy the backend after changing the values.

## 5. Verify delivery safely

1. Leave email disabled until sender verification and secrets are configured.
2. Enable it in a development/test deployment first.
3. Trigger one test notification to an inbox you control.
4. Confirm the message arrives and check `EmailDeliveryLogs` for the delivery attempt.
5. Review Brevo's transactional logs if the API accepts a message but it does not arrive. An API acceptance is not a guarantee that the recipient inbox delivered it.

The service uses Brevo's `POST https://api.brevo.com/v3/smtp/email` endpoint and sends plain-text message bodies. It does not log the API key. A successful API response is recorded as `Sent`; this means accepted by Brevo, not independently confirmed as delivered.

## Official references

- [Send a transactional email](https://developers.brevo.com/docs/send-a-transactional-email)
- [Senders and domains](https://developers.brevo.com/docs/getting-started-with-senders-and-domains)
- [Domain setup for better deliverability](https://help.brevo.com/hc/en-us/articles/35852083084178-Domain-setup-for-better-email-deliverability)
