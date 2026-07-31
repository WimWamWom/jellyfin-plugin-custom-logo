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
    /// Horizontal space reserved for the logo, overridable for wide logos. Falls back to the web
    /// client's own header height when no logo height is configured.
    /// </summary>
    private const string TextOffset = "var(--customlogo-text-offset,var(--customlogo-size,1.7em))";

    /// <summary>
    /// Selector for the logo image inside jellyfin-web 12's React/MUI toolbar button. That button
    /// replaces <see cref="HeaderSelector"/> as the visible header whenever the web client's default
    /// layout ("Modern") is active or a dashboard page is open; <see cref="HeaderSelector"/> itself is
    /// then present but hidden. The button carries no Jellyfin-specific class, so this targets MUI's
    /// own stable global class name instead. It cannot match anything else: across the whole web
    /// client, the server logo is the only place that passes a literal &lt;img&gt; as a button's start
    /// icon, everywhere else passes an SVG icon component.
    /// </summary>
    private const string ModernHeaderIconSelector = ".MuiButton-startIcon img";

    /// <summary>
    /// Selector for the button element itself, used to hide its own text before <c>::after</c> can
    /// supply a replacement. See <see cref="ModernHeaderIconSelector"/>.
    /// </summary>
    private const string ModernHeaderButtonSelector = ".MuiButton-root:has(.MuiButton-startIcon img)";

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

        // An empty appearance field means "leave it to Jellyfin": the declaration is skipped
        // entirely, so the web client's own value applies instead of a value of the plugin's choosing.
        var logoSize = OptionalCssLength(config.HeaderLogoSize);
        var textSize = OptionalCssLength(config.HeaderTextFontSize);
        var textColor = OptionalCssValue(config.HeaderTextColor);
        var textWeight = OptionalCssValue(config.HeaderTextFontWeight);

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

            if (textSize is not null)
            {
                sb.Append("--customlogo-text-size:").Append(textSize).Append(';');
            }
        }

        if (logoSize is not null)
        {
            sb.Append("--customlogo-size:").Append(logoSize).Append(';');
        }

        sb.Append('}');

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
              .Append("overflow:visible;");

            if (logoSize is not null)
            {
                sb.Append("height:var(--customlogo-size);");
            }

            // Reserve space for the logo so the ::after text sits next to it rather than on top of
            // it. Wide (banner style) logos can widen the gap via --customlogo-text-offset.
            sb.Append("padding-left:")
              .Append(headerText ? "calc(" + TextOffset + " + 0.5em)" : TextOffset)
              .Append(";}");

            if (headerText)
            {
                sb.Append(HeaderSelector).Append("::after{")
                  .Append("content:var(--customlogo-header-text);")
                  .Append("line-height:1;")
                  .Append("white-space:nowrap;");

                if (textSize is not null)
                {
                    sb.Append("font-size:var(--customlogo-text-size);");
                }

                if (textColor is not null)
                {
                    sb.Append("color:").Append(textColor).Append(';');
                }

                if (textWeight is not null)
                {
                    sb.Append("font-weight:").Append(textWeight).Append(';');
                }

                sb.Append('}');
            }

            if (hasLogo)
            {
                sb.Append(".layout-tv .pageTitleWithDefaultLogo{background-image:var(--customlogo-image)!important;}")
                  .Append(ModernHeaderIconSelector).Append("{content:var(--customlogo-image)!important;}");
            }

            if (headerText)
            {
                // The button's own text is a bare text node next to the icon, with no element around
                // it to select. Making the whole button transparent hides that text without touching
                // font-size, which the icon's own inline sizing (set by jellyfin-web) is relative to.
                // The ::after content then supplies the replacement text with its own explicit colour,
                // since it would otherwise inherit the transparent one. The original text still
                // reserves its layout width, so a long configured server name leaves a gap before the
                // replacement text; there is no CSS way to collapse a bare text node's width without
                // also collapsing font-size, which the icon depends on.
                sb.Append(ModernHeaderButtonSelector).Append("{color:transparent!important;}")
                  .Append(ModernHeaderButtonSelector).Append("::after{")
                  .Append("content:var(--customlogo-header-text);")
                  .Append("color:").Append(textColor ?? "#fff").Append(';');

                if (textSize is not null)
                {
                    sb.Append("font-size:var(--customlogo-text-size);");
                }

                if (textWeight is not null)
                {
                    sb.Append("font-weight:").Append(textWeight).Append(';');
                }

                sb.Append('}');
            }

            if (headerText && config.HideHeaderTextOnMobile)
            {
                // On narrow viewports drop the text and shrink the padding back to just the logo.
                sb.Append("@media (max-width:50em){")
                  .Append(HeaderSelector).Append("{padding-left:").Append(TextOffset).Append(";}")
                  .Append(HeaderSelector).Append("::after{display:none;}")
                  .Append(ModernHeaderButtonSelector).Append("::after{display:none;}")
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
    /// Validates an optional CSS length.
    /// </summary>
    /// <param name="value">The configured value.</param>
    /// <returns>
    /// The sanitized length, or <c>null</c> when the field was left empty, meaning the declaration
    /// should be omitted so that Jellyfin's own value applies.
    /// </returns>
    private static string? OptionalCssLength(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // An unusable value sanitizes to empty, which must also be dropped rather than emitted.
        var sanitized = SanitizeCssLength(value, string.Empty);
        return sanitized.Length == 0 ? null : sanitized;
    }

    /// <summary>
    /// Validates an optional bare CSS value such as a colour or font weight.
    /// </summary>
    /// <param name="value">The configured value.</param>
    /// <returns>
    /// The sanitized value, or <c>null</c> when the field was left empty or the value was unusable,
    /// meaning the declaration should be omitted so that Jellyfin's own value applies.
    /// </returns>
    private static string? OptionalCssValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var sanitized = SanitizeCssValue(value, string.Empty);
        return sanitized.Length == 0 ? null : sanitized;
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
