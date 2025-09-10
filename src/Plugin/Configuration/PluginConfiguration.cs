using System;
using System.Globalization;
using Dalamud.Configuration;
using GoodFriend.Plugin.Base;

namespace GoodFriend.Plugin.Configuration;

/// <summary>
///     Provides access to and determines the Plugin configuration.
/// </summary>
internal sealed class PluginConfiguration : IPluginConfiguration
{


    /// <summary>
    ///     The current configuration version, incremented on breaking changes.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    ///     The configuration for the API.
    /// </summary>
    public ApiConfiguration ApiConfig { get; set; } = new();

    /// <summary>
    ///     The configuration for the API.
    /// </summary>
    public sealed class ApiConfiguration
    {
        /// <summary>
        ///     The default API URL to use.
        /// </summary>
        public static readonly Uri DefaultBaseUri = new("https://goodfriend-relay.dollware.net");

        /// <summary>
        ///     The Base URL to use when interacting with the API.
        /// </summary>
        public Uri BaseUrl { get; set; } = DefaultBaseUri;

        /// <summary>
        ///     The authentication key to use with authenticated requests.
        /// </summary>
        public string AuthKey { get; set; } = string.Empty;

        /// <summary>
        ///     An optional additional key used when hashing any identifable value to ensure they can only be read by others in the same group.
        /// </summary>
        public string PrivateGroupKey { get; set; } = string.Empty;
    }

    /// <summary>
    ///     Saves the current configuration to disk.
    /// </summary>
    public void Save() => DalamudInjections.PluginInterface.SavePluginConfig(this);

    /// <summary>
    ///     Loads the configuration from the disk or creates a new one if not found.
    /// </summary>
    /// <returns>The configuration instance.</returns>
    public static PluginConfiguration Load()
    {
        var config = DalamudInjections.PluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
        if (config.ApiConfig.BaseUrl.AbsoluteUri.StartsWith("https://goodfriend.blooym.dev", true, CultureInfo.InvariantCulture))
        {
            Logger.Information($"Updating API Base URL from old version {config.ApiConfig.BaseUrl} to new version {ApiConfiguration.DefaultBaseUri}");
            config.ApiConfig.BaseUrl = ApiConfiguration.DefaultBaseUri;
            config.Save();
        }
        return config;
    }
}
