# Deep Linking And Share Post Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Build stable individual post URLs and a share prompt that exposes a copyable deep link.

**Architecture:** Add a clean-architecture display-one-post use case that projects a single active post through the existing post summary pipeline. The API exposes `GET /api/posts/{postId}` with bearer authentication, and the Blazor WebAssembly app renders `/posts/{postId:guid}` using a shared post card component also used by the feed.

**Tech Stack:** .NET 10, ASP.NET Core minimal APIs, Blazor WebAssembly, xUnit, FluentAssertions, Docker Compose.

---

### Task 1: Post Use Case

**Files:**
- Modify: `tests/SocialApp.Post.Tests/PostComponentTests.cs`
- Modify: `src/SocialApp.Post/RequestModels/PostRequests.cs`
- Modify: `src/SocialApp.Post/ResponseModels/PostResponses.cs`
- Modify: `src/SocialApp.Post/UseCases/PostBoundaries.cs`
- Modify: `src/SocialApp.Post/UseCases/PostInteractors.cs`
- Modify: `src/SocialApp.Post/Controllers/PostControllers.cs`
- Modify: `src/SocialApp.Post/Presenters/PostPresenters.cs`
- Modify: `src/SocialApp.Post/ViewModels/PostViewModels.cs`

**Steps:**
1. Write a failing test proving the display-one interactor returns one active post with reader-specific like state.
2. Run `dotnet test tests/SocialApp.Post.Tests/SocialApp.Post.Tests.csproj --filter Display`.
3. Add request/response/boundary/controller/presenter/interactor records and classes.
4. Run the focused post tests again.

### Task 2: API Endpoint

**Files:**
- Modify: `tests/SocialApp.Api.Tests/SocialAppApiSliceTests.cs`
- Modify: `src/SocialApp.Api/Endpoints/SocialAppSliceEndpoints.cs`

**Steps:**
1. Write failing API tests for `GET /api/posts/{postId}` success, not found for deleted/missing posts, and unauthorized without a valid bearer token.
2. Run `dotnet test tests/SocialApp.Api.Tests/SocialApp.Api.Tests.csproj --filter Individual`.
3. Map `GET /api/posts/{postId:guid}` before mutating child routes and reuse the existing HTTP summary mapper.
4. Run the focused API tests again.

### Task 3: Web Deep Link And Share UI

**Files:**
- Modify: `tests/SocialApp.Web.Tests/WebConfigurationTests.cs`
- Modify: `src/SocialApp.Web/Services/SocialAppApiClient.cs`
- Create: `src/SocialApp.Web/Components/PostCard.razor`
- Create: `src/SocialApp.Web/Pages/PostDetails.razor`
- Modify: `src/SocialApp.Web/Pages/Feed.razor`
- Modify: `src/SocialApp.Web/wwwroot/css/app.css`

**Steps:**
1. Write failing route/client-shape tests for the `/posts/{postId:guid}` page and shared post card component.
2. Run `dotnet test tests/SocialApp.Web.Tests/SocialApp.Web.Tests.csproj --filter Post`.
3. Add the API client method.
4. Extract reusable post card markup and action callbacks from `Feed.razor`.
5. Add `PostDetails.razor` that loads one post, handles actions, and uses `prompt` for sharing.
6. Update feed to render `PostCard` and share prompt links.
7. Run the focused web tests again.

### Task 4: Full Verification

**Steps:**
1. Run `dotnet test SocialApp.sln`.
2. Run `docker compose config`.
3. Run `docker compose build`.
4. Run `docker compose up -d`.
5. Smoke test API `http://localhost:8080` and Web `http://localhost:8081`.
6. Report all verification results.
