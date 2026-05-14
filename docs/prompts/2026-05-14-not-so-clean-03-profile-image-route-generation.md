# Prompt: Move Profile Image Route Generation Out Of Presenters

## Objective

Remove hard-coded HTTP route construction from User presenters.

Current finding:

- `src/SocialApp.User/Presenters/UserPresenters.cs` builds profile image URLs like `/api/profile-images/{assetId}`.
- That leaks API route knowledge into a presenter packaged with the User business component.

## Requirements

- Keep User presenters free of HTTP route literals.
- Preserve the existing profile image API response shape unless a coordinated contract change is necessary.
- Keep profile image metadata owned by the User component.
- Let the API or Web layer build transport-specific URLs.
- Avoid introducing a shared URL-building abstraction unless it is clearly needed.

## Suggested Scope

- Change the User presenter or view model mapping so it emits profile image asset identity and metadata without API route construction.
- Move URL generation to the API endpoint layer, preferably using named routes or ASP.NET Core link generation.
- Update Web client or UI code only if the HTTP contract changes.
- Add or update tests around profile image URL output at the API boundary rather than in User presenter tests.
- Remove presenter tests that assert API route strings from the User component, replacing them with metadata assertions.

## Verification

- Run focused User tests and API tests covering profile image responses.
- Run `dotnet test SocialApp.sln`.
- Run `docker compose config`.
- Run `docker compose build`.
- Because this affects a user-visible media flow, run `docker compose up -d` and smoke test viewing and updating a profile image through API `http://localhost:8080` and Web `http://localhost:8081`.

