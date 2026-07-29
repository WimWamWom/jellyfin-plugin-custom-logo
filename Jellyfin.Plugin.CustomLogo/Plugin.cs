using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Jellyfin.Plugin.CustomLogo.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.CustomLogo;

/// <summary>
/// The main plugin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// File name used to store the uploaded logo inside the plugin data folder.
    /// </summary>
    public const string LogoStorageFileName = "logo.bin";

    /// <summary>
    /// File name used to store the uploaded favicon inside the plugin data folder.
    /// </summary>
    public const string FaviconStorageFileName = "favicon.bin";

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => "Custom Logo";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("f3a1c0d2-7b5e-4a68-9d31-2e6c8b4f7a10");

    /// <inheritdoc />
    public override string Description
        => "Replaces the Jellyfin splash, header, drawer and favicon branding with your own logo and header text.";

    /// <summary>
    /// Gets the absolute path of the stored logo upload.
    /// </summary>
    /// <returns>The absolute file path.</returns>
    public string GetLogoStoragePath() => Path.Combine(DataFolderPath, LogoStorageFileName);

    /// <summary>
    /// Gets the absolute path of the stored favicon upload.
    /// </summary>
    /// <returns>The absolute file path.</returns>
    public string GetFaviconStoragePath() => Path.Combine(DataFolderPath, FaviconStorageFileName);

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                DisplayName = Name,

                // Puts the plugin into the dashboard's left-hand navigation alongside the other
                // plugins, instead of only being reachable through the plugin list.
                EnableInMainMenu = true,
                MenuIcon = "image",
                EmbeddedResourcePath = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.Configuration.configPage.html",
                    GetType().Namespace)
            }
        ];
    }
}
