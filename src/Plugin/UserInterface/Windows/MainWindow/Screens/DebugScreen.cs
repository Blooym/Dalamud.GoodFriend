#if DEBUG
using System;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using GoodFriend.Plugin.Base;
using GoodFriend.Plugin.Localization;
using GoodFriend.Plugin.Utility;
using Sirensong.Game.Helpers;
using Sirensong.UserInterface;
using Sirensong.UserInterface.Style;

namespace GoodFriend.Plugin.UserInterface.Windows.MainWindow.Screens;

internal static class DebugScreen
{
    /// <summary>
    ///     The current debug option.
    /// </summary>
    private static DebugOption currentOption = DebugOption.Friends;

    /// <summary>
    ///     Draws the debug option list.
    /// </summary>
    public static void DrawDebugList()
    {
        foreach (var setting in Enum.GetValues<DebugOption>())
        {
            if (ImGui.Selectable(setting.ToString(), setting == currentOption))
            {
                currentOption = setting;
            }
        }
    }

    /// <summary>
    ///     Draws the debug details.
    /// </summary>
    public static void DrawDebugDetails()
    {
        switch (currentOption)
        {
            case DebugOption.Friends:
                DrawApiDebug();
                break;
        }
    }

    /// <summary>
    ///     Draws the API debug screen.
    /// </summary>
    private static unsafe void DrawApiDebug()
    {
        SiGui.Heading(Strings.UI_MainWindow_DebugScreen_DetectedFriends);
        SiGui.TextWrapped(Strings.UI_MainWindow_DebugScreen_DetectedFriends_Usage);
        ImGui.Dummy(Spacing.SectionSpacing);
        using (var child = ImRaii.Child("DebugFriendList"))
        {
            if (child.Success)
            {
                foreach (var f in FriendHelper.FriendList)
                {
                    if (f.ExtraFlags is Constants.WaitingForFriendListApprovalStatus)
                    {
                        continue;
                    }

                    if (string.IsNullOrEmpty(f.NameString))
                    {
                        continue;
                    }

                    if (ImGui.Selectable(f.NameString))
                    {
                        var salt = CryptoUtil.GenerateSalt();
                        var hash = CryptoUtil.HashValueWithSalt(f.ContentId, salt);
                        ImGui.SetClipboardText($"Friend: {f.NameString} Hash: {hash} | Salt: {salt}");
                        ChatHelper.Print(string.Format(Strings.UI_MainWindow_DebugScreen_DetectedFriends_Copied, f.NameString), 708);
                    }
                }
            }
        }
    }

    private enum DebugOption
    {
        Friends,
    }
}
#endif
