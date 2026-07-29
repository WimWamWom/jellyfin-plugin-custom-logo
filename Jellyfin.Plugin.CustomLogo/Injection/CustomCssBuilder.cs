using System.Text;
using Jellyfin.Plugin.CustomLogo.Configuration;

namespace Jellyfin.Plugin.CustomLogo.Injection;

/// <summary>
/// Builds the stylesheet that is injected into the served <c>index.html</c>.
/// </summary>
/// <remarks>
/// The generated CSS mirrors the hand written custom-CSS block this plugin replaces, but every
/// value is driven by the plugin configuration and every user supplied value is escaped so that it
/// cannot break out of the CSS string, the rule block, or the surrounding <c>&lt;style&gt;</c> element.
/// </remarks>
internal static class CustomCssBuilder
{
    /// <summary>
    /// Builds the stylesheet for the supplied configuration.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="logoUrl">The resolved logo URL (external URL or <c>data:</c> URI), or <c>null</c> when none is configured.</param>
    /// <returns>The stylesheet, or an empty string when there is nothing to inject.</returns>
    public static string Build(PluginConfiguration config, string? logoUrl)
    {
        var hasLogo = !string.IsNullOrEmpty(logoUrl);
        var splash = config.IsSplashLogoEnabled() && hasLogo;
        var header = config.IsHeaderLogoEnabled();
        var drawer = config.IsDrawerLogoEnabled() && hasLogo;
        var headerText = header && config.ShowHeaderText && !string.IsNullOrEmpty(config.HeaderText);

        // Nothing to do: bail out so the middleware can serve the untouched file.
        if (!splash && !drawer && !headerText && !(header && hasLogo))
        {
            return string.Empty;
        }

        var logoSize = SanitizeCssValue(config.HeaderLogoSize, "2.2em");
        var textColor = SanitizeCssValue(config.HeaderTextColor, "#fff");
        var textSize = SanitizeCssValue(config.HeaderTextFontSize, "1.1em");
        var textWeight = SanitizeCssValue(config.HeaderTextFontWeight, "600");

        var sb = new StringBuilder(1024);

        // Custom properties, so the values stay inspectable and overridable by further custom CSS.
        sb.Append(":root{");
        if (hasLogo)
        {
            sb.Append("--customlogo-image:url(\"").Append(EscapeCssString(logoUrl!)).Append("\");");
        }

        if (headerText)
        {
            sb.Append("--customlogo-header-text:\"").Append(EscapeCssString(config.HeaderText)).Append("\";");
        }

        sb.Append("--customlogo-size:").Append(logoSize).Append(";}");

        if (splash)
        {
            sb.Append(".splashLogo{background-image:var(--customlogo-image)!important;}");
        }

        if (header)
        {
            sb.Append(".pageTitleWithDefaultLogo{");
            if (hasLogo)
            {
                sb.Append("background-image:var(--customlogo-image)!important;")
                  .Append("background-size:auto var(--customlogo-size)!important;")
                  .Append("background-position:left center!important;")
                  .Append("background-repeat:no-repeat!important;");
            }

            sb.Append("display:flex!important;")
              .Append("align-items:center;")
              .Append("width:auto!important;")
              .Append("height:var(--customlogo-size)!important;")
              .Append("overflow:visible;");

            // Reserve space for the logo so the ::after text sits next to it rather than on top of it.
            sb.Append("padding-left:")
              .Append(headerText ? "calc(var(--customlogo-size) + 0.5em)" : "var(--customlogo-size)")
              .Append(";}");

            if (headerText)
            {
                sb.Append(".pageTitleWithDefaultLogo::after{")
                  .Append("content:var(--customlogo-header-text);")
                  .Append("color:").Append(textColor).Append(';')
                  .Append("font-size:").Append(textSize).Append(';')
                  .Append("font-weight:").Append(textWeight).Append(';')
                  .Append("white-space:nowrap;}");
            }

            if (hasLogo)
            {
                sb.Append(".layout-tv .pageTitleWithDefaultLogo{background-image:var(--customlogo-image)!important;}");
            }

            if (headerText && config.HideHeaderTextOnMobile)
            {
                // On narrow viewports drop the text and shrink the padding back to just the logo.
                sb.Append("@media (max-width:50em){")
                  .Append(".pageTitleWithDefaultLogo{padding-left:var(--customlogo-size);}")
                  .Append(".pageTitleWithDefaultLogo::after{display:none;}")
                  .Append('}');
            }
        }

        if (drawer)
        {
            sb.Append(".adminDrawerLogo img{content:var(--customlogo-image);}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Escapes a value for use inside a double quoted CSS string.
    /// </summary>
    /// <param name="value">The raw value.</param>
    /// <returns>The escaped value.</returns>
    private static string EscapeCssString(string value)
    {
        var sb = new StringBuilder(value.Length + 16);
        foreach (var c in value)
        {
            switch (c)
            {
                case '\\':
                    sb.Append("\\\\");
                    break;
                case '"':
                    sb.Append("\\\"");
                    break;

                // Escaped as CSS character escapes so a value can never close the <style> element.
                case '<':
                    sb.Append("\\00003c");
                    break;
                case '>':
                    sb.Append("\\00003e");
                    break;
                case '\r':
                case '\n':
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Validates a bare CSS value such as a length or colour, falling back when it contains
    /// anything that could terminate the declaration or the rule block.
    /// </summary>
    /// <param name="value">The configured value.</param>
    /// <param name="fallback">The value to use when <paramref name="value"/> is unusable.</param>
    /// <returns>A safe CSS value.</returns>
    private static string SanitizeCssValue(string? value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        var trimmed = value.Trim();
        foreach (var c in trimmed)
        {
            var allowed = char.IsAsciiLetterOrDigit(c)
                || c is '.' or ',' or '%' or '#' or '(' or ')' or '-' or '+' or '*' or '/' or ' ' or '_';
            if (!allowed)
            {
                return fallback;
            }
        }

        return trimmed;
    }
}
