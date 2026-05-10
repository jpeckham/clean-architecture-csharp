# Codex Agent Prompt — Component-First Clean Architecture Reference Repository

## Objective

Create a baseline reference architecture repository in C# demonstrating Robert C. Martin's Clean Architecture principles using true component/package decomposition rather than traditional layer-first decomposition.

The repository must emphasize:
- Component cohesion
- Independent deployability
- Stable abstractions
- Explicit boundaries
- Dependency rule enforcement
- Feature/component packaging
- Replaceable infrastructure
- Testability

The repository is intended to:
- Teach Clean Architecture
- Demonstrate component principles
- Serve as a reusable baseline repository
- Work well with AI-assisted development
- Be easy for humans and LLMs to reason about

Use a microblogging-style social application because the domain is universally understandable.

---

# Required Features

Implement the following features:

## User Component
1. Create Account
2. Login
3. Forgot Password
4. Change Password
5. Search User
6. View User

## Post Component
7. Create Post
8. Scroll Posts
9. Search Posts
10. Follow User Posts
11. Block User Posts
12. Add Like To Post
13. Delete Like From Post
14. Reply To Post
15. Repost
16. Delete Post

---

# Critical Architectural Requirement

This repository MUST use COMPONENT-FIRST decomposition.

DO NOT organize primarily by technical layer.

DO NOT create giant shared projects like:
- UseCases
- Infrastructure
- InterfaceAdapters
- Application

Those are layer-first structures and do not properly demonstrate component/package architecture.

Instead, organize around cohesive business capabilities.

There are intentionally ONLY TWO primary business components:
- User
- Post

---

# Required Architectural Style

The architecture MUST follow:
- Robert C. Martin Clean Architecture
- Component Principles
- SOLID principles
- Screaming Architecture concepts

The architecture MUST intentionally demonstrate:
- REP (Reuse/Release Equivalence Principle)
- CCP (Common Closure Principle)
- CRP (Common Reuse Principle)
- ADP (Acyclic Dependencies Principle)
- SDP (Stable Dependencies Principle)
- SAP (Stable Abstractions Principle)

---

# Required Solution Structure

Example structure:

/src
    /SocialApp.User
    /SocialApp.Post

/tests
    /SocialApp.User.Tests
    /SocialApp.Post.Tests
    /SocialApp.Architecture.Tests

---

# Purpose of SocialApp.Architecture.Tests

The SocialApp.Architecture.Tests assembly exists to mechanically enforce architectural rules.

It is NOT:
- a unit test project
- an integration test project
- a behavior test project
- an end-to-end test project

Its sole purpose is to prevent architectural decay.

Use:
- NetArchTest
- reflection-based tests
- assembly inspection

This project should contain tests validating:

- dependency direction
- boundary isolation
- absence of cyclic dependencies
- framework leakage prevention
- naming conventions
- interface enforcement
- component isolation

Examples of rules to validate:

- User component must not depend on Post implementation details
- Entities must not depend on presenters
- Controllers must not contain business logic
- Interactors must not depend on frameworks
- Components must not reference ASP.NET
- Components must not reference Entity Framework
- No cyclic dependencies allowed
- Presenters must implement output boundaries
- Controllers must depend on input boundaries

Architecture rules should be executable tests rather than documentation-only guidance.



# Architectural Intent

Each component owns:
- its own entities
- its own use cases
- its own boundaries
- its own presenters
- its own controllers
- its own gateway interfaces
- its own DTOs
- its own tests

This is intentional.

The architecture should "scream" the business capabilities rather than technical frameworks.

---

# Component Responsibilities

# 1. SocialApp.User

Purpose:
- User account lifecycle
- Authentication behavior

Contains:
- Entities
- Use cases
- Controllers
- Presenters
- Gateway interfaces
- View models

Required use cases:
- CreateAccount
- Login
- ForgotPassword
- ChangePassword
- SearchUser
- ViewUser

Required interfaces:
- ICreateAccountInputBoundary
- ICreateAccountOutputBoundary
- ILoginInputBoundary
- ILoginOutputBoundary
- IUserGateway
- ISessionGateway
- IPasswordResetGateway

Required implementations:
- CreateAccountInteractor
- LoginInteractor
- ForgotPasswordInteractor
- ChangePasswordInteractor
- SearchUserInteractor
- ViewUserInteractor

- CreateAccountController
- LoginController
- ForgotPasswordController
- ChangePasswordController
- SearchUserController
- ViewUserController

- CreateAccountPresenter
- LoginPresenter
- ForgotPasswordPresenter
- ChangePasswordPresenter
- SearchUserPresenter
- ViewUserPresenter

Rules:
- No ASP.NET
- No database technology
- No ORM
- No infrastructure framework
- No dependency injection framework

User owns all account and authentication behavior.

Do NOT split "account" and "authentication" into separate components.

---

# 2. SocialApp.Post

Purpose:
- Post management
- Post searching
- Feed scrolling

Contains:
- Entities
- Use cases
- Controllers
- Presenters
- Gateway interfaces
- View models

Required use cases:
- CreatePost
- ScrollPosts
- SearchPosts
- FollowUserPosts
- BlockUserPosts
- AddLikeToPost
- DeleteLikeFromPost
- ReplyToPost
- Repost
- DeletePost

Required interfaces:
- ICreatePostInputBoundary
- ICreatePostOutputBoundary
- IPostGateway
- IPostSearchGateway

Required implementations:
- CreatePostInteractor
- ScrollPostsInteractor
- SearchPostsInteractor
- FollowUserPostsInteractor
- BlockUserPostsInteractor
- AddLikeToPostInteractor
- DeleteLikeFromPostInteractor
- ReplyToPostInteractor
- RepostInteractor
- DeletePostInteractor

- CreatePostController
- ScrollPostsController
- SearchPostsController
- FollowUserPostsController
- BlockUserPostsController
- AddLikeToPostController
- DeleteLikeFromPostController
- ReplyToPostController
- RepostController
- DeletePostController

- CreatePostPresenter
- ScrollPostsPresenter
- SearchPostsPresenter
- FollowUserPostsPresenter
- BlockUserPostsPresenter
- AddLikeToPostPresenter
- DeleteLikeFromPostPresenter
- ReplyToPostPresenter
- RepostPresenter
- DeletePostPresenter

Rules:
- No database technology
- No frameworks
- No infrastructure leakage

Post owns all posting/search/feed behavior.

---

# No Shared Core Library

Do NOT create:
- SharedKernel
- Core
- Common
- Abstractions

This repository intentionally avoids a central "core" dependency because:
- It becomes a dumping ground
- It increases coupling
- It hides dependency direction problems
- It violates component independence

If a concept truly must be shared, duplicate it unless there is overwhelming architectural justification to centralize it.

Prefer duplication over premature shared abstractions.

---

# Required Internal Structure

Each component should internally contain folders similar to:

/Controllers
/Presenters
/UseCases
/Entities
/Gateways
/ViewModels
/RequestModels
/ResponseModels

These folders are INTERNAL to the component.

The repository itself is package-by-component.

---

# Dependency Rules

Dependencies MUST point inward only.

Allowed:
Post -> User (only through abstractions if absolutely necessary)

NOT allowed:
User -> Post
Controllers -> infrastructure frameworks
Interactors -> databases
Entities -> presenters
Entities -> controllers
Entities -> gateways

Avoid cyclic dependencies entirely.

---

# Required Clean Architecture Boundaries

Each component MUST contain:

## Controllers

Responsibilities:
- Translate external requests
- Build request models
- Invoke input boundaries

Controllers MUST NOT:
- Contain business logic
- Access persistence directly

---

## Presenters

Responsibilities:
- Implement output boundaries
- Transform response models into view models

Presenters MUST NOT:
- Execute business rules
- Access databases

---

## Interactors

Responsibilities:
- Execute application business rules
- Coordinate entities and gateways

Interactors MUST:
- Depend on abstractions only

---

## Gateway Interfaces

Gateway interfaces belong INSIDE the owning component.

Avoid:
- Shared repositories
- Generic repositories
- Shared data services
- IRepository<T>

---

# Infrastructure Requirement

Infrastructure implementations should remain inside the owning component.

Example:
- InMemoryUserGateway inside SocialApp.User
- InMemoryPostGateway inside SocialApp.Post

There should NOT be a separate infrastructure project.

The infrastructure exists only as mock/fake implementations to support:
- testing
- demonstrations
- interactors

---

# No Application Host

Do NOT create:
- ConsoleHost
- ASP.NET host
- API host
- Web application
- Startup project

This repository is intended to be architecture-focused only.

The executable behavior should be demonstrated entirely through tests.

---

# Required Testing Strategy

Use:
- xUnit
- FluentAssertions
- NetArchTest

---

# User Component Tests

SocialApp.User.Tests should validate:
- User entity behavior
- Authentication rules
- Password rules
- Login behavior
- Controller behavior
- Presenter behavior
- Gateway interactions

---

# Post Component Tests

SocialApp.Post.Tests should validate:
- Post entity behavior
- Search behavior
- Feed scrolling behavior
- Delete rules
- Controller behavior
- Presenter behavior
- Gateway interactions

---

# Architecture Tests

Create explicit architecture tests validating:
- No cyclic dependencies
- No forbidden references
- Dependency direction
- No framework leakage
- Boundary isolation

Examples:
- Entities cannot reference presenters
- Interactors cannot reference infrastructure frameworks
- Controllers cannot contain business rules
- User must not depend on Post implementation details

---

# Required Request Flow Demonstration

The tests should clearly demonstrate:

Controller
    -> Input Boundary
        -> Interactor
            -> Gateway Interface
                -> Fake/InMemory Gateway
            -> Output Boundary
                -> Presenter
                    -> ViewModel

This flow should be easy to trace and understand.

---

# Explicitly Avoid

Do NOT introduce:
- ASP.NET
- Entity Framework
- MediatR
- CQRS frameworks
- Event sourcing
- Minimal APIs
- Generic repositories
- Reflection-based DI
- Vertical slice frameworks
- GraphQL
- Microservices
- ORMs
- Framework magic

The repository is intentionally framework-independent.

---

# Required Documentation

Generate:

## README.md

Explain:
- Component-first architecture
- Why there is no shared core library
- Why package-by-component was chosen
- Dependency rules
- How to add a new component
- How to add a new use case

---

## docs/Architecture.md

Include:
- Dependency diagrams
- Component diagrams
- Request flow diagrams
- Explanation of component principles
- Why this differs from package-by-layer

---

## docs/AddingNewUseCase.md

Demonstrate:
- How to add a use case
- How to create boundaries
- How to create interactors
- How to add architecture tests

---

# Important Design Goals

Optimize for:
- Clarity
- Explicitness
- Educational value
- Boundary visibility
- Replaceability
- Testability
- AI readability
- Human readability

NOT:
- Clever abstractions
- Framework optimization
- Boilerplate minimization
- Runtime performance tricks

---

# Deliverables

Generate:
- Complete solution
- All csproj files
- Full component structure
- Controllers
- Presenters
- Interactors
- Entities
- Gateway interfaces
- In-memory implementations
- Architecture tests
- Unit tests
- Documentation

Do NOT generate:
- Web applications
- API hosts
- Console applications

The repository should compile successfully and all tests should pass.
