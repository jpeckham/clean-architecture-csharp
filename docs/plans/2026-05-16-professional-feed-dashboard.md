# Professional Feed Dashboard Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Redesign the SocialApp feed as a dark, professional, feed-first dashboard with a wider feed, polished cards, stronger typography, and a redesigned composer.

**Architecture:** Keep all behavior in the existing Blazor WebAssembly feed page. Use small semantic markup changes in `Feed.razor` and concentrate visual changes in `wwwroot/css/app.css`, with static web tests documenting the UI requirements.

**Tech Stack:** .NET 10, Blazor WebAssembly, Razor components, CSS, xUnit, FluentAssertions, Docker Compose.

---

### Task 1: Add Redesign Guard Tests

**Files:**
- Modify: `tests/SocialApp.Web.Tests/WebConfigurationTests.cs`
- Test: `tests/SocialApp.Web.Tests/WebConfigurationTests.cs`

**Step 1: Write the failing test**

Add tests that read `src/SocialApp.Web/wwwroot/css/app.css` and assert:

```csharp
[Fact]
public void Feed_styles_use_dark_professional_dashboard_theme()
{
    var css = ReadWebStylesheet();

    css.Should().Contain("color-scheme: dark");
    css.Should().Contain("--surface");
    css.Should().Contain("--accent");
}

[Fact]
public void Feed_styles_widen_posts_and_style_cards()
{
    var css = ReadWebStylesheet();

    css.Should().Contain("minmax(0, 820px)");
    css.Should().Contain(".post {");
    css.Should().Contain("box-shadow:");
}

[Fact]
public void Composer_has_dedicated_dashboard_styling()
{
    var css = ReadWebStylesheet();

    css.Should().Contain(".composer-header");
    css.Should().Contain(".composer-submit-row");
    css.Should().Contain(".composer textarea");
}
```

Add a helper:

```csharp
private static string ReadWebStylesheet()
{
    var root = FindRepositoryRoot();
    return File.ReadAllText(Path.Combine(root, "src", "SocialApp.Web", "wwwroot", "css", "app.css"));
}
```

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/SocialApp.Web.Tests/SocialApp.Web.Tests.csproj --filter WebConfigurationTests`

Expected: FAIL because the current stylesheet is light mode and lacks the new dashboard selectors/tokens.

**Step 3: Commit**

Commit the failing tests only if using separate commits:

```bash
git add tests/SocialApp.Web.Tests/WebConfigurationTests.cs
git commit -m "test: pin professional feed dashboard styling"
```

### Task 2: Update Feed Markup

**Files:**
- Modify: `src/SocialApp.Web/Pages/Feed.razor`

**Step 1: Add composer structure**

Replace the plain composer heading with:

```razor
<div class="composer-header">
    <div>
        <h2>Create Post</h2>
        <p>Post as @Session.Handle</p>
    </div>
</div>
```

Add `class="composer-input"` to the main `InputTextArea`.

Wrap the submit button in:

```razor
<div class="composer-submit-row">
    <span>@(selectedMedia.Count == 0 ? "Text only" : $"{selectedMedia.Count} media selected")</span>
    <button type="submit" disabled="@IsPostSubmitDisabled">Post</button>
</div>
```

**Step 2: Add feed toolbar structure**

Keep existing search behavior, but visually group the title and search form in the feed section using existing `feed-title-row` and `search-form` classes. Do not change routing or API calls.

### Task 3: Implement Dark Dashboard Styling

**Files:**
- Modify: `src/SocialApp.Web/wwwroot/css/app.css`

**Step 1: Add CSS tokens**

Set `:root` to dark mode and add tokens for background, surfaces, elevated surfaces, borders, text, muted text, accent, danger, and focus.

**Step 2: Widen the feed**

Update `.workspace` and `.feed-layout` so the feed column can reach roughly `820px`, while the composer column stays compact. Keep mobile as one column.

**Step 3: Style cards**

Make `.panel`, `.feed`, `.post`, `.quoted-post`, media previews, messages, and auth panels use dark surfaces, borders, radii no larger than `8px`, and consistent padding.

**Step 4: Improve typography and spacing**

Tune headings, body copy, timestamps, muted text, form controls, empty states, and action rows for a professional dashboard.

**Step 5: Redesign composer**

Style `.composer-header`, `.composer-input`, `.composer-media`, `.composer-submit-row`, file buttons, preview grid, and validation messages.

**Step 6: Keep responsive behavior**

At `max-width: 840px`, collapse to one column, reduce shell padding, and make action rows wrap cleanly.

### Task 4: Verify

**Files:**
- No edits unless verification exposes issues.

**Step 1: Run web tests**

Run: `dotnet test tests/SocialApp.Web.Tests/SocialApp.Web.Tests.csproj`

Expected: PASS.

**Step 2: Run relevant repository tests**

Run: `dotnet test SocialApp.sln`

Expected: PASS.

**Step 3: Run Docker Compose config**

Run: `docker compose config`

Expected: valid Compose configuration.

**Step 4: Build Docker Compose**

Run: `docker compose build`

Expected: API and Web images build successfully.

**Step 5: Smoke test user-facing flow**

Run: `docker compose up -d`

Smoke test:

- API: `http://localhost:8080`
- Web: `http://localhost:8081`

Confirm the web app serves and the API endpoint responds.
