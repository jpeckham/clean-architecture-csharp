# Post Likes Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add authenticated like and unlike behavior for posts, including filled/unfilled heart state and visible like counts in the feed.

**Architecture:** The post component already owns like state through `SocialPost.LikedBy`. The API will expose authenticated post-like endpoints that derive the liker from the bearer token, and post summaries will include whether the current reader has liked each post.

**Tech Stack:** C#/.NET minimal API, Blazor WebAssembly, xUnit, FluentAssertions.

---

### Task 1: Post Component Behavior

**Files:**
- Modify: `tests/SocialApp.Post.Tests/PostComponentTests.cs`
- Modify: `src/SocialApp.Post/ResponseModels/PostResponses.cs`
- Modify: `src/SocialApp.Post/ViewModels/PostViewModels.cs`
- Modify: `src/SocialApp.Post/UseCases/PostInteractors.cs`
- Modify: `src/SocialApp.Post/Presenters/PostPresenters.cs`

**Step 1: Write failing tests**

Add tests proving:
- scrolling posts marks `LikedByCurrentReader` true only for posts liked by the reader
- deleting a like fails when the handle has not liked the post
- adding a like persists through the gateway

**Step 2: Run red tests**

Run:

```powershell
dotnet test tests/SocialApp.Post.Tests/SocialApp.Post.Tests.csproj
```

Expected: FAIL because summary state and delete-like validation are not implemented.

**Step 3: Implement minimal component changes**

Update `PostSummaryResponse` and `PostSummaryViewModel` to include `bool LikedByCurrentReader`. Change summary creation to accept the reader handle for scroll results. Save posts after add-like and delete-like. Reject delete-like when `LikedBy` does not contain the requester handle.

**Step 4: Run green tests**

Run:

```powershell
dotnet test tests/SocialApp.Post.Tests/SocialApp.Post.Tests.csproj
```

Expected: PASS.

### Task 2: API Endpoints

**Files:**
- Modify: `tests/SocialApp.Api.Tests/SocialAppApiSliceTests.cs`
- Modify: `src/SocialApp.Api/Endpoints/SocialAppSliceEndpoints.cs`

**Step 1: Write failing tests**

Add API tests proving:
- authenticated users can like and unlike their own like
- feed summaries show `LikeCount` and `LikedByCurrentReader`
- missing or invalid tokens cannot like/unlike
- one user cannot delete another user's like

**Step 2: Run red tests**

Run:

```powershell
dotnet test tests/SocialApp.Api.Tests/SocialApp.Api.Tests.csproj
```

Expected: FAIL because the like endpoints and summary contract are missing.

**Step 3: Implement minimal API changes**

Map:
- `POST /api/posts/{postId:guid}/likes`
- `DELETE /api/posts/{postId:guid}/likes`

Authenticate with the existing bearer-token helper, call the existing post like controllers with the session user's handle, and translate presenter results to `200`, `400`, `401`, or `404`.

**Step 4: Run green tests**

Run:

```powershell
dotnet test tests/SocialApp.Api.Tests/SocialApp.Api.Tests.csproj
```

Expected: PASS.

### Task 3: Blazor Feed UI

**Files:**
- Modify: `src/SocialApp.Web/Services/SocialAppApiClient.cs`
- Modify: `src/SocialApp.Web/Pages/Feed.razor`
- Modify: `src/SocialApp.Web/wwwroot/css/app.css`

**Step 1: Update client contract**

Add `LikePostAsync` and `DeleteLikeAsync` methods that send authenticated requests to the new endpoints. Extend `PostSummaryResult` with `LikedByCurrentReader`.

**Step 2: Render and toggle heart**

Render a compact heart button below the post content. Use an unfilled heart for unliked posts, a filled heart for liked posts, and put `LikeCount` immediately to the right. On click, call like or unlike depending on `LikedByCurrentReader`, then reload posts.

**Step 3: Run web build**

Run:

```powershell
dotnet build src/SocialApp.Web/SocialApp.Web.csproj
```

Expected: PASS.

### Task 4: Full Verification

**Files:**
- Verify all touched projects.

**Step 1: Run full test suite**

Run:

```powershell
dotnet test SocialApp.sln
```

Expected: PASS.

**Step 2: Run full build**

Run:

```powershell
dotnet build SocialApp.sln
```

Expected: PASS.
