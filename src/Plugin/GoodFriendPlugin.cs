using Dalamud.Plugin;
using GoodFriend.Plugin.Base;
using Sirensong;

namespace GoodFriend.Plugin;

public sealed class GoodFriendPlugin : IDalamudPlugin
{
    /// <summary>
    ///     The plugin's main entry point.
    /// </summary>
    /// <param name="pluginInterface"></param>
    public GoodFriendPlugin(IDalamudPluginInterface pluginInterface)
    {
        SirenCore.Initialize(pluginInterface, pluginInterface.InternalName);
        pluginInterface.Create<DalamudInjections>();
        Services.Initialize();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Services.Dispose();
        SirenCore.Dispose();
    }
}
