# Profile Thumbnail Menu Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Replace authenticated page logout buttons with a reusable profile thumbnail menu that offers `Profile` and `Log Out`.

**Architecture:** Add a shared Blazor component under `src/SocialApp.Web/Components` that owns current-user profile lookup, thumbnail fallback, menu open/close state, profile navigation, and logout. Existing authenticated pages keep their masthead layout but replace page-local logout behavior with the shared component.

**Tech Stack:** Blazor WebAssembly, C#, `AppSession`, `SocialAppApiClient`, xUnit/FluentAssertions source-level tests, Docker Compose.

---

### Task 1: Add Web Tests

**Files:**
- Modify: `tests/SocialApp.Web.Tests/WebConfigurationTests.cs`

**Step 1: Write the failing tests**

Add tests that read `UserAccountMenu.razor`, `Feed.razor`, `PostDetails.razor`, and `UserProfile.razor` from disk. Assert:

- `UserAccountMenu.razor` exists and contains `Profile`, `Log Out`, `GetUserAsync`, `Session.SignOut()`, and `NavigateTo("/")`.
- `Feed.razor`, `PostDetails.razor`, and `UserProfile.razor` each contain `<UserAccountMenu`.
- `Feed.razor` no longer contains the old `@onclick="SignOut"` logout button or `private void SignOut()`.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/SocialApp.Web.Tests/SocialApp.Web.Tests.csproj`

Expected: FAIL because `UserAccountMenu.razor` does not exist and pages do not use it.

### Task 2: Add Shared Account Menu Component

**Files:**
- Create: `src/SocialApp.Web/Components/UserAccountMenu.razor`

**Step 1: Implement minimal component**

Create a component that:

- Injects `SocialAppApiClient`, `AppSession`, and `NavigationManager`.
- On parameters set, fetches `Api.GetUserAsync(Session.SessionToken!, Session.Handle!)` when logged in.
- Renders a button with the current profile image if available.
- Falls back to initials from display name or handle.
- Toggles a menu containing `Profile` and `Log Out`.
- Navigates to `/users/{escapedHandle}` for `Profile`.
- Calls `Session.SignOut()` and navigates to `/` for `Log Out`.

**Step 2: Run focused tests**

Run: `dotnet test tests/SocialApp.Web.Tests/SocialApp.Web.Tests.csproj`

Expected: Some tests may still fail until pages consume the component.

### Task 3: Replace Page-Level Logout UI

**Files:**
- Modify: `src/SocialApp.Web/Pages/Feed.razor`
- Modify: `src/SocialApp.Web/Pages/PostDetails.razor`
- Modify: `src/SocialApp.Web/Pages/UserProfile.razor`

**Step 1: Update masthead actions**

Add `<UserAccountMenu />` to each authenticated page's masthead action area. Remove the page-local logout button and `SignOut` method from `Feed.razor`.

**Step 2: Run focused tests**

Run: `dotnet test tests/SocialApp.Web.Tests/SocialApp.Web.Tests.csproj`

Expected: PASS.

### Task 4: Style the Menu

**Files:**
- Modify: `src/SocialApp.Web/wwwroot/css/app.css`

**Step 1: Add compact menu styles**

Add styles for `.account-menu`, `.account-menu-button`, `.account-thumbnail`, `.account-menu-popover`, and `.account-menu-item` that match the existing dark dashboard theme, stay compact, and work on mobile mastheads.

**Step 2: Run relevant tests**

Run: `dotnet test`

Expected: PASS.

### Task 5: Required Compose Verification

**Files:**
- No source edits expected.

**Step 1: Validate compose file**

Run: `docker compose config`

Expected: exit code 0.

**Step 2: Build compose services**

Run: `docker compose build`

Expected: exit code 0.

**Step 3: Smoke test through compose**

Run: `docker compose up -d`, then open or request `http://localhost:8081`.

Expected: web app responds and the authenticated masthead can show the profile menu after login.
