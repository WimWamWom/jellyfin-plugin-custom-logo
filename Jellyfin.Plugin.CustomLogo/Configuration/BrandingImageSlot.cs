namespace Jellyfin.Plugin.CustomLogo.Configuration;

/// <summary>
/// Identifies which uploaded image is being addressed.
/// </summary>
public enum BrandingImageSlot
{
    /// <summary>
    /// The main logo, used for the splash, header and drawer.
    /// </summary>
    Logo,

    /// <summary>
    /// The dedicated favicon.
    /// </summary>
    Favicon
}
