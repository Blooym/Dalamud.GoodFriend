using FFXIVClientStructs.FFXIV.Client.UI;
using Sirensong.Game.Enums;
using Sirensong.Game.Helpers;

namespace GoodFriend.Plugin.Utility;

internal static class NotificationUtil
{
    public static void ShowErrorToast(string message)
    {
        ToastHelper.ShowErrorToast(message);
        unsafe
        {
            UIGlobals.PlaySoundEffect((uint)SoundEffect.Se11, default, default, default);
        }
    }
}
