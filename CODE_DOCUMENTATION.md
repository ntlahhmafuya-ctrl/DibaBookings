# DIBA Bookings — Code Documentation

This document explains the responsibility of the main files and functionality areas. Code changes should keep the same convention: add a clear heading above each function or feature explaining what it is responsible for, then use short comments for important validation, security, and data-handling decisions.

## Frontend

### Application routes — `diba-bookings-frontend/src/App.js`
Responsible for connecting URL paths to React pages:
- `/` — public landing page.
- `/login` — sign-in form.
- `/register` — account registration form.
- `/dashboard` — chooses the dashboard based on the signed-in user's role.
- `/privacy` — privacy notice and personal-data export.
- `/profile` — self-service profile editing.

### Shared API client — `diba-bookings-frontend/src/services/api.js`
Responsible for creating the Axios client used by frontend services and attaching the stored bearer token to requests when one is available. Keep the API base URL and common request configuration in this file rather than duplicating them across pages.

### Profile page — `diba-bookings-frontend/src/pages/Profile.js`
Responsible for:
- loading the current user's profile from `GET /api/Users/me`;
- showing the first name, last name, email, and read-only account role;
- validating required fields before submission;
- saving the editable fields through `PUT /api/Users/me`;
- updating the locally cached display name/email after a successful save; and
- showing loading, success, and error feedback.

The page does not send a user ID. The backend identifies the current user from the authenticated request.

### Privacy notice and personal-data export — `diba-bookings-frontend/src/pages/PrivacyPolicy.js`
Responsible for presenting the initial privacy notice and allowing a signed-in user to request and download their personal-data export as a JSON file. The export endpoint is responsible for deciding which records belong to the authenticated user.

### Dashboard shell — `diba-bookings-frontend/src/components/common/DashboardShell.js`
Responsible for the shared header, profile shortcut, sign-out button, and content area used by the staff and administrator dashboards.

### Organiser sidebar — `diba-bookings-frontend/src/components/organizer/navigation/OrganizerSidebar.js`
Responsible for organiser workspace navigation, notification count, opening/closing the navigation drawer, and links to profile and sign-out actions.

### Styling — `diba-bookings-frontend/src/styles/landing.css`
Contains the public landing page, privacy notice, data-export controls, and self-service profile page styling. Keep page-specific styles grouped under a labelled heading.

## Backend

### User API controller — `Backend/DIBA_Backend/DIBA_Backend/Controllers/UsersController.cs`
The controller requires authentication by default. Its actions have these responsibilities:

#### `GetMyProfile` — `GET /api/Users/me`
Reads the user ID from the authenticated identity, loads that user's profile and role, and returns the profile response. It does not accept a user ID from the URL or request body.

#### `ExportMyData` — `GET /api/Users/me/export`
Returns a copy of the authenticated user's account details, owned events, bookings, payment summaries, and notifications. Queries are filtered by the authenticated user's ID. Password hashes and payment-provider credentials are not included.

#### `GetUsers` — `GET /api/Users`
Allows only users in the Administrator role to retrieve user summaries for administration.

#### `UpdateMyProfile` — `PUT /api/Users/me`
Updates the authenticated user's first name, last name, and email. The controller checks that the new email is not already assigned to another account. Role and account status are not editable through this endpoint.

### Profile update DTO — `Backend/DIBA_Backend/DIBA_Backend/Dto/User/UpdateUserDto.cs`
Defines the fields accepted for a profile update. It intentionally excludes the user ID, role, and account status so the request cannot use this DTO to change another account's identity or permissions.

## Documentation rules for future changes

1. Add a heading before each controller action, service function, React page, or substantial helper.
2. State the responsibility in one or two plain-language sentences.
3. Comment on non-obvious validation, ownership checks, authorization, and data transformations.
4. Explain *why* a security or business rule exists; avoid comments that merely repeat the code.
5. Update this file when a new major feature or responsibility is added.
6. Do not describe a feature as tested unless it has actually been run and verified.
