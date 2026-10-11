# DIBA Bookings: Microsoft Graph email setup

DIBA's notification email service can send through Microsoft Graph using OAuth 2.0 client credentials. The application does not need an Outlook password or SMTP basic authentication.

## Account requirement

This implementation uses app-only authentication and therefore requires a Microsoft 365 work/school tenant with an Exchange Online mailbox. A personal Outlook.com/Hotmail account alone is not sufficient for this client-credentials setup.

Recommended sender mailbox: `bookings@yourdomain.co.za` after the domain is added to Microsoft 365. A tenant's `onmicrosoft.com` address can be used for initial testing if it has an Exchange Online mailbox.

## Microsoft Entra setup

1. Sign in to the Microsoft Entra admin center with an account that can register applications.
2. Register a single-tenant application for DIBA Bookings.
3. Record the **Application (client) ID** and **Directory (tenant) ID**.
4. Under **API permissions**, add **Microsoft Graph → Application permissions → Mail.Send**.
5. Grant administrator consent for the tenant.
6. Create a client secret under **Certificates & secrets**. Copy the secret *value* when it is shown; it will not be shown again.
7. Make sure the sender address is a real Exchange Online mailbox in this tenant.
8. Restrict the app's mailbox access to the dedicated DIBA sender mailbox using your organisation's supported Exchange Online application access controls (prefer Exchange Application RBAC where available). Application `Mail.Send` is powerful; do not leave it with unnecessary access to every mailbox.

Microsoft's references:
- [Register an application](https://learn.microsoft.com/en-us/entra/identity-platform/quickstart-register-app)
- [Get access without a user (client credentials)](https://learn.microsoft.com/en-us/graph/auth-v2-service)
- [Send mail with Microsoft Graph](https://learn.microsoft.com/en-us/graph/api/user-sendmail?view=graph-rest-1.0)
- [Microsoft Graph permissions](https://learn.microsoft.com/en-us/graph/permissions-reference)

## Local development

Run commands from the repository root, adjusting the path only if your project is stored elsewhere. The project already has a `UserSecretsId`, so do not add credentials to `appsettings.json`.

```powershell
dotnet user-secrets set "Email:Enabled" "true" --project "Backend/DIBA_Backend/DIBA_Backend/DIBA_Backend.csproj"
dotnet user-secrets set "Email:Provider" "MicrosoftGraph" --project "Backend/DIBA_Backend/DIBA_Backend/DIBA_Backend.csproj"
dotnet user-secrets set "Email:FromAddress" "bookings@yourdomain.co.za" --project "Backend/DIBA_Backend/DIBA_Backend/DIBA_Backend.csproj"
dotnet user-secrets set "Email:MicrosoftGraph:TenantId" "YOUR_TENANT_ID" --project "Backend/DIBA_Backend/DIBA_Backend/DIBA_Backend.csproj"
dotnet user-secrets set "Email:MicrosoftGraph:ClientId" "YOUR_CLIENT_ID" --project "Backend/DIBA_Backend/DIBA_Backend/DIBA_Backend.csproj"
dotnet user-secrets set "Email:MicrosoftGraph:ClientSecret" "YOUR_CLIENT_SECRET_VALUE" --project "Backend/DIBA_Backend/DIBA_Backend/DIBA_Backend.csproj"
```

Replace the example sender and placeholder values with your real details. Do not paste the client secret into chat, source files, screenshots, or GitHub.

To check that the secret names are present without displaying values, avoid sharing output from `dotnet user-secrets list`, because it prints secret values.

## Production deployment

Set these environment variables in the hosting provider's private environment/secrets settings, not in the repository:

- `Email__Enabled=true`
- `Email__Provider=MicrosoftGraph`
- `Email__FromAddress=bookings@yourdomain.co.za`
- `Email__MicrosoftGraph__TenantId=...`
- `Email__MicrosoftGraph__ClientId=...`
- `Email__MicrosoftGraph__ClientSecret=...`

Keep email disabled until tenant permissions, sender mailbox, and secrets have been configured. Then test delivery to an address you control and verify the `EmailDeliveryLogs` record. A Graph API accepted response means Microsoft accepted the send request; it does not by itself guarantee that the recipient's inbox received the message.
