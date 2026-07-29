using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Jellyfin.Plugin.CustomLogo.Configuration;
using MediaBrowser.Controller.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CustomLogo.Injection;

/// <summary>
/// Produces the branded version of the web client's <c>index.html</c>.
/// </summary>
/// <remarks>
/// The result is cached and only rebuilt when either the on-disk <c>index.html</c> or the plugin
/// configuration changes, so a page load costs one dictionary-sized string comparison rather than a
/// file read plus a set of regular expression passes.
/// </remarks>
internal sealed partial class IndexHtmlTransformer
{
    private const string StyleElementId = "jellyfin-custom-logo";

    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly ILogger<IndexHtmlTransformer> _logger;
    private readonly object _syncRoot = new();

    private string? _cachedKey;
    private string? _cachedHtml;

    /// <summary>
    /// Initializes a new instance of the <see cref="IndexHtmlTransformer"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">The server configuration manager.</param>
    /// <param name="logger">The logger.</param>
    public IndexHtmlTransformer(
        IServerConfigurationManager serverConfigurationManager,
        ILogger<IndexHtmlTransformer> logger)
    {
        _serverConfigurationManager = serverConfigurationManager;
        _logger = logger;
    }

    /// <summary>
    /// Gets the branded <c>index.html</c>.
    /// </summary>
    /// <returns>
    /// The transformed HTML, or <c>null</c> when the plugin is disabled, nothing is configured, or
    /// the file could not be read. In every <c>null</c> case the caller must fall through to the
    /// server's normal static file handling.
    /// </returns>
    public string? GetTransformedHtml()
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

        var webPath = _serverConfigurationManager.ApplicationPaths.WebPath;
        if (string.IsNullOrEmpty(webPath))
        {
            return null;
        }

        var indexPath = Path.Combine(webPath, "index.html");
        var indexInfo = new FileInfo(indexPath);
        if (!indexInfo.Exists)
        {
            return null;
        }

        var key = BuildCacheKey(config, plugin, indexInfo);

        lock (_syncRoot)
        {
            if (string.Equals(_cachedKey, key, StringComparison.Ordinal))
            {
                return _cachedHtml;
            }
        }

        var html = Transform(indexPath, config, plugin);

        lock (_syncRoot)
        {
            _cachedKey = key;
            _cachedHtml = html;
        }

        return html;
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
    /// Builds a key that changes whenever anything the output depends on changes.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="indexInfo">The on-disk index file.</param>
    /// <returns>The cache key.</returns>
    private static string BuildCacheKey(PluginConfiguration config, Plugin plugin, FileInfo indexInfo)
    {
        var sb = new StringBuilder(256);

        sb.Append(indexInfo.LastWriteTimeUtc.Ticks.ToString(CultureInfo.InvariantCulture)).Append('|')
          .Append(indexInfo.Length.ToString(CultureInfo.InvariantCulture)).Append('|')
          .Append(config.Mode.ToString()).Append('|')
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

    private string? Transform(string indexPath, PluginConfiguration config, Plugin plugin)
    {
        string html;
        try
        {
            html = File.ReadAllText(indexPath);
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to read the web client index at {Path}", indexPath);
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Not permitted to read the web client index at {Path}", indexPath);
            return null;
        }

        var logoUrl = BrandingAssetResolver.ResolveLogo(config, plugin, _logger);
        var changed = false;

        var css = CustomCssBuilder.Build(config, logoUrl);
        if (css.Length > 0)
        {
            // Injected as the last thing in <head>, so it is parsed before the body is rendered and
            // before the webpack bundle's stylesheet is fetched. The rules use !important, which
            // beats the later-loading bundle rules of equal specificity.
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
                _logger.LogWarning("No closing head tag found in {Path}; skipping style injection", indexPath);
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
