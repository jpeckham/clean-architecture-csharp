# Prompt: Harden Architecture Boundary Tests

## Objective

Strengthen `SocialApp.Architecture.Tests` so strict boundary drift is caught by tests.

Current finding:

- Existing architecture tests enforce several useful project-reference rules.
- They do not currently forbid controllers, presenters, view models, or in-memory gateway implementations from living in the same assemblies as business rules.
- Some tests validate behavior rather than architecture.

## Requirements

- Preserve existing architecture tests that still encode valid rules.
- Add enforcement for ring responsibilities without blocking the current component-first intent.
- Avoid brittle tests that fail on harmless naming changes unless naming is the rule being enforced.
- Do not add a large custom analyzer unless a simple NetArchTest/reflection rule is insufficient.
- Keep test failures readable and actionable.

## Suggested Scope

- Add tests preventing domain entities from depending on presenters, controllers, gateways, infrastructure namespaces, or framework packages.
- Add tests preventing interactors/use cases from depending on concrete infrastructure or API/Web assemblies.
- Add tests detecting route literals, `System.Net.Http`, `System.Text.Json`, ASP.NET, MongoDB, Azure SDK, or DI framework usage in inner component namespaces where appropriate.
- Add tests around adapter placement if the current project structure supports a reliable rule.
- Move non-architecture behavior tests out of `SocialApp.Architecture.Tests` if they belong in API/Web/component tests.
- Update test names and assertion messages so a failed boundary rule tells the next developer what to move.

## Verification

- Run `dotnet test tests/SocialApp.Architecture.Tests/SocialApp.Architecture.Tests.csproj`.
- Run `dotnet test SocialApp.sln`.

