# Changelog

Each `## <version>` section below becomes both the GitHub release body and the changelog Jellyfin
shows under **Revisions** on the plugin page. Jellyfin renders it as Markdown.

Use the same four-part version you tag with, e.g. `## 1.0.0.3` for tag `v1.0.0.3`. Add the section
*before* pushing the tag — the release workflow reads it from here.

## 1.0.0.3

**Changed**

- Larger defaults, so the header fits without tweaking: logo height `2.2em` (was `1.8em`) and text
  height `1.5em` (was `1.1em`). Settings you have already saved are left untouched.
- **Header text size** is now labelled **Header text height** and states that it is set independently
  of the logo height. Its width follows the text automatically, as before.

**Added**

- `--customlogo-text-size` custom property, so the header text height can also be overridden from your
  own custom CSS.

**Fixed**

- The header text is now centred properly next to the logo — its line height no longer nudges it off
  centre.
- Release notes are no longer a bare "Release x.y.z." stub. They are written in `CHANGELOG.md` and used
  for both the GitHub release and the changelog shown in the plugin catalog.

## 1.0.0.2

**Fixed**

- The header logo was clipped at the top and bottom, and the header text sat in the wrong place. The
  layout rules now win by CSS specificity instead of depending on stylesheet order, which is not
  reliable because Jellyfin can load its own styles after the plugin's.
- The logo now scales to fit its box, so it can no longer be clipped.

**Changed**

- Default header logo height lowered to `1.8em`. Existing settings are left untouched.
- New `--customlogo-text-offset` variable to widen the gap before the header text, for wide
  banner-style logos.

**Removed**

- The browser tab title option. It could never work: Jellyfin overwrites the tab title with the server
  name immediately after loading. Use *Dashboard → General → Server name* instead.

## 1.0.0.1

**Fixed**

- No longer breaks plugins that build on File Transformation, such as Media Bar. The branding is now
  applied on top of other plugins' changes instead of replacing the page outright.

**Added**

- Entry in the dashboard's left-hand navigation.
- Plugin icon in the catalog.

**Changed**

- Only the logo image itself is forced with `!important` now, so your own custom CSS keeps control over
  the header layout.

## 1.0.0.0

Initial release.

- Replaces the splash, header, admin drawer and favicon branding with your own logo.
- Custom header text next to the logo, optionally hidden on narrow screens.
- Logo from an external URL, or uploaded straight from the dashboard.
- Choose between replacing all logos, only selected ones, or nothing at all.
- The branding is injected into the page server-side, so there is no flash of the default Jellyfin logo
  while loading.
