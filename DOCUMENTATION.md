# Custom Logo Documentation

Detailed documentation for the [Jellyfin Custom Logo Plugin](README.md).

- [How it works, and why it does it this way](#how-it-works-and-why-it-does-it-this-way)
- [Configuration reference](#configuration-reference)
- [Working alongside your own custom CSS](#working-alongside-your-own-custom-css)
- [The catalog icon](#the-catalog-icon)
- [Working alongside other plugins](#working-alongside-other-plugins)
- [Migrating from a hand-written CSS block](#migrating-from-a-hand-written-css-block)
- [Security](#security)
- [Compatibility](#compatibility)
- [Building](#building)
- [Releasing](#releasing)

## How it works, and why it does it this way

The obvious way to restyle Jellyfin is the built-in **Dashboard → General → Custom CSS** box. That
CSS is fetched by JavaScript *after* the web client has already booted, which is exactly why the
default Jellyfin logo flashes on screen for a moment before your own logo replaces it.

This plugin avoids that by writing the branding into the HTML document itself, before the browser
ever sees it:

1. `PluginServiceRegistrator` registers an [`IStartupFilter`][startupfilter] with Jellyfin's DI
   container. Jellyfin calls plugin service registrators from `ApplicationHost.Init(IServiceCollection)`
   during the host's `ConfigureServices`, so the registration lands in the same container ASP.NET
   Core later reads startup filters from.
2. ASP.NET Core applies that filter around `Startup.Configure`, which places `CustomLogoMiddleware`
   at the very front of the request pipeline, ahead of `UseStaticFiles`, which is what normally
   serves `index.html` off disk.
3. For `GET /web/` and `GET /web/index.html`, the middleware buffers the response, lets the rest of
   the pipeline produce the page, and only then injects a `<style>` block as the last element in
   `<head>` and rewrites the icon and title tags. Everything else falls straight through untouched.

Because the CSS is parsed as part of `<head>`, it applies on the **first paint**. There is no window
in which the default logo can appear.

Three consequences worth knowing:

- **Nothing on disk is modified.** Some older plugins patch `jellyfin-web/index.html` in place. That
  breaks on every web client update and fails outright on read-only container images. This plugin
  holds the branded copy in memory only.
- **No third-party plugin is required.** The community [File Transformation][filetransformation]
  plugin solves a similar problem, but it is a separate install and registers callbacks by
  reflection. Jellyfin 10.11 has no native web-file transformation API of its own. `IStartupFilter`
  is a stock ASP.NET Core extension point that happens to be reachable from a Jellyfin plugin.
- **Uploads are inlined.** Uploaded images are embedded as `data:` URIs rather than served from a
  plugin endpoint. That removes an extra HTTP round trip, which matters here: an image still being
  fetched is an image not yet painted.

The result is cached and only rebuilt when the plugin configuration or the incoming markup changes.

### Buffering details

Buffering the response correctly needs three things that are easy to get wrong:

- The whole `IHttpResponseBodyFeature` is replaced, not just `Response.Body`. The static file
  middleware serves physical files with `SendFileAsync`, which bypasses `Response.Body` entirely.
- `Accept-Encoding` is dropped for the request. Response compression sits *downstream* of this
  middleware, so otherwise the buffer would contain gzip rather than HTML.
- `If-None-Match` and `If-Modified-Since` are dropped, because a `304` carries no body to rewrite.
  For the same reason `ETag` and `Last-Modified` are stripped from the response: the body no longer
  matches the file on disk, so those validators would be wrong.

## Configuration reference

Open **Custom Logo** in the dashboard's left-hand navigation, or go through
**Dashboard → Plugins → Custom Logo**.

| Setting | Notes |
| --- | --- |
| Replacement mode | `All logos`, `Only the logos I select`, or `Nothing`. `Nothing` leaves the served page untouched without uninstalling the plugin. |
| Individual logos | Splash, header, drawer and favicon toggles. Only consulted in `Only the logos I select` mode. |
| Logo source | An external URL, or a file uploaded here (max 2 MB). |
| Header text | Drawn next to the header logo. |
| Appearance | Logo height, text height, text colour, text weight: all plain CSS values. Logo and text height are independent; the text's width always follows its content. |
| Favicon | Uses the main logo unless you tick "use a different image". |

Settings apply on the next full page load. Reload the web client with <kbd>Ctrl</kbd>+<kbd>F5</kbd>
(<kbd>Cmd</kbd>+<kbd>Shift</kbd>+<kbd>R</kbd> on macOS).

A logo URL is loaded by the **browser**, not by the server, so it has to be reachable from your
clients. If you want a logo that works everywhere including outside your network, upload the file
instead.

### Notes on specific targets

- **The browser tab title is out of scope.** It is not driven by the HTML: `libraryMenu.js` overwrites
  `document.title` with `ServerName` as soon as the client boots, which server-side is
  `ConfigurationManager.Configuration.ServerName`. Set it under
  **Dashboard → General → Server name**. Jellyfin already handles this, so the plugin does not touch
  it.
- **Header text placement.** The header logo and text only appear on pages without their own title.
  Jellyfin's `setTitle()` removes the logo classes and writes the page title instead, so on most
  subpages you see that title rather than your branding. That is stock behaviour.
- **Header text on mobile.** Below `50em` the text is hidden by default and the header collapses to
  just the logo, matching Jellyfin's own narrow layout.

## Working alongside your own custom CSS

The plugin marks exactly one thing `!important`: the `background-image` on
`.pageTitleWithDefaultLogo`. That one has to be forced, because Jellyfin's themes are applied after
the page loads and every stock theme sets a logo on precisely that selector.

Everything else is plain, unforced CSS, but the header rules are written as
`.pageTitleWithDefaultLogo.pageTitleWithLogo`, using two classes on purpose. jellyfin-web always
applies both together, and the two-class specificity beats the web client's own `.pageTitle` (which
sets `height: 1.7em`) and `.pageTitleWithLogo` (which sets `width: 13.2em`) regardless of stylesheet
order. Document order is not safe to rely on here: with code splitting, bundle CSS can be inserted at
runtime and therefore *after* the plugin's block. When those rules lost, the box stayed at the stock
height and clipped the logo top and bottom.

Your own `!important` still wins over all of it, since importance beats specificity.

The plugin exposes its values as custom properties, which is usually the tidiest way to adjust it:

```css
:root {
    --customlogo-size: 2em;          /* header logo height */
    --customlogo-text-size: 1.8em;   /* header text height */
    --customlogo-text-offset: 8em;   /* gap before the header text, widen for banner logos */
    --customlogo-header-text: "My Server";
}
```

`--customlogo-text-size` is the header text height, set independently of the logo height. There is
deliberately no fixed ratio between the two, since they are tuned by eye. The text's width is never
set: the header box is `width: auto` and the text is `white-space: nowrap`, so it always grows to fit.

`--customlogo-text-offset` defaults to the logo height, which suits roughly square logos. A wide
banner logo is drawn wider than it is tall, so increase this until the text clears it.

If you previously replaced the logos with a hand-written CSS block, delete the logo rules from it;
otherwise the two fight over the same selectors. Keep any unrelated custom CSS as it is.

## The catalog icon

`manifest.json` carries an `imageUrl`. Jellyfin does **not** hand that URL straight to the browser
for installed plugins. `PluginManager.PopulateManifest` downloads the image *server-side* while the
plugin is being installed or updated, stores it next to the plugin, records it as `ImagePath` in the
plugin's `meta.json`, and the web client then loads it from `/Plugins/{id}/{version}/Image`. Only for
plugins that are not installed does the catalog use `imageUrl` directly.

Two things follow from that:

- **Adding or changing `imageUrl` has no effect on an already installed version.** The image is only
  fetched during install/update, so the icon appears after the next version is installed.
- **The filename matters.** Jellyfin derives it from the last URL segment, so the URL has to end in a
  real file name (`.../static/logo.png`). A URL ending in a slash or a bare query string will not
  produce a usable file.

Any host works: `raw.githubusercontent.com` is fine and needs no CDN, since the server does the
fetching.

## Working alongside other plugins

Buffering rather than short-circuiting is deliberate. [File Transformation][filetransformation],
which [Media Bar][mediabar] and Home Screen Sections build on, replaces the static file provider so
that `index.html` is rewritten as it is read. Serving our own copy straight from disk would silently
discard all of that and break those plugins. Instead this plugin brands whatever the pipeline hands
it, on top of everyone else's changes.

Ordering is therefore not something you need to configure: whatever produces `index.html`, the
branding is applied to the finished result.

## Migrating from a hand-written CSS block

Move the values into the plugin's settings and delete the logo rules from your custom CSS. The plugin
generates the equivalent rules itself, and serves them early enough that the splash logo no longer
flashes.

One fix worth mentioning if you are porting a hand-written block: the commonly shared version of this
snippet sets `padding-left: var(--logoBreite)` inside the mobile media query while only ever defining
`--logoGroesse`. That reference resolves to nothing. The plugin uses a single `--customlogo-size`
property for both.

## Security

Every administrator-supplied value is escaped before it reaches the page:

- Text and URLs going into CSS are escaped as CSS strings, including `<` and `>`, so a value cannot
  close the `<style>` element.
- Bare CSS values such as lengths and colours are validated against an allowlist and fall back to
  their defaults if they contain anything that could terminate a declaration.
- Values going into HTML attributes are HTML-encoded.

Upload and delete endpoints require the `RequiresElevation` policy, the same privilege level as the
rest of the dashboard.

## Compatibility

Built against the **Jellyfin 10.11** plugin ABI (`targetAbi 10.11.0.0`, .NET 9). The project
references `Jellyfin.Controller` / `Jellyfin.Model` 10.11.0, the lowest 10.11 patch, so the
assembly stays loadable across the whole 10.11.x line.

**On Jellyfin 12.0.0 (in development):** expect this to need work before it loads. Jellyfin enforces
plugin ABI per release, so the `targetAbi` and package references have to be bumped regardless. The
parts most likely to actually break:

- `Startup.Configure` serves the web client with `UseStaticFiles` over a `PhysicalFileProvider` at
  `/web`. If 12.0.0 reworks how the web client is hosted, the path match in `CustomLogoMiddleware`
  needs revisiting.
- The CSS selectors (`.splashLogo`, `.pageTitleWithDefaultLogo`, `.adminDrawerLogo`) are
  jellyfin-web class names, not a stable API. A web client rewrite can rename them.
- If 12.0.0 introduces a native web-file transformation service, prefer it over the `IStartupFilter`
  approach.

This is deliberately built only against stable 10.11 APIs. Nothing here depends on pre-release
Jellyfin code.

## Building

Requires the [.NET 9 SDK][dotnet].

```bash
dotnet build Jellyfin.Plugin.CustomLogo.sln --configuration Release
```

The plugin assembly lands in `Jellyfin.Plugin.CustomLogo/bin/Release/net9.0/`. Copy
`Jellyfin.Plugin.CustomLogo.dll` into a folder under your server's `plugins/` directory and restart.

Only `Jellyfin.Plugin.CustomLogo.dll` ships in the release zip: the Jellyfin and ASP.NET Core
assemblies are provided by the server at runtime.

## Releasing

Changelog entries are shown to users inside Jellyfin, so keep them to what changes about the plugin
itself. Repository and build changes (workflows, packaging, documentation) belong in the commit
history rather than in the catalog.

1. Add a section for the new version at the top of [CHANGELOG.md](CHANGELOG.md), using the same
   four-part version you are about to tag:

   ```markdown
   ## 1.0.0.3

   **Fixed**

   - ...
   ```

2. Commit it, then tag and push:

   ```bash
   git tag v1.0.0.3
   git push origin v1.0.0.3
   ```

The workflow builds the plugin, extracts that changelog section, publishes a GitHub release with the
zip attached and the section as the release body, then appends the version, its MD5 checksum and the
same changelog to `manifest.json` and commits that back to `main`. Only after that does the repository
URL actually offer something installable.

If no matching section exists the release still succeeds, but the changelog falls back to a bare
`Release x.y.z.` stub and the job log carries a warning. Jellyfin renders the changelog as Markdown,
so headings and lists work in the plugin catalog.

Wait for the build workflow to go green before tagging; otherwise the release workflow fails on the
same error and you end up with a tag and no release.

[startupfilter]: https://learn.microsoft.com/en-us/aspnet/core/fundamentals/startup#extend-startup-with-startup-filters
[filetransformation]: https://github.com/IAmParadox27/jellyfin-plugin-file-transformation
[mediabar]: https://github.com/IAmParadox27/jellyfin-plugin-media-bar
[dotnet]: https://dotnet.microsoft.com/download
