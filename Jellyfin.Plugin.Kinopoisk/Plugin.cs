using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Kinopoisk.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Kinopoisk;

/// <summary>
/// Kinopoisk metadata plugin for Jellyfin.
/// </summary>
public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Plugin name constant.
    /// </summary>
    public const string PluginName = "КиноПоиск";
    
    /// <summary>
    /// Provider name for external IDs.
    /// </summary>
    public const string ProviderId = "Kinopoisk";

    /// <summary>
    /// Initializes a new instance of the <see cref="Plugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Application paths.</param>
    /// <param name="xmlSerializer">XML serializer.</param>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the plugin instance.
    /// </summary>
    public static Plugin? Instance { get; private set; }

    /// <inheritdoc />
    public override string Name => PluginName;

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("a0ad3c8a-0e15-4c2f-8f5a-9c7b6d4e3f2a");

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.configPage.html"
            }
        };
    }
}
