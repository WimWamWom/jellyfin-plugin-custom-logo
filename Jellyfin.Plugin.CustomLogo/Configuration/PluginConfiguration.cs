using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.CustomLogo.Configuration;

/// <summary>
/// Controls how much of the default Jellyfin branding the plugin replaces.
/// </summary>
public enum LogoReplacementMode
{
    /// <summary>
    /// Replace nothing. The plugin stays installed but injects no markup or CSS at all.
    /// </summary>
    None,

    /// <summary>
    /// Replace every supported logo, ignoring the individual target toggles.
    /// </summary>
    All,

    /// <summary>
    /// Replace only the targets that are individually enabled.
    /// </summary>
    Specific
}

/// <summary>
/// Where an image is loaded from.
/// </summary>
public enum ImageSourceKind
{
    /// <summary>
    /// Use an external URL supplied by the administrator.
    /// </summary>
    Url,

    /// <summary>
    /// Use an image uploaded through the plugin configuration page.
    /// </summary>
    Upload
}

/// <summary>
/// What is drawn next to the header logo.
/// </summary>
public enum HeaderTextKind
{
    /// <summary>
    /// Leave whatever Jellyfin draws there. The default ("Modern") layout shows the server name;
    /// the classic header has no text of its own, so there it means the logo stands alone.
    /// </summary>
    Default,

    /// <summary>
    /// Draw the text configured in <see cref="PluginConfiguration.HeaderText"/>.
    /// </summary>
    Custom,

    /// <summary>
    /// Draw no text at all, so the logo has the whole header button to itself.
    /// </summary>
    None
}

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    private HeaderTextKind _headerTextMode;

    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        Mode = LogoReplacementMode.All;

        ReplaceSplashLogo = true;
        ReplaceHeaderLogo = true;
        ReplaceDrawerLogo = true;
        ReplaceFavicon = true;

        LogoSource = ImageSourceKind.Url;
        LogoUrl = string.Empty;
        UploadedLogoContentType = string.Empty;
        UploadedLogoFileName = string.Empty;

        UseSeparateFavicon = false;
        FaviconSource = ImageSourceKind.Url;
        FaviconUrl = string.Empty;
        UploadedFaviconContentType = string.Empty;
        UploadedFaviconFileName = string.Empty;

        // All left empty on purpose. With no logo and no header text configured, the plugin injects
        // nothing at all, so a fresh install has no visible effect until it is set up. The appearance
        // fields stay empty for the same reason: an empty field hands that detail back to Jellyfin,
        // so setting up a logo changes the logo and nothing else. Height, colour and weight are the
        // web client's own until the administrator decides otherwise.
        HeaderText = string.Empty;
        HeaderTextMode = HeaderTextKind.Default;
        ShowHeaderText = null;
        HideHeaderTextOnMobile = true;
        HeaderLogoSize = string.Empty;
        HeaderTextColor = string.Empty;
        HeaderTextFontSize = string.Empty;
        HeaderTextFontWeight = string.Empty;
    }

    /// <summary>
    /// Gets or sets the overall replacement mode.
    /// </summary>
    public LogoReplacementMode Mode { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the splash (loading) logo is replaced.
    /// </summary>
    public bool ReplaceSplashLogo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the header / navbar logo is replaced.
    /// </summary>
    public bool ReplaceHeaderLogo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the admin drawer logo is replaced.
    /// </summary>
    public bool ReplaceDrawerLogo { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the favicon and touch icons are replaced.
    /// </summary>
    public bool ReplaceFavicon { get; set; }

    /// <summary>
    /// Gets or sets where the logo image is loaded from.
    /// </summary>
    public ImageSourceKind LogoSource { get; set; }

    /// <summary>
    /// Gets or sets the external logo URL, used when <see cref="LogoSource"/> is <see cref="ImageSourceKind.Url"/>.
    /// </summary>
    public string LogoUrl { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the uploaded logo.
    /// </summary>
    public string UploadedLogoContentType { get; set; }

    /// <summary>
    /// Gets or sets the original file name of the uploaded logo. Empty when nothing has been uploaded.
    /// </summary>
    public string UploadedLogoFileName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the favicon uses its own image instead of the main logo.
    /// </summary>
    public bool UseSeparateFavicon { get; set; }

    /// <summary>
    /// Gets or sets where the favicon image is loaded from.
    /// </summary>
    public ImageSourceKind FaviconSource { get; set; }

    /// <summary>
    /// Gets or sets the external favicon URL.
    /// </summary>
    public string FaviconUrl { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the uploaded favicon.
    /// </summary>
    public string UploadedFaviconContentType { get; set; }

    /// <summary>
    /// Gets or sets the original file name of the uploaded favicon. Empty when nothing has been uploaded.
    /// </summary>
    public string UploadedFaviconFileName { get; set; }

    /// <summary>
    /// Gets or sets the text rendered next to the header logo.
    /// </summary>
    public string HeaderText { get; set; }

    /// <summary>
    /// Gets or sets what is drawn next to the header logo.
    /// </summary>
    /// <remarks>
    /// While <see cref="ShowHeaderText"/> still holds a value this reads back as the mode that
    /// configuration already rendered as, so upgrading cannot change anyone's header. The stored
    /// value takes over as soon as the configuration is saved once, which clears that flag.
    /// </remarks>
    public HeaderTextKind HeaderTextMode
    {
        get
        {
            if (ShowHeaderText is not bool legacy)
            {
                return _headerTextMode;
            }

            // Before the mode existed, the plugin only ever *added* text: unticking the box, or
            // leaving the text empty, left the web client's own header alone rather than emptying
            // it. Both therefore migrate to Default, not to None.
            return legacy && !string.IsNullOrEmpty(HeaderText)
                ? HeaderTextKind.Custom
                : HeaderTextKind.Default;
        }

        set => _headerTextMode = value;
    }

    /// <summary>
    /// Gets or sets the flag that <see cref="HeaderTextMode"/> replaced in 2.0.0.1, or <c>null</c>
    /// in any configuration saved since.
    /// </summary>
    /// <remarks>
    /// Kept only so an older configuration can still be read back faithfully; see
    /// <see cref="HeaderTextMode"/>. The configuration page clears it on save, so it disappears
    /// from the stored XML the first time the settings are touched.
    /// </remarks>
    public bool? ShowHeaderText { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the header text is hidden on narrow (mobile) viewports.
    /// </summary>
    public bool HideHeaderTextOnMobile { get; set; }

    /// <summary>
    /// Gets or sets the header logo height as a CSS length, for example <c>2.2em</c>.
    /// </summary>
    public string HeaderLogoSize { get; set; }

    /// <summary>
    /// Gets or sets the header text colour as a CSS colour, for example <c>#fff</c>.
    /// </summary>
    public string HeaderTextColor { get; set; }

    /// <summary>
    /// Gets or sets the header text height as a CSS length, for example <c>1.5em</c>. Independent of
    /// <see cref="HeaderLogoSize"/>; the text's width always follows its content.
    /// </summary>
    public string HeaderTextFontSize { get; set; }

    /// <summary>
    /// Gets or sets the header text weight as a CSS font weight, for example <c>600</c>.
    /// </summary>
    public string HeaderTextFontWeight { get; set; }

    /// <summary>
    /// Gets a value indicating whether the splash logo should be replaced given the current mode.
    /// </summary>
    /// <returns><c>true</c> when the splash logo should be replaced.</returns>
    public bool IsSplashLogoEnabled() => IsTargetEnabled(ReplaceSplashLogo);

    /// <summary>
    /// Gets a value indicating whether the header logo should be replaced given the current mode.
    /// </summary>
    /// <returns><c>true</c> when the header logo should be replaced.</returns>
    public bool IsHeaderLogoEnabled() => IsTargetEnabled(ReplaceHeaderLogo);

    /// <summary>
    /// Gets a value indicating whether the drawer logo should be replaced given the current mode.
    /// </summary>
    /// <returns><c>true</c> when the drawer logo should be replaced.</returns>
    public bool IsDrawerLogoEnabled() => IsTargetEnabled(ReplaceDrawerLogo);

    /// <summary>
    /// Gets a value indicating whether the favicon should be replaced given the current mode.
    /// </summary>
    /// <returns><c>true</c> when the favicon should be replaced.</returns>
    public bool IsFaviconEnabled() => IsTargetEnabled(ReplaceFavicon);

    private bool IsTargetEnabled(bool specificToggle) => Mode switch
    {
        LogoReplacementMode.All => true,
        LogoReplacementMode.Specific => specificToggle,
        _ => false
    };
}
