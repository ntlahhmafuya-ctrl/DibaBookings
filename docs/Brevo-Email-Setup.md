# DIBA Bookings — Brevo email setup

DIBA Bookings uses Brevo's transactional email API for booking and payment notifications. Email delivery stays disabled until the sender and API key are configured.

## 1. Create a Brevo account and API key

1. Create or sign in to a Brevo account: https://www.brevo.com/
2. In the Brevo dashboard, open **Settings → SMTP & API → API Keys** (labels may vary).
3. Create an API key for this application. Treat it like a password. Do not paste it into source code, appsettings files, screenshots, commits, or chat.
4. Review Brevo's sender requirements and verify the sender you intend to use.

Official documentation:
- Send transactional email: https://developers.brevo.com/docs/send-a-transactional-email
- Senders and domains: https://developers.brevo.com/docs/getting-started-with-senders-and-domains
- Domain authentication: https://developers.brevo.com/docs/domain-authentication-and-verification

## 2. Choose a sender address

**For initial testing at no extra domain cost:** try the personal Outlook/Hotmail address you already control, and complete Brevo's sender verification if the dashboard allows it. Set the sender name to `DIBA Bookings`. Do not invent an address such as `bookings@dibabookings.co.za` unless you own and can receive mail for that domain.

A personal Outlook/Hotmail address is not a domain you control, so you cannot add Brevo's SPF/DKIM DNS records for it. Brevo may limit sender verification or deliverability for free email domains. If Brevo will not accept it, do not try to bypass the restriction. Leave email disabled until you can use an address/domain that meets its requirements.

**Longer-term recommendation:** use a domain you own, for example `bookings@your-owned-domain.co.za`, and authenticate it in Brevo by adding the exact DNS records Brevo provides. This usually improves trust and deliverability, but owning a domain may cost money.

## 3. Configure secrets locally

From the folder containing `DIBA_Backend.csproj`, run in Visual Studio Package Manager Console or a terminal, adjusting the project path as needed:

```powershell
dotnet user-secrets init --project ".\Backend\DIBA_Backend\DIBA_Backend\DIBA_Backend.csproj"
dotnet user-secrets set "Email:Enabled" "true" --project ".\Backend\DIBA_Backend\DIBA_Backend.csproj"
dotnet user-secrets set "Email:Provider" "Brevo" --project ".\Backend\DIBA_Backend\DIBA_Backend.csproj"
dotnet user-secrets set "Email:Brevo:ApiKey" "PASTE_YOUR_BREVO_API_KEY_LOCALLY" --project ".\Backend\DIBA_Backend\DIBA_Backend.csproj"
dotnet user-secrets set "Email:FromAddress" "YOUR_VERIFIED_SENDER_ADDRESS" --project ".\Backend\DIBA_Backend\DIBA_Backend.csproj"
dotnet user-secrets set "Email:FromName" "DIBA Bookings" --project ".\Backend\DIBA_Backend\DIBA_Backend.csproj"
```

Replace the two placeholders on your own computer. Never commit actual secrets. If `user-secrets init` reports that a UserSecretsId already exists, skip that command and continue.

## 4. Configure deployment

In your hosting provider's private environment-variable settings, set:

- `Email__Enabled` = `true`
- `Email__Provider` = `Brevo`
- `Email__Brevo__ApiKey` = your Brevo API key
- `Email__FromAddress` = your verified sender email
- `Email__FromName` = `DIBA Bookings`

Do not add the API key to GitHub Actions logs or public build arguments. Keep `Email:Enabled` false until the sender is verified and a test message succeeds.

## 5. Test safely

1. Send a test notification only to an inbox you control.
2. Confirm the email arrives and check Spam/Junk.
3. Check `EmailDeliveryLogs` for `Sent` or `Failed`. `Sent` means Brevo accepted the API request, not necessarily that the recipient inbox delivered it.
4. Test booking approval, rejection, cancellation, and payment/refund notification paths.
5. Do not treat email delivery as proof of a booking or payment state; the database remains authoritative.

## Configuration behavior

`appsettings.json` intentionally contains an empty sender address and empty API-key placeholder. The provider defaults to Brevo, but `Email:Enabled` is false. Local User Secrets and private deployment environment variables override these safe defaults.
