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
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
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
        ShowHeaderText = true;
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
    /// Gets or sets a value indicating whether the header text is rendered at all.
    /// </summary>
    public bool ShowHeaderText { get; set; }

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
