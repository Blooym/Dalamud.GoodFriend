using System;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using GoodFriend.Plugin.Base;
using GoodFriend.Plugin.Localization;
using Sirensong.Game.Helpers;
using Sirensong.UserInterface;
using Sirensong.UserInterface.Style;

namespace GoodFriend.Plugin.ModuleSystem.Modules;

internal sealed class CurrentFriendsOnlineModule : BaseModule
{
    /// <summary>
    ///     The configuration for this module.
    /// </summary>
    private CurrentFriendsOnlineModuleConfig Config { get; set; } = BaseModuleConfig.Load<CurrentFriendsOnlineModuleConfig>();

    /// <inheritdoc/>
    public override string Name { get; } = Strings.Modules_CurrentFriendsOnlineModule_Name;

    /// <inheritdoc/>
    public override string? Description { get; } = Strings.Modules_CurrentFriendsOnlineModule_Description;

    /// <inheritdoc/>
    public override ModuleTag Tag { get; } = ModuleTag.Notifications;

    /// <inheritdoc/>
    protected override void OnEnable() => DalamudInjections.ClientState.Login += this.OnLogin;

    /// <inheritdoc/>
    protected override void OnDisable() => DalamudInjections.ClientState.Login -= this.OnLogin;

    /// <inheritdoc/>
    protected override void DrawModule()
    {
        SiGui.Heading(Strings.Modules_CurrentFriendsOnlineModule_UI_Upon_Login);

        var friendCountOnlineEnabled = this.Config.ShowOnlineFriendCountOnLogin;
        if (SiGui.Checkbox(Strings.Modules_CurrentFriendsOnlineModule_UI_SendFriendCount, Strings.Modules_CurrentFriendsOnlineModule_UI_SendFriendCount_Description, ref friendCountOnlineEnabled))
        {
            this.Config.ShowOnlineFriendCountOnLogin = friendCountOnlineEnabled;
            this.Config.Save();
        }
        ImGui.Dummy(Spacing.ReadableSpacing);

        using (ImRaii.Disabled(!friendCountOnlineEnabled))
        {
            ImGui.Indent();
            var namesOnLogin = this.Config.AddFriendNamesToOnlineCount;
            if (SiGui.Checkbox(Strings.Modules_CurrentFriendsOnlineModule_UI_IncludeFriendNames, Strings.Modules_CurrentFriendsOnlineModule_UI_IncludeFriendNames_Description, ref namesOnLogin))
            {
                this.Config.AddFriendNamesToOnlineCount = namesOnLogin;
                this.Config.Save();
            }
            ImGui.Dummy(Spacing.ReadableSpacing);

            var time = (int)this.Config.OnlineFriendShowDelay.TotalSeconds;
            SiGui.Text(Strings.Modules_CurrentFriendsOnlineModule_UI_SendFriendCount_Delay);
            if (SiGui.SliderInt("##OnlineFriendShowDelaySlider", ref time, CurrentFriendsOnlineModuleConfig.OnlineFriendShowDelayMinSeconds, CurrentFriendsOnlineModuleConfig.OnlineFriendShowDelayMaxSeconds))
            {
                this.Config.OnlineFriendShowDelay = TimeSpan.FromSeconds(time);
                this.Config.Save();
            }
            ImGui.SameLine();
            if (ImGuiComponents.IconButtonWithText(FontAwesomeIcon.Sync, Strings.UI_ResetToDefault))
            {
                this.Config.OnlineFriendShowDelay = TimeSpan.FromSeconds(CurrentFriendsOnlineModuleConfig.OnlineFriendShowDelayDefaultSeconds);
                this.Config.Save();
            }
            SiGui.TextDisabledWrapped(Strings.Modules_CurrentFriendsOnlineModule_UI_SendFriendCount_Delay_Description);
            ImGui.Unindent();
        }
    }

    private void OnLogin()
    {
        // Don't show a message if disabled
        if (!this.Config.ShowOnlineFriendCountOnLogin)
        {
            return;
        }

        Task.Run(async () =>
        {
            // Wait for the delay after login.
            await Task.Delay(this.Config.OnlineFriendShowDelay);
            await DalamudInjections.Framework.RunOnFrameworkThread(() =>
            {
                // Don't show a message if the player has no friends added.
                if (FriendHelper.FriendList.Length is 0)
                {
                    Logger.Debug("Friends list was empty upon logging in");
                    return;
                }

                // Get how many friends are online.
                var onlineFriends = FriendHelper.FriendList.ToArray().Where(x => x.State.HasFlag(InfoProxyCommonList.CharacterData.OnlineStatus.Online));
                var characterData = onlineFriends as InfoProxyCommonList.CharacterData[] ?? [.. onlineFriends];
                var onlineFriendCount = characterData.Length;
                Logger.Debug($"Player has {onlineFriendCount} friends online right now, sending chat message.");
                if (onlineFriendCount is 0)
                {
                    DalamudInjections.ChatGui.Print(Strings.Modules_CurrentFriendsOnlineModule_NoFriendsOnline);
                }
                else
                {
                    if (!this.Config.AddFriendNamesToOnlineCount)
                    {
                        DalamudInjections.ChatGui.Print(string.Format(Strings.Modules_CurrentFriendsOnlineModule_PluralFriendsOnline, onlineFriendCount));
                        return;
                    }
                    var chatMessage = new SeStringBuilder().AddText(onlineFriendCount is 1
                        ? string.Format(Strings.Modules_CurrentFriendsOnlineModule_SingularFriendOnline, onlineFriendCount)
                        : string.Format(Strings.Modules_CurrentFriendsOnlineModule_PluralFriendsOnline, onlineFriendCount));
                    foreach (var friend in characterData)
                    {
                        chatMessage.AddText($"\n - {friend.NameString}");
                        if (DalamudInjections.ObjectTable.LocalPlayer?.HomeWorld.RowId != friend.HomeWorld)
                        {
                            chatMessage.AddText(" ").AddIcon(BitmapFontIcon.CrossWorld).AddText(Services.WorldSheet.GetRow(friend.HomeWorld).Name.ExtractText());
                        }
                    }
                    DalamudInjections.ChatGui.Print(chatMessage.Build());
                }
            });
        });
    }

    /// <summary>
    ///     Configuration for the current online friends module.
    /// </summary>
    private sealed class CurrentFriendsOnlineModuleConfig : BaseModuleConfig
    {
        /// <inheritdoc />
        public override uint Version { get; protected set; }

        /// <inheritdoc />
        protected override string Identifier { get; set; } = "CurrentFriendsOnlineModule";

        /// <summary>
        ///     whether the online friend count notification should be shown.
        /// </summary>
        public bool ShowOnlineFriendCountOnLogin { get; set; } = true;

        /// <summary>
        ///     Whether to add the names of each online friend to messages that show the number of online friends.
        /// </summary>
        public bool AddFriendNamesToOnlineCount { get; set; }

        /// <summary>
        ///     The delay period before showing the "friends online" message when logging in.
        /// </summary>
        /// <remarks>
        ///     One delay may not work for everyone as network conditions differ. This value has been set to a reasonable
        ///     default that one may wish to change later.
        ///  </remarks>
        public TimeSpan OnlineFriendShowDelay { get; set; } = TimeSpan.FromSeconds(OnlineFriendShowDelayDefaultSeconds);
        public const int OnlineFriendShowDelayDefaultSeconds = 8;
        public const int OnlineFriendShowDelayMinSeconds = 8;
        public const int OnlineFriendShowDelayMaxSeconds = 30;
    }
}
