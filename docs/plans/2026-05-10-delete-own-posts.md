# Delete Own Posts Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Let signed-in users delete only posts they authored from the Blazor feed UI.

**Architecture:** Keep the author-only rule inside `SocialApp.Post` by using the existing `DeletePost` use case and `SocialPost.DeleteBy`. The API and Blazor app are delivery details that authenticate, invoke the component controller, and present the result. Cosmos Mongo persists the changed deleted state through `IPostGateway`.

**Tech Stack:** C#/.NET, xUnit, FluentAssertions, ASP.NET Core Minimal APIs, Blazor WebAssembly, MongoDB driver test gateway.

---

### Task 1: Component Rule Coverage

**Files:**
- Modify: `tests/SocialApp.Post.Tests/PostComponentTests.cs`

**Step 1: Write the failing test**

Add a test named `Delete_post_rejects_non_author_and_keeps_post_visible` that creates a post by `@ada`, tries to delete it through `DeletePostController` as `@grace`, and asserts the action throws `InvalidOperationException` and `IsDeleted` remains false.

**Step 2: Run test to verify it fails or exposes current behavior**

Run: `dotnet test tests/SocialApp.Post.Tests/SocialApp.Post.Tests.csproj --filter Delete_post_rejects_non_author_and_keeps_post_visible`

Expected: the test passes if the domain rule already exists. If it fails, fix only the Post component rule before continuing.

**Step 3: Commit**

Run: `git add tests/SocialApp.Post.Tests/PostComponentTests.cs && git commit -m "test: cover post deletion ownership rule"`

### Task 2: Persist Deleted State

**Files:**
- Modify: `tests/SocialApp.Infrastructure.CosmosMongo.Tests/CosmosMongoMappingTests.cs`
- Modify: `src/SocialApp.Post/UseCases/PostInteractors.cs`

**Step 1: Write the failing test**

Add a test proving a post deleted through `DeletePostInteractor` is not returned by `ScrollFor` after using `CosmosMongoPostGateway`.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/SocialApp.Infrastructure.CosmosMongo.Tests/SocialApp.Infrastructure.CosmosMongo.Tests.csproj --filter Delete`

Expected: FAIL because the interactor marks the entity deleted but does not save it back through the gateway.

**Step 3: Write minimal implementation**

In `DeletePostInteractor.Handle`, after `post.DeleteBy(request.RequesterHandle)`, call `posts.Save(post)`.

**Step 4: Run test to verify it passes**

Run: `dotnet test tests/SocialApp.Infrastructure.CosmosMongo.Tests/SocialApp.Infrastructure.CosmosMongo.Tests.csproj --filter Delete`

Expected: PASS.

**Step 5: Commit**

Run: `git add tests/SocialApp.Infrastructure.CosmosMongo.Tests/CosmosMongoMappingTests.cs src/SocialApp.Post/UseCases/PostInteractors.cs && git commit -m "fix: persist deleted post state"`

### Task 3: Hosted API Endpoint

**Files:**
- Modify: `tests/SocialApp.Api.Tests/SocialAppApiSliceTests.cs`
- Modify: `src/SocialApp.Api/Endpoints/SocialAppSliceEndpoints.cs`

**Step 1: Write failing API tests**

Add tests for `DELETE /api/posts/{postId}`:

- valid creator bearer token deletes the post and removes it from recent posts
- missing or invalid bearer token returns unauthorized
- non-author bearer token returns forbidden or bad request and leaves the post visible

**Step 2: Run tests to verify failure**

Run: `dotnet test tests/SocialApp.Api.Tests/SocialApp.Api.Tests.csproj --filter Delete`

Expected: FAIL with no matching endpoint.

**Step 3: Write minimal implementation**

Map `DELETE /api/posts/{postId}`. Read the bearer token with the existing helper, resolve the session, invoke `DeletePostController`, catch `InvalidOperationException`, and return the presenter view model.

**Step 4: Run tests to verify pass**

Run: `dotnet test tests/SocialApp.Api.Tests/SocialApp.Api.Tests.csproj --filter Delete`

Expected: PASS.

**Step 5: Commit**

Run: `git add tests/SocialApp.Api.Tests/SocialAppApiSliceTests.cs src/SocialApp.Api/Endpoints/SocialAppSliceEndpoints.cs && git commit -m "feat: expose delete post endpoint"`

### Task 4: Blazor Client And Feed UI

**Files:**
- Modify: `src/SocialApp.Web/Services/SocialAppApiClient.cs`
- Modify: `src/SocialApp.Web/Pages/Feed.razor`
- Modify: `src/SocialApp.Web/wwwroot/css/app.css` if existing button styles need spacing

**Step 1: Add client method**

Add `DeletePostAsync(string sessionToken, Guid postId)` that sends `DELETE /api/posts/{postId}` with the bearer token and reads `SimpleResult`.

**Step 2: Add UI behavior**

Inject `IJSRuntime`. Render a `Delete` button only when `string.Equals(item.AuthorHandle, Session.Handle, StringComparison.OrdinalIgnoreCase)`. On click, call `confirm`, then `Api.DeletePostAsync`, refresh posts, and show the result message.

**Step 3: Verify build**

Run: `dotnet build SocialApp.sln`

Expected: PASS.

**Step 4: Commit**

Run: `git add src/SocialApp.Web/Services/SocialAppApiClient.cs src/SocialApp.Web/Pages/Feed.razor src/SocialApp.Web/wwwroot/css/app.css && git commit -m "feat: add delete action to feed"`

### Task 5: Full Verification

**Files:**
- All touched files

**Step 1: Run full test suite**

Run: `dotnet test SocialApp.sln`

Expected: PASS.

**Step 2: Run build**

Run: `dotnet build SocialApp.sln`

Expected: PASS.

**Step 3: Commit if any verification-only cleanup was needed**

Commit any final fixes with a focused message.

