<h1 align="center">Jellyfin Custom Logo Plugin</h1>

<p align="center">
Replace the default Jellyfin branding — splash logo, header logo, header text, admin drawer logo,
favicon and browser tab title — with your own, configured entirely from the admin dashboard.
</p>

---

## What it does

| Target | Element replaced |
| --- | --- |
| Splash / loading logo | `.splashLogo` — the large logo shown while the web client boots |
| Header logo | `.pageTitleWithDefaultLogo`, including `.layout-tv` |
| Header text | Rendered via `.pageTitleWithDefaultLogo::after`, optionally hidden on narrow screens |
| Admin drawer logo | `.adminDrawerLogo img` |
| Favicon | `<link rel="icon">`, `shortcut icon`, `apple-touch-icon`, `msapplication-TileImage` |
| Browser tab title | `<title>` and `<meta name="application-name">` |

Each target can be switched on individually, or you can flip the whole plugin between
**all logos**, **only the ones you select**, and **nothing at all**.

## How it works, and why it does it this way

The obvious way to restyle Jellyfin is the built-in **Dashboard → General → Custom CSS** box. That
CSS is fetched by JavaScript *after* the web client has already booted, which is exactly why the
default Jellyfin logo flashes on screen for a moment before your own logo replaces it.

This plugin avoids that by writing the branding into the HTML document itself, before the browser
ever sees it:

1. `PluginServiceRegistrator` registers an [`IStartupFilter`][startupfilter] with Jellyfin's DI
   container. Jellyfin calls plugin service registrators from `ApplicationHost.Init(IServiceCollection)`
   during the host's `ConfigureServices`, so the registration lands in the same container ASP.NET Core
   later reads startup filters from.
2. ASP.NET Core applies that filter around `Startup.Configure`, which places
   `CustomLogoMiddleware` at the very front of the request pipeline — ahead of `UseStaticFiles`,
   which is what normally serves `index.html` straight off disk.
3. For `GET /web/` and `GET /web/index.html`, the middleware reads the real `index.html`, injects a
   `<style>` block as the last element in `<head>`, rewrites the icon and title tags, and returns
   the result. Everything else falls straight through untouched.

Because the CSS is parsed as part of `<head>`, it applies on the **first paint**. There is no
window in which the default logo can appear.

Two consequences worth knowing:

- **Nothing on disk is modified.** Some older plugins patch `jellyfin-web/index.html` in place. That
  breaks on every web client update and fails outright on read-only container images. This plugin
  holds the branded copy in memory only.
- **No third-party plugin is required.** The community [File Transformation][filetransformation]
  plugin solves a similar problem, but it is a separate install and registers callbacks by
  reflection. Jellyfin 10.11 has no native web-file transformation API of its own — `IStartupFilter`
  is a stock ASP.NET Core extension point that happens to be reachable from a Jellyfin plugin.

Uploaded images are embedded into the page as `data:` URIs rather than served from a plugin
endpoint. That removes an extra HTTP round trip, which matters here: an image still being fetched is
an image not yet painted.

The result is cached and only rebuilt when the plugin configuration or the on-disk `index.html`
changes.

## Installation

### From the plugin repository (recommended)

1. In Jellyfin go to **Dashboard → Plugins → Repositories** and add:

   ```
   https://raw.githubusercontent.com/WimWamWom/jellyfin-plugin-custom-logo/main/manifest.json
   ```

2. Open **Dashboard → Plugins → Catalog**, find **Custom Logo**, and install it.
3. Restart Jellyfin.

> The manifest starts out with an empty `versions` list. It is filled in automatically by the
> release workflow the first time you push a `v*` tag (see [Releasing](#releasing)).

### Manually

1. Download `custom-logo_<version>.zip` from the [releases page][releases].
2. Extract it into `<jellyfin-data>/plugins/Custom Logo/`.
3. Restart Jellyfin.

## Configuration

**Dashboard → Plugins → Custom Logo**.

| Setting | Notes |
| --- | --- |
| Replacement mode | `All logos`, `Only the logos I select`, or `Nothing`. `Nothing` leaves the served page untouched without uninstalling the plugin. |
| Individual logos | Splash, header, drawer, favicon and tab title toggles. Only consulted in `Only the logos I select` mode. |
| Logo source | An external URL, or a file uploaded here (max 2 MB). |
| Header text | Drawn next to the header logo and used as the tab title. |
| Appearance | Logo height, text colour, text size, text weight — all plain CSS values. |
| Favicon | Uses the main logo unless you tick "use a different image". |

Settings apply on the next full page load — reload the web client with <kbd>Ctrl</kbd>+<kbd>F5</kbd>
(<kbd>Cmd</kbd>+<kbd>Shift</kbd>+<kbd>R</kbd> on macOS).

A logo URL is loaded by the **browser**, not by the server, so it has to be reachable from your
clients. If you want a logo that works everywhere including outside your network, upload the file
instead.

### Notes on specific targets

- **Browser tab title.** The web client rewrites `document.title` as you navigate, so replacing
  `<title>` mainly affects the initial load and the tab title before the app takes over. Replacing it
  everywhere would mean fighting the client's own routing on every navigation, which is not
  something a branding plugin should do.
- **Header text on mobile.** Below `50em` the text is hidden by default and the header collapses to
  just the logo, matching Jellyfin's own narrow layout.

## Security

Every administrator-supplied value is escaped before it reaches the page. Text and URLs going into
CSS are escaped as CSS strings (including `<` and `>`, so a value cannot close the `<style>`
element); bare CSS values such as lengths and colours are validated against an allowlist and fall
back to their defaults if they contain anything that could terminate a declaration; values going
into HTML attributes are HTML-encoded. Upload and delete endpoints require the `RequiresElevation`
policy — the same privilege level as the rest of the dashboard.

## Compatibility

Built against the **Jellyfin 10.11** plugin ABI (`targetAbi 10.11.0.0`, .NET 9). The project
references `Jellyfin.Controller` / `Jellyfin.Model` 10.11.0 — the lowest 10.11 patch — so the
assembly stays loadable across the whole 10.11.x line.

**On Jellyfin 12.0.0 (in development):** expect this to need work before it loads. Jellyfin enforces
plugin ABI per release, so the `targetAbi` and package references have to be bumped regardless. The
parts most likely to actually break:

- `Startup.Configure` serves the web client with `UseStaticFiles` over a `PhysicalFileProvider` at
  `/web`. If 12.0.0 reworks how the web client is hosted, the path match in `CustomLogoMiddleware`
  needs revisiting.
- The CSS selectors (`.splashLogo`, `.pageTitleWithDefaultLogo`, `.adminDrawerLogo`) are
  jellyfin-web class names, not a stable API. A web client rewrite can rename them.
- If 12.0.0 introduces a native web-file transformation service, prefer it over the
  `IStartupFilter` approach.

This is deliberately built only against stable 10.11 APIs — nothing here depends on pre-release
Jellyfin code.

## Building

Requires the [.NET 9 SDK][dotnet].

```bash
dotnet build Jellyfin.Plugin.CustomLogo.sln --configuration Release
```

The plugin assembly lands in `Jellyfin.Plugin.CustomLogo/bin/Release/net9.0/`. Copy
`Jellyfin.Plugin.CustomLogo.dll` into a folder under your server's `plugins/` directory and restart.

Only `Jellyfin.Plugin.CustomLogo.dll` ships in the release zip — the Jellyfin and ASP.NET Core
assemblies are provided by the server at runtime.

### Releasing

Push a tag and the release workflow does the rest:

```bash
git tag v1.0.0.0
git push origin v1.0.0.0
```

It builds the plugin, publishes a GitHub release with the zip attached, then appends the version and
its MD5 checksum to `manifest.json` and commits that back to `main`.

## Migrating from a custom-CSS block

If you currently do this by hand in **Dashboard → General → Custom CSS**, move the values into the
plugin's settings and delete the CSS block. The plugin generates the equivalent rules itself, and
serves them early enough that the splash logo no longer flashes.

One fix worth mentioning if you are porting a hand-written block: the common version of this snippet
sets `padding-left: var(--logoBreite)` inside the mobile media query while only ever defining
`--logoGroesse`. That reference resolves to nothing. The plugin uses a single `--customlogo-size`
property for both.

## License

[GPL-3.0](LICENSE), matching [jellyfin-plugin-template][template].

[startupfilter]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup#extend-startup-with-startup-filters
[filetransformation]: https://github.com/IAmParadox27/jellyfin-plugin-file-transformation
[template]: https://github.com/jellyfin/jellyfin-plugin-template
[releases]: https://github.com/WimWamWom/jellyfin-plugin-custom-logo/releases
[dotnet]: https://dotnet.microsoft.com/download
