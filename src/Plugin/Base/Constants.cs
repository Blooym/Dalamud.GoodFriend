namespace GoodFriend.Plugin.Base;

/// <summary>
///     A collection of constant values used throughout the plugin.
/// </summary>
internal static class Constants
{
    public const uint WaitingForFriendListApprovalStatus = 0x20;

    /// <summary>
    ///     Links to various places.
    /// </summary>
    internal static class Link
    {
        /// <summary>
        ///     The URL to the donation page.
        /// </summary>
        public const string Donate = "https://go.blooym.dev/donate";
    }

    /// <summary>
    ///     The plugin's directories.
    /// </summary>
    internal static class Directory
    {
        /// <summary>
        ///     The directory where the module configurations are stored.
        /// </summary>
        public static readonly string ModuleConfig = @$"{DalamudInjections.PluginInterface.ConfigDirectory}\ModuleConfig\";
    }
}
