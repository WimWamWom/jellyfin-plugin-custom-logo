# Changelog

Changes to the plugin, as users experience them. Heading format: `## 1.0.0.3`, matching the tag
`v1.0.0.3`. See [DOCUMENTATION.md](DOCUMENTATION.md#releasing) for how this file is published.

> Entries describe the plugin only: what changes for someone running it. Repository and build
> details (workflows, packaging, docs) belong in the commit history, not here: these sections are
> shown to users inside Jellyfin.

## 2.0.0.2

**Fixed**

- **Header logo height** now works in the default ("Modern") layout. It only ever sized the classic
  header, so in the layout most people actually see, the field appeared to do nothing at all.
- A logo that is wider than it is tall is no longer squeezed into a square there. The web client caps
  its header icon at 1.25em in both directions, which suits Jellyfin's own square icon and left every
  wider logo at a fraction of its proper height. That cap on the width is lifted, so the logo keeps
  its shape at whatever height it is given, exactly as the classic header has always drawn it.
  Both reported by @ToTheXtreme64 in
  [#1](https://github.com/WimWamWom/jellyfin-plugin-custom-logo/issues/1).

## 2.0.0.1

**Added**

- The header text is now a choice of three: **Default text** keeps whatever Jellyfin puts next to the
  logo, **My custom text** draws your own, and **No text** removes it altogether. Only the default
  ("Modern") layout has any text of its own there — the server name on the header button — so in the
  classic, TV and legacy layouts the first and last option look the same.
- With **No text**, the logo takes the whole clickable header button. The gap the web client reserves
  between the logo and the server name goes away with the text instead of being left standing empty.
  Requested by @ToTheXtreme64 in
  [#1](https://github.com/WimWamWom/jellyfin-plugin-custom-logo/issues/1).

**Changed**

- "Hide the header text on narrow screens" now applies to the default text as well as to your own.
  If you have a logo configured and no custom text, the server name is hidden below 50em from now on,
  the same way your own text always was. Untick the box to keep it at every width.
- The "Show the header text next to the logo" tick box is gone, replaced by the choice above. It only
  ever suppressed your own text, never Jellyfin's, so there was no way to ask for an empty header.
- Existing configurations keep rendering exactly as they do today. A header text that was configured
  and shown becomes **My custom text**; everything else becomes **Default text**, including an
  unticked box, which never emptied the header in the first place.

## 2.0.0.0

**Changed**

- Verified against the final Jellyfin 12.0 release. Nothing changed in the plugin itself; 1.0.0.7
  already runs on it unmodified.

## 1.0.0.7

**Changed**

- A fresh install now starts with the appearance fields empty, so logo height, text colour, text
  height and text weight are all Jellyfin's own until you set them. Previously it arrived with
  `2em` / `#fff` / `1.8em` / `400` filled in, which quietly restyled the header the moment a logo was
  configured. Existing installations keep the values they have saved; clear a field to hand that
  detail back.
- With no text colour configured, the header text in the default ("Modern") layout now follows the
  web client's own colour instead of being forced to white, which was hard to read on light themes.

**Fixed**

- In the default ("Modern") layout, the header text now sits directly next to the logo. It was
  pushed to the right by a gap as wide as the server name, because the button's own text was only
  made invisible and still took up its space.

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
