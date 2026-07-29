using System;
using System.IO;
using Jellyfin.Plugin.CustomLogo.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CustomLogo.Injection;

/// <summary>
/// Resolves the configured logo and favicon into a URL that can be embedded directly into the
/// served <c>index.html</c>.
/// </summary>
/// <remarks>
/// Uploaded images are inlined as <c>data:</c> URIs rather than being served from a plugin
/// endpoint. That keeps the branding free of an extra HTTP round trip, which is what makes the
/// splash logo correct on the very first paint instead of a frame or two later.
/// </remarks>
internal static class BrandingAssetResolver
{
    /// <summary>
    /// Maximum size of an upload that will be inlined, in bytes.
    /// </summary>
    public const int MaxInlineBytes = 2 * 1024 * 1024;

    private const string DefaultContentType = "image/png";

    /// <summary>
    /// Resolves the main logo.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The resolved URL, or <c>null</c> when no logo is configured.</returns>
    public static string? ResolveLogo(PluginConfiguration config, Plugin plugin, ILogger logger)
    {
        if (config.LogoSource == ImageSourceKind.Upload)
        {
            return ReadAsDataUri(plugin.GetLogoStoragePath(), config.UploadedLogoContentType, logger);
        }

        return NormalizeUrl(config.LogoUrl);
    }

    /// <summary>
    /// Resolves the favicon, falling back to the main logo when no dedicated favicon is configured.
    /// </summary>
    /// <param name="config">The plugin configuration.</param>
    /// <param name="plugin">The plugin instance.</param>
    /// <param name="logoUrl">The already resolved main logo URL.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>The resolved URL, or <c>null</c> when nothing is configured.</returns>
    public static string? ResolveFavicon(PluginConfiguration config, Plugin plugin, string? logoUrl, ILogger logger)
    {
        if (!config.UseSeparateFavicon)
        {
            return logoUrl;
        }

        if (config.FaviconSource == ImageSourceKind.Upload)
        {
            return ReadAsDataUri(plugin.GetFaviconStoragePath(), config.UploadedFaviconContentType, logger);
        }

        return NormalizeUrl(config.FaviconUrl);
    }

    private static string? NormalizeUrl(string? url)
    {
        return string.IsNullOrWhiteSpace(url) ? null : url.Trim();
    }

    private static string? ReadAsDataUri(string path, string? contentType, ILogger logger)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists)
            {
                return null;
            }

            if (info.Length > MaxInlineBytes)
            {
                logger.LogWarning(
                    "Uploaded image {Path} is {Length} bytes which exceeds the {Limit} byte inline limit; ignoring it",
                    path,
                    info.Length,
                    MaxInlineBytes);
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            if (bytes.Length == 0)
            {
                return null;
            }

            var mime = string.IsNullOrWhiteSpace(contentType) ? DefaultContentType : contentType.Trim();
            return string.Concat("data:", mime, ";base64,", Convert.ToBase64String(bytes));
        }
        catch (IOException ex)
        {
            logger.LogError(ex, "Failed to read uploaded image {Path}", path);
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogError(ex, "Not permitted to read uploaded image {Path}", path);
            return null;
        }
    }
}
