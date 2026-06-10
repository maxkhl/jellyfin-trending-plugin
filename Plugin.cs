using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace JellyfinTrending;

/// <summary>Trending plugin main class.</summary>
public class Plugin : BasePlugin<BasePluginConfiguration>
{
    /// <summary>Initializes a new instance of <see cref="Plugin"/>.</summary>
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer) { }

    /// <inheritdoc />
    public override string Name => "Trending";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("3a7b9c2d-4e5f-6a7b-8c9d-0e1f2a3b4c5d");

    /// <inheritdoc />
    public override string Description => "Shows trending content across all users";
}
