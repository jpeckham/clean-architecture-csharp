# Email Login Design

## Goal

Login should use the user's private email address instead of their public handle, and the UI should stop pre-populating authentication fields with demo values.

## Approach

Create account continues to collect display name, handle, email, and password. Login collects email and password. The API session contracts accept `Email` for login and device login, while successful responses still return the public handle because the feed and post APIs use handles for public author identity.

The user component login interactors authenticate with `IUserGateway.FindByEmail`. Device OTP verification can continue to use the handle returned by the first device-login step because the current flow does not have a separate pending-login token.

## Boundaries

This change stays inside the user component, API contracts/endpoints, and Blazor Web UI. Post behavior and public handle usage remain unchanged.

## Verification

- User component tests prove email login works and handle login is rejected.
- API slice tests prove `/api/sessions` accepts `email`.
- Web build proves updated forms and client contracts compile.

