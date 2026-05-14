# Prompt: Fix InMemory Post Search Gateway Abstraction Leak

## Objective

Remove the downcast from `IPostGateway` to `InMemoryPostGateway` in the in-memory post search gateway.

Current finding:

- `src/SocialApp.Post/Gateways/PostGateways.cs` contains `InMemoryPostSearchGateway`.
- It accepts `IPostGateway` but immediately casts it to `InMemoryPostGateway`.
- That is a runtime-fragile abstraction leak.

## Requirements

- Preserve existing search behavior.
- Do not make production persistence depend on in-memory types.
- Do not introduce a generic repository.
- Keep the in-memory implementation clearly test/demo scoped.
- Keep changes narrow unless the local design requires a small supporting interface.

## Suggested Scope

- Either inject `InMemoryPostGateway` directly into `InMemoryPostSearchGateway`, or add a narrow internal read-store interface implemented by the in-memory gateway.
- Update dependency registration and tests accordingly.
- Add a regression test that proves constructing the in-memory search gateway does not accept an incompatible gateway abstraction.
- Keep the public `IPostSearchGateway` contract unchanged unless tests reveal a real design issue.

## Verification

- Run focused Post component tests for search.
- Run `dotnet test SocialApp.sln`.

