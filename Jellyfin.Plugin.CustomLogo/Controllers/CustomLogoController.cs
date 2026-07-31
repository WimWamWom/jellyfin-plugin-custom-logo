using System;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Plugin.CustomLogo.Configuration;
using Jellyfin.Plugin.CustomLogo.Injection;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.CustomLogo.Controllers;

/// <summary>
/// Upload and retrieval of the plugin's branding images.
/// </summary>
/// <remarks>
/// These endpoints only manage the stored files. The images are delivered to browsers inlined as
/// <c>data:</c> URIs inside the branded <c>index.html</c>, so this controller is not on the hot path
/// for rendering the logo.
/// </remarks>
[ApiController]
[Authorize(Policy = Policies.RequiresElevation)]
[Route("CustomLogo")]
public partial class CustomLogoController : ControllerBase
{
    private readonly ILogger<CustomLogoController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomLogoController"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public CustomLogoController(ILogger<CustomLogoController> logger)
    {
        _logger = logger;
    }

    // Source-generated: the slot argument is boxed to pass through the ad-hoc ILogger.LogInformation
    // overload, which CA1873 flags regardless of how cheap the value actually is to evaluate.
    // LoggerMessage sidesteps that by generating a strongly typed method with no boxing.
    [LoggerMessage(Level = LogLevel.Information, Message = "Stored uploaded branding image for slot {Slot}")]
    private partial void LogImageStored(BrandingImageSlot slot);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleared stored branding image for slot {Slot}")]
    private partial void LogImageCleared(BrandingImageSlot slot);

    /// <summary>
    /// Uploads a branding image.
    /// </summary>
    /// <param name="slot">The image slot to write to.</param>
    /// <param name="file">The uploaded file.</param>
    /// <returns>No content on success.</returns>
    /// <response code="204">Image uploaded.</response>
    /// <response code="400">The upload was missing, too large, or not an image.</response>
    [HttpPost("Upload/{slot}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> UploadImage([FromRoute] BrandingImageSlot slot, IFormFile? file)
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        if (file is null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        if (file.Length > BrandingAssetResolver.MaxInlineBytes)
        {
            return BadRequest("The image is larger than the 2 MB limit.");
        }

        var contentType = file.ContentType;
        if (string.IsNullOrWhiteSpace(contentType)
            || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only image files are accepted.");
        }

        var path = GetStoragePath(plugin, slot);
        Directory.CreateDirectory(plugin.DataFolderPath);

        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await using (stream.ConfigureAwait(false))
        {
            await file.CopyToAsync(stream, HttpContext.RequestAborted).ConfigureAwait(false);
        }

        var config = plugin.Configuration;
        if (slot == BrandingImageSlot.Logo)
        {
            config.UploadedLogoContentType = contentType;
            config.UploadedLogoFileName = Path.GetFileName(file.FileName);
            config.LogoSource = ImageSourceKind.Upload;
        }
        else
        {
            config.UploadedFaviconContentType = contentType;
            config.UploadedFaviconFileName = Path.GetFileName(file.FileName);
            config.FaviconSource = ImageSourceKind.Upload;
            config.UseSeparateFavicon = true;
        }

        plugin.UpdateConfiguration(config);
        LogImageStored(slot);

        return NoContent();
    }

    /// <summary>
    /// Gets a previously uploaded branding image.
    /// </summary>
    /// <param name="slot">The image slot to read.</param>
    /// <returns>The stored image.</returns>
    /// <response code="200">The image file, with its stored content type.</response>
    /// <response code="404">No image has been uploaded for this slot.</response>
    [HttpGet("Image/{slot}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult GetImage([FromRoute] BrandingImageSlot slot)
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var path = GetStoragePath(plugin, slot);
        if (!System.IO.File.Exists(path))
        {
            return NotFound();
        }

        var config = plugin.Configuration;
        var contentType = slot == BrandingImageSlot.Logo
            ? config.UploadedLogoContentType
            : config.UploadedFaviconContentType;

        if (string.IsNullOrWhiteSpace(contentType))
        {
            contentType = "application/octet-stream";
        }

        return PhysicalFile(path, contentType);
    }

    /// <summary>
    /// Removes a previously uploaded branding image.
    /// </summary>
    /// <param name="slot">The image slot to clear.</param>
    /// <returns>No content.</returns>
    /// <response code="204">The stored image was cleared.</response>
    [HttpDelete("Image/{slot}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult DeleteImage([FromRoute] BrandingImageSlot slot)
    {
        var plugin = Plugin.Instance;
        if (plugin is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var path = GetStoragePath(plugin, slot);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        var config = plugin.Configuration;
        if (slot == BrandingImageSlot.Logo)
        {
            config.UploadedLogoContentType = string.Empty;
            config.UploadedLogoFileName = string.Empty;
            config.LogoSource = ImageSourceKind.Url;
        }
        else
        {
            config.UploadedFaviconContentType = string.Empty;
            config.UploadedFaviconFileName = string.Empty;
            config.FaviconSource = ImageSourceKind.Url;
        }

        plugin.UpdateConfiguration(config);
        LogImageCleared(slot);

        return NoContent();
    }

    private static string GetStoragePath(Plugin plugin, BrandingImageSlot slot)
        => slot == BrandingImageSlot.Logo ? plugin.GetLogoStoragePath() : plugin.GetFaviconStoragePath();
}
