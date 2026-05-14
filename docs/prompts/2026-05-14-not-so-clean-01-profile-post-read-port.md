# Prompt: Replace Profile Post Static Mapper Coupling

## Objective

Remove the User profile post adapter's dependency on `CreatePostInteractor.ToSummary` and replace it with an explicit read contract.

Current finding:

- `src/SocialApp.Api/Endpoints/UserProfilePostGatewayAdapter.cs` imports `SocialApp.Post.UseCases`.
- It calls `CreatePostInteractor.ToSummary`.
- That couples a User-facing API adapter to a Post use-case implementation helper.

## Requirements

- Preserve component-first Clean Architecture.
- Do not add a shared read-model project.
- Do not make `SocialApp.User` depend on `SocialApp.Post`.
- Do not make User profile behavior reuse Post interactor implementation details.
- Keep profile post summaries behaviorally equivalent for existing API/Web consumers.

## Suggested Scope

- Introduce a dedicated Post-owned read port or query service contract for profile post summaries.
- Keep the contract close to the Post component unless an existing boundary location is clearly better.
- Move the summary projection logic currently exposed by `CreatePostInteractor.ToSummary` behind that read contract.
- Update `UserProfilePostGatewayAdapter` to depend on the new contract rather than `CreatePostInteractor`.
- Remove or reduce public static helper exposure from `CreatePostInteractor` if it is no longer needed.
- Add or update tests proving profile pages still include recent posts with like/repost state for the current reader.

## Verification

- Run focused Post and API tests covering profile-post projection.
- Run `dotnet test SocialApp.sln`.
- Run `docker compose config`.
- Run `docker compose build`.
- If the profile page behavior changes at all, run `docker compose up -d` and smoke test viewing a user profile through API `http://localhost:8080` and Web `http://localhost:8081`.

