# Architecture

This repository demonstrates Clean Architecture with component-first packaging. The business components are the primary units of reuse, release, change, and reasoning.

## Component Diagram

```text
+---------------------+      +---------------------+
| SocialApp.User   |      | SocialApp.Post   |
|---------------------|      |---------------------|
| Entities            |      | Entities            |
| UseCases            |      | UseCases            |
| Gateways            |      | Gateways            |
| Controllers         |      | Controllers         |
| Presenters          |      | Presenters          |
| RequestModels       |      | RequestModels       |
| ResponseModels      |      | ResponseModels      |
| ViewModels          |      | ViewModels          |
+---------------------+      +---------------------+

No component references another component.
No component references application hosts or infrastructure frameworks.
```

## Request Flow

```text
External caller in test
  -> Controller
    -> Input Boundary
      -> Interactor
        -> Entity
        -> Gateway Interface
          -> In-memory Gateway
        -> Output Boundary
          -> Presenter
            -> View Model
```

Example: account creation

```text
CreateAccountController
  -> ICreateAccountInputBoundary
    -> CreateAccountInteractor
      -> UserAccount
      -> IUserGateway / ISessionGateway
      -> ICreateAccountOutputBoundary
        -> CreateAccountPresenter
          -> CreateAccountViewModel
```

## Dependency Direction

Dependencies point inward toward business rules:

```text
Controllers   Presenters
     \           /
      \         /
       Boundaries
          |
      Interactors
          |
       Entities
```

Gateway implementations are in the owning component because this repository is architecture-focused and uses in-memory infrastructure only for tests and demonstrations. There is no separate infrastructure project.

## Component Principles

REP, the Reuse/Release Equivalence Principle:
Each component is a coherent unit that could be versioned and released independently. `SocialApp.User` changes for account and authentication behavior. `SocialApp.Post` changes for posting, search, and feed behavior.

CCP, the Common Closure Principle:
Classes that change together live together. User account behavior is not split across account, authentication, and application projects. Post feed behavior is not scattered across shared use-case or infrastructure assemblies.

CRP, the Common Reuse Principle:
Consumers do not take dependencies on unrelated code. There is no shared core package that forces a component to reuse concepts it does not need.

ADP, the Acyclic Dependencies Principle:
Component references are acyclic. The current graph has no cross-component references.

SDP, the Stable Dependencies Principle:
Volatile details do not sit beneath stable business rules. Interactors depend on gateway interfaces owned by their component.

SAP, the Stable Abstractions Principle:
Stable boundaries are abstract where they need to be. Input boundaries, output boundaries, and gateway interfaces define the stable contracts around use cases.

## How This Differs From Package-By-Layer

A layer-first solution often starts like this:

```text
Application/
Domain/
Infrastructure/
InterfaceAdapters/
```

That structure highlights technical categories first and business capabilities second. It also encourages unrelated use cases to share broad projects and dependencies.

This repository starts with components:

```text
SocialApp.User/
SocialApp.Post/
```

Technical roles exist, but only inside the component that owns the behavior. The package boundary is the business boundary.

## Architecture Tests

`SocialApp.Architecture.Tests` enforces rules that documentation alone cannot protect:

- component isolation
- absence of framework references
- entity isolation
- interactor isolation from adapters
- presenter/output-boundary pairing
- controller/input-boundary pairing
- acyclic component references
