# Prompt: Extract Password Hashing From User Entity

## Objective

Move password hashing, password verification, password policy, and legacy plaintext-migration behavior out of the `UserAccount` entity.

Current finding:

- `src/SocialApp.User/Entities/UserAccount.cs` owns password hashing and verification details.
- The entity knows the concrete hash format and migration fallback.
- That mixes enterprise state/invariants with security implementation and migration concerns.

## Requirements

- Keep `UserAccount` focused on identity state and domain invariants.
- Introduce explicit password abstractions in the User application boundary.
- Keep concrete password hashing in an outer implementation, not in the entity.
- Preserve existing account creation, login, forgot password, and change password behavior.
- Preserve any existing plaintext-migration behavior unless deliberately removed with tests and documentation.
- Do not introduce ASP.NET Identity unless the repository already has a clear local pattern for it.

## Suggested Scope

- Add a User-owned password service abstraction such as `IPasswordService`.
- If useful, split policy validation into `IPasswordPolicy`.
- Update User interactors to call the password service before constructing or updating entity state.
- Change `UserAccount` so it accepts and stores opaque password hashes rather than creating them directly.
- Move the current PBKDF2 hashing implementation into a concrete adapter or infrastructure class.
- Register the concrete password service where User dependencies are composed.
- Update component tests to cover create account, login, password reset, and change password through the new abstraction.

## Verification

- Run focused User component tests.
- Run `dotnet test SocialApp.sln`.
- Run `docker compose config`.
- Run `docker compose build`.
- Because this changes authentication behavior, run `docker compose up -d` and smoke test account creation, login, and change password through API `http://localhost:8080` and Web `http://localhost:8081`.

