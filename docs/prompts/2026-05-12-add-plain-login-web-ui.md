# Prompt: Add Plain Login Use Case To Web UI

## Objective

Add web UI coverage for the existing `Login` use case.

The domain use case already exists in `SocialApp.User` and the API/client route already exists:
- `LoginInteractor`
- `POST /api/sessions`
- `SocialAppApiClient.LoginAsync`

The current login page uses device-aware login through `LoginWithDeviceAsync`. Add a UI path for plain email/password login without removing the device OTP flow.

## Requirements

- Preserve the component-first Clean Architecture structure.
- Keep the existing device login and OTP behavior working.
- Add a clear way to use plain login from the web UI.
- On success, store the returned handle/session token in `AppSession` and navigate to `/feed`.
- Surface failed login as a user-visible message.
- Reuse the existing login page style and service patterns.

## Suggested Scope

- Update `src/SocialApp.Web/Pages/Login.razor` or add a small separate page.
- Use `SocialAppApiClient.LoginAsync`.
- Avoid duplicating large login form logic if a small mode switch is enough.
- Add focused tests for the plain login route if existing test patterns support it.

## Verification

- Run `dotnet test SocialApp.sln`.
- Manually verify plain login reaches `/feed` and device login still supports OTP.
