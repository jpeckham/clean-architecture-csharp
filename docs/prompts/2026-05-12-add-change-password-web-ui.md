# Prompt: Add Change Password Use Case To Web UI

## Objective

Add web UI coverage for the existing legacy `ChangePassword` use case.

The domain use case exists in `SocialApp.User`, but it is not currently mapped through the API or web UI:
- `ChangePasswordInteractor`
- `ChangePasswordController`
- `ChangePasswordPresenter`

The current UI uses `ResetPassword`, which validates the newer password reset token gateway. This prompt is for exposing `ChangePassword` separately for the existing reset-token flow.

## Requirements

- Preserve the component-first Clean Architecture structure.
- Add an API endpoint that invokes `ChangePasswordInteractor`.
- Add web client support in `SocialAppApiClient`.
- Add a Blazor UI path that accepts reset token and new password.
- Keep the existing `/reset-password` flow working.
- Surface invalid token and account-not-found responses as user-visible messages.

## Suggested Scope

- Add an HTTP contract in `src/SocialApp.Api/Contracts/AccountContracts.cs`.
- Add an endpoint in `SocialAppSliceEndpoints`.
- Add client record/methods in `SocialAppApiClient`.
- Add or update a page in `src/SocialApp.Web/Pages`.
- Add focused API slice tests and any relevant component tests.

## Verification

- Run `dotnet test SocialApp.sln`.
- Manually verify the change-password UI changes a password for a valid legacy reset token.
