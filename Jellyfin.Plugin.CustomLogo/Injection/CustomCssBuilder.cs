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
    /// Selector for the header title element. Both classes are always applied together by
    /// jellyfin-web, so matching both wins over the web client's own single-class rules regardless
    /// of stylesheet order.
    /// </summary>
    private const string HeaderSelector = ".pageTitleWithDefaultLogo.pageTitleWithLogo";

    /// <summary>
    /// Horizontal space reserved for the logo, overridable for wide logos.
    /// </summary>
    private const string TextOffset = "var(--customlogo-text-offset,var(--customlogo-size))";

    /// <summary>
    /// Default header logo height.
    /// </summary>
    private const string DefaultLogoSize = "2em";

    /// <summary>
    /// Default header text height. Set independently of the logo height on purpose: the two are
    /// tuned by eye, not by a fixed ratio. The text's width always follows its content.
    /// </summary>
    private const string DefaultTextSize = "1.8em";

    /// <summary>
    /// Default header text weight. Normal rather than semi-bold, which reads as heavy at these sizes.
    /// </summary>
    private const string DefaultTextWeight = "400";

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

        var logoSize = SanitizeCssLength(config.HeaderLogoSize, DefaultLogoSize);
        var textSize = SanitizeCssLength(config.HeaderTextFontSize, DefaultTextSize);
        var textColor = SanitizeCssValue(config.HeaderTextColor, "#fff");
        var textWeight = SanitizeCssValue(config.HeaderTextFontWeight, DefaultTextWeight);

        var sb = new StringBuilder(1024);

        // Custom properties, so the values stay inspectable and overridable by further custom CSS.
        sb.Append(":root{");
        if (hasLogo)
        {
            sb.Append("--customlogo-image:url(\"").Append(EscapeCssString(logoUrl!)).Append("\");");
        }

        if (headerText)
        {
            sb.Append("--customlogo-header-text:\"").Append(EscapeCssString(config.HeaderText)).Append("\";")
              .Append("--customlogo-text-size:").Append(textSize).Append(';');
        }

        sb.Append("--customlogo-size:").Append(logoSize).Append(";}");

        if (splash)
        {
            sb.Append(".splashLogo{background-image:var(--customlogo-image)!important;}");
        }

        if (header)
        {
            // jellyfin-web always applies .pageTitleWithLogo together with .pageTitleWithDefaultLogo,
            // so matching both gives two-class specificity. That deliberately beats the web client's
            // own .pageTitle (height) and .pageTitleWithLogo (width) rules no matter which order the
            // stylesheets end up in: with code splitting, bundle CSS can be inserted at runtime and
            // therefore *after* this block. Relying on document order here previously left the box at
            // the stock 1.7em, which clipped the logo top and bottom and squashed the header text.
            sb.Append(HeaderSelector).Append('{');

            if (hasLogo)
            {
                // Only the image is forced: themes set background-image on .pageTitleWithDefaultLogo
                // and are applied after page load, so that one declaration has to win outright.
                // Sizing to 100% of the box means the logo can never overflow and get clipped.
                sb.Append("background-image:var(--customlogo-image)!important;")
                  .Append("background-size:auto 100%;")
                  .Append("background-position:left center;")
                  .Append("background-repeat:no-repeat;");
            }

            sb.Append("display:flex;")
              .Append("align-items:center;")
              .Append("width:auto;")
              .Append("height:var(--customlogo-size);")
              .Append("overflow:visible;");

            // Reserve space for the logo so the ::after text sits next to it rather than on top of
            // it. Wide (banner style) logos can widen the gap via --customlogo-text-offset.
            sb.Append("padding-left:")
              .Append(headerText ? "calc(" + TextOffset + " + 0.5em)" : TextOffset)
              .Append(";}");

            if (headerText)
            {
                sb.Append(HeaderSelector).Append("::after{")
                  .Append("content:var(--customlogo-header-text);")
                  .Append("color:").Append(textColor).Append(';')
                  .Append("font-size:var(--customlogo-text-size);")
                  .Append("line-height:1;")
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
                  .Append(HeaderSelector).Append("{padding-left:").Append(TextOffset).Append(";}")
                  .Append(HeaderSelector).Append("::after{display:none;}")
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
    /// Validates a CSS length, treating a value with no unit as <c>em</c>.
    /// </summary>
    /// <remarks>
    /// Administrators reasonably think of these settings as plain numbers and type "2" rather than
    /// "2em". A unitless number is not a valid length, so the browser would drop the whole
    /// declaration and the setting would appear to do nothing. Assuming <c>em</c> keeps that from
    /// being a silent failure.
    /// </remarks>
    /// <param name="value">The configured value.</param>
    /// <param name="fallback">The value to use when <paramref name="value"/> is unusable.</param>
    /// <returns>A safe CSS length.</returns>
    private static string SanitizeCssLength(string? value, string fallback)
    {
        var sanitized = SanitizeCssValue(value, fallback);
        return IsUnitlessNumber(sanitized) ? sanitized + "em" : sanitized;
    }

    /// <summary>
    /// Determines whether a value is a plain number with no unit attached.
    /// </summary>
    /// <param name="value">The value to test.</param>
    /// <returns><c>true</c> when the value consists only of digits and at most one decimal point.</returns>
    private static bool IsUnitlessNumber(string value)
    {
        var seenDigit = false;
        var seenDot = false;

        foreach (var c in value)
        {
            if (char.IsAsciiDigit(c))
            {
                seenDigit = true;
            }
            else if (c == '.' && !seenDot)
            {
                seenDot = true;
            }
            else
            {
                return false;
            }
        }

        return seenDigit;
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
