using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.CustomLogo.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CustomLogo.Injection;

/// <summary>
/// Applies the configured branding to the web client's <c>index.html</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is a pure transformation over HTML that has already been produced by the rest of the
/// pipeline. It deliberately does not read <c>index.html</c> from disk: other plugins (notably
/// File Transformation, which the Media Bar and Home Screen Sections plugins build on) replace the
/// static file provider so that the file is rewritten as it is read. Reading from disk here would
/// silently discard their work.
/// </para>
/// <para>
/// The last result is cached, so repeated page loads with unchanged input and configuration cost
/// one string comparison instead of a set of regular expression passes.
/// </para>
/// </remarks>
internal sealed partial class IndexHtmlTransformer
{
    private const string StyleElementId = "jellyfin-custom-logo";

    private readonly ILogger<IndexHtmlTransformer> _logger;
    private readonly object _syncRoot = new();

    private string? _cachedConfigKey;
    private string? _cachedInput;
    private string? _cachedOutput;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexHtmlTransformer"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public IndexHtmlTransformer(ILogger<IndexHtmlTransformer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Gets a value indicating whether the plugin currently has anything to inject.
    /// </summary>
    /// <returns>
    /// <c>false</c> when the plugin is unconfigured or switched off, in which case the caller
    /// should not bother buffering the response at all.
    /// </returns>
    public bool IsEnabled()
    {
        var plugin = Plugin.Instance;
        return plugin is not null && plugin.Configuration.Mode != LogoReplacementMode.None;
    }

    /// <summary>
    /// Applies the branding to the supplied markup.
    /// </summary>
    /// <param name="html">The <c>index.html</c> produced by the rest of the pipeline.</param>
    /// <returns>The branded markup, or <c>null</c> when there was nothing to change.</returns>
    public string? Transform(string html)
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return null;
        }

        var config = plugin.Configuration;
        if (config.Mode == LogoReplacementMode.None)
        {
            return null;
        }

        var configKey = BuildConfigKey(config, plugin);

        lock (_syncRoot)
        {
            if (string.Equals(_cachedConfigKey, configKey, StringComparison.Ordinal)
                && _cachedInput is not null
                && string.Equals(_cachedInput, html, StringComparison.Ordinal))
            {
                return _cachedOutput;
            }
        }

        var output = Build(html, config, plugin);

        lock (_syncRoot)
        {
            _cachedConfigKey = configKey;
            _cachedInput = html;
            _cachedOutput = output;
        }

        return output;
    }

    [GeneratedRegex(
        "<link\\b[^>]*\\brel=[\"'](shortcut icon|icon|apple-touch-icon)[\"'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex IconLinkRegex();

    [GeneratedRegex(
        "<meta\\b[^>]*\\bname=[\"']msapplication-TileImage[\"'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex TileImageRegex();

    [GeneratedRegex(
        "<title>.*?</title>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(
        "<meta\\b[^>]*\\bname=[\"']application-name[\"'][^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ApplicationNameRegex();

    /// <summary>
    /// Escapes a value for use inside a double quoted HTML attribute.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The escaped value.</returns>
    private static string EncodeAttribute(string value)
    {
        var sb = new StringBuilder(value.Length + 16);
        foreach (var c in value)
        {
            switch (c)
            {
                case '&':
                    sb.Append("&amp;");
                    break;
                case '<':
                    sb.Append("&lt;");
                    break;
                case '>':
                    sb.Append("&gt;");
                    break;
                case '"':
                    sb.Append("&quot;");
                    break;
                case '\'':
                    sb.Append("&#39;");
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds a key that changes whenever the configuration or an uploaded image changes.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="plugin">The plugin instance.</param>
    /// <returns>The cache key.</returns>
    private static string BuildConfigKey(PluginConfiguration config, Plugin plugin)
    {
        var sb = new StringBuilder(256);

        sb.Append(config.Mode.ToString()).Append('|')
          .Append(config.ReplaceSplashLogo).Append(config.ReplaceHeaderLogo)
          .Append(config.ReplaceDrawerLogo).Append(config.ReplaceFavicon)
          .Append(config.ReplaceBrowserTitle).Append('|')
          .Append(config.LogoSource.ToString()).Append('|')
          .Append(config.LogoUrl).Append('|')
          .Append(config.UseSeparateFavicon).Append('|')
          .Append(config.FaviconSource.ToString()).Append('|')
          .Append(config.FaviconUrl).Append('|')
          .Append(config.HeaderText).Append('|')
          .Append(config.ShowHeaderText).Append(config.HideHeaderTextOnMobile).Append('|')
          .Append(config.HeaderLogoSize).Append('|')
          .Append(config.HeaderTextColor).Append('|')
          .Append(config.HeaderTextFontSize).Append('|')
          .Append(config.HeaderTextFontWeight).Append('|')
          .Append(GetStamp(plugin.GetLogoStoragePath())).Append('|')
          .Append(GetStamp(plugin.GetFaviconStoragePath()));

        return sb.ToString();
    }

    /// <summary>
    /// Gets a change stamp for an uploaded image, so replacing an upload busts the cache.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A stamp string.</returns>
    private static string GetStamp(string path)
    {
        var info = new FileInfo(path);
        return info.Exists
            ? string.Concat(
                info.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture),
                ":",
                info.Length.ToString(CultureInfo.InvariantCulture))
            : "none";
    }

    private string? Build(string html, PluginConfiguration config, Plugin plugin)
    {
        var logoUrl = BrandingAssetResolver.ResolveLogo(config, plugin, _logger);
        var changed = false;

        var css = CustomCssBuilder.Build(config, logoUrl);
        if (css.Length > 0)
        {
            // Injected as the last thing in <head>, so it is parsed before the body is rendered and
            // before the webpack bundle's stylesheet is fetched. The rules use !important, which
            // beats the later-loading bundle and theme rules of equal specificity.
            var index = html.LastIndexOf("</head>", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                html = html.Insert(
                    index,
                    string.Concat("<style id=\"", StyleElementId, "\">", css, "</style>"));
                changed = true;
            }
            else
            {
                _logger.LogWarning("No closing head tag found in the served index; skipping style injection");
            }
        }

        if (config.IsFaviconEnabled())
        {
            var faviconUrl = BrandingAssetResolver.ResolveFavicon(config, plugin, logoUrl, _logger);
            if (!string.IsNullOrEmpty(faviconUrl))
            {
                var encoded = EncodeAttribute(faviconUrl);

                html = IconLinkRegex().Replace(
                    html,
                    match => string.Concat("<link rel=\"", match.Groups[1].Value, "\" href=\"", encoded, "\">"));

                html = TileImageRegex().Replace(
                    html,
                    _ => string.Concat("<meta name=\"msapplication-TileImage\" content=\"", encoded, "\">"));

                changed = true;
            }
        }

        if (config.IsBrowserTitleEnabled() && !string.IsNullOrWhiteSpace(config.HeaderText))
        {
            var title = EncodeAttribute(config.HeaderText.Trim());

            html = TitleRegex().Replace(html, _ => string.Concat("<title>", title, "</title>"));
            html = ApplicationNameRegex().Replace(
                html,
                _ => string.Concat("<meta name=\"application-name\" content=\"", title, "\">"));

            changed = true;
        }

        return changed ? html : null;
    }
}
