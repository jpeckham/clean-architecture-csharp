# Professional Feed Dashboard Design

## Goal

Revamp the SocialApp feed into a dark, professional, feed-first dashboard without inventing product areas that the app cannot yet populate with real data.

## Direction

The current app has real feed primitives: creating posts, attaching media, searching posts, reading recent posts, liking, reposting, deleting, and signing out. The redesign should make those workflows feel deliberate and polished while avoiding placeholder modules such as trending topics, network rankings, or analytics cards.

## Approach

Use a content-honest professional dashboard layout:

- Apply a dark visual system across the app with neutral surfaces, restrained borders, clear focus states, and teal/blue action accents.
- Widen the feed so posts are the primary workspace instead of a narrow list.
- Redesign posts as individual elevated cards with better author/time hierarchy, readable body copy, media spacing, and a clearer action row.
- Redesign the composer as a dashboard-grade control with a stronger header, larger input, improved media controls, preview cards, and a clearer submit action.
- Keep search and session actions visible as real dashboard controls, not fake side widgets.

## Layout

Desktop uses a two-column feed workspace: a compact left rail for composing and session actions, plus a wider central feed column for search and posts. The composer may stay sticky on desktop so posting remains accessible while reading. On mobile, the layout collapses to a single column with the composer above the feed.

## Components

`Feed.razor` should keep existing API calls and state flow. Markup changes should be limited to semantic grouping and class names needed for visual structure. No new feed data should be invented.

`app.css` should carry the bulk of the work through design tokens, card styling, typography, responsive layout, and component states.

`PostMediaGrid.razor` can remain behaviorally unchanged. Its existing classes should be styled to match the new dark card system.

## Testing

Add static web configuration tests that pin the redesign requirements most likely to regress:

- The stylesheet opts into dark color scheme.
- The feed layout defines a wider feed column.
- Posts have standalone card styling instead of only a top divider.
- The composer has dedicated dashboard styling.

Run the web test project plus the repository's required Docker Compose checks for Web changes.
