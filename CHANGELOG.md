# Changelog

Changes to the plugin, as users experience them. Heading format: `## 1.0.0.3`, matching the tag
`v1.0.0.3`. See [DOCUMENTATION.md](DOCUMENTATION.md#releasing) for how this file is published.

> Entries describe the plugin only: what changes for someone running it. Repository and build
> details (workflows, packaging, docs) belong in the commit history, not here: these sections are
> shown to users inside Jellyfin.

## 1.0.0.6

**Added**

- Header logo and text are now also replaced in the default ("Modern") layout's toolbar. Previously
  only the TV layout and the legacy desktop/mobile layouts were covered, because the classic header
  they use is present but hidden behind the new default layout.

**Changed**

- Now targets Jellyfin 12.0 (`net10.0`, plugin ABI `12.0.0.0`). This is a hard cutover: this version
  no longer loads on Jellyfin 10.11.x. If you are still on 10.11, stay on 1.0.0.5.

**Fixed**

- The logo and favicon preview images on the configuration page loaded a broken-image icon instead of
  the upload, because Jellyfin 12 rejects the query parameter they used to authenticate.

## 1.0.0.5

**Changed**

- Clearing an appearance field now hands that detail back to Jellyfin instead of applying a value
  chosen by the plugin, and every field says so. Empty the logo height and the header keeps Jellyfin's
  own height; empty the text colour and it keeps Jellyfin's colour. Clear the logo itself and the
  plugin stays out of the page entirely.

**Fixed**

- A failure while applying the branding can no longer stop the web client from loading. The page is
  served unbranded instead and the error is written to the server log.
- Requests other than the web client page are now dismissed by a plain path check. Every request,
  including media streaming, previously read the network configuration first.

## 1.0.0.4

**Changed**

- The header text is no longer semi-bold. It looked heavy and blocky next to the logo, so the default
  weight is now normal (`400`); `600` is still available in the text weight field.
- New default sizes: logo height `2em`, text height `1.8em`. Settings you have already saved are left
  untouched.

**Fixed**

- Size fields now accept a plain number and read it as `em`. Previously a value like `2` was not a
  valid CSS length, so the browser discarded it and the setting silently did nothing.

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

- The header text is now centred properly next to the logo; its line height no longer nudges it off
  centre.

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
