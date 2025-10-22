using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using GoodFriend.Client.Http.Requests;
using GoodFriend.Client.Http.Responses;
using GoodFriend.Plugin.Base;
using GoodFriend.Plugin.Localization;
using GoodFriend.Plugin.Utility;
using Sirensong.Extensions;
using Sirensong.Game.Helpers;
using Sirensong.UserInterface;
using Sirensong.UserInterface.Style;
using Sirensong.Utility;

namespace GoodFriend.Plugin.ModuleSystem.Modules;

/// <inheritdoc />
internal sealed class LoginStateModule : BaseModule
{
    /// <summary>
    ///     A cache of the current players friend list data for usage when it becomes unavailable due to game conditions.
    /// </summary>
    private readonly InfoProxyCommonList.CharacterData[] cachedFriendList = new InfoProxyCommonList.CharacterData[200];

    /// <summary>
    ///     The current world ID.
    /// </summary>
    private uint currentWorldId;

    /// <summary>
    ///     The current territory ID.
    /// </summary>
    private ushort currentTerritoryId;

    /// <summary>
    ///     The current free company ID.
    /// </summary>
    private uint currentHomeworldId;

    /// <summary>
    ///     The current player content ID.
    /// </summary>
    private ulong currentContentId;

    /// <inheritdoc />
    public override string Name => Strings.Modules_LoginStateModule_Name;

    /// <inheritdoc />
    public override string? Description => Strings.Modules_LoginStateModule_Description;

    /// <inheritdoc />
    public override ModuleTag Tag => ModuleTag.Notifications;

    /// <summary>
    ///     The configuration for this module.
    /// </summary>
    private LoginStateModuleConfig Config { get; } = BaseModuleConfig.Load<LoginStateModuleConfig>();

    /// <inheritdoc />
    protected override void OnEnable()
    {
        Services.PlayerEventSseStream.OnStreamMessage += this.HandlePlayerStreamMessage;
        DalamudInjections.Framework.Update += this.OnFrameworkUpdate;
        DalamudInjections.ClientState.Login += this.OnLogin;
        DalamudInjections.ClientState.Logout += this.OnLogout;

        DalamudInjections.Framework.RunOnFrameworkThread(() =>
        {
            if (DalamudInjections.ClientState.IsLoggedIn && DalamudInjections.ClientState.LocalPlayer is not null)
            {
                this.SetStoredValues();
            }
        });
    }

    /// <inheritdoc />
    protected override void OnDisable()
    {
        Services.PlayerEventSseStream.OnStreamMessage -= this.HandlePlayerStreamMessage;
        DalamudInjections.Framework.Update -= this.OnFrameworkUpdate;
        DalamudInjections.ClientState.Login -= this.OnLogin;
        DalamudInjections.ClientState.Logout -= this.OnLogout;

        this.ClearStoredValues();
    }

    /// <inheritdoc />
    protected override void DrawModule()
    {
        // Event settings
        SiGui.Heading(Strings.Modules_LoginStateModule_UI_EventSettings);
        var receiveEvents = this.Config.ReceiveEvents;
        if (SiGui.Checkbox(Strings.Modules_LoginStateModule_UI_EventSettings_ReceieveEvents, Strings.Modules_LoginStateModule_UI_EventSettings_ReceieveEvents_Description, ref receiveEvents))
        {
            this.Config.ReceiveEvents = receiveEvents;
            this.Config.Save();
        }
        ImGui.Dummy(Spacing.ReadableSpacing);

        // Filtering
        using (ImRaii.Disabled(!receiveEvents))
        {

            SiGui.Heading(Strings.Modules_LoginStateModule_UI_FilteringOptions);
            var hideSameFc = this.Config.HideSameFC;
            if (SiGui.Checkbox(Strings.Modules_LoginStateModule_UI_HideSameCompany, Strings.Modules_LoginStateModule_UI_HideSameCompany_Description, ref hideSameFc))
            {
                this.Config.HideSameFC = hideSameFc;
                this.Config.Save();
            }
            ImGui.Dummy(Spacing.ReadableSpacing);

            var hideDifferentHomeworld = this.Config.HideDifferentHomeworld;
            if (SiGui.Checkbox(Strings.Modules_LoginStateModule_UI_HideDiffHomeworld, Strings.Modules_LoginStateModule_UI_HideDiffHomeworld_Description, ref hideDifferentHomeworld))
            {
                this.Config.HideDifferentHomeworld = hideDifferentHomeworld;
                this.Config.Save();
            }
            ImGui.Dummy(Spacing.ReadableSpacing);

            var hideDifferentTerritory = this.Config.HideDifferentTerritory;
            if (SiGui.Checkbox(Strings.Modules_LoginStateModule_UI_HideDiffTerritory, Strings.Modules_LoginStateModule_UI_HideDiffTerritory_Description, ref hideDifferentTerritory))
            {
                this.Config.HideDifferentTerritory = hideDifferentTerritory;
                this.Config.Save();
            }
            ImGui.Dummy(Spacing.ReadableSpacing);

            var hideDifferentWorld = this.Config.HideDifferentWorld;
            if (SiGui.Checkbox(Strings.Modules_LoginStateModule_UI_HideDiffWorld, Strings.Modules_LoginStateModule_UI_HideDiffWorld_Description, ref hideDifferentWorld))
            {
                this.Config.HideDifferentWorld = hideDifferentWorld;
                this.Config.Save();
            }
            ImGui.Dummy(Spacing.ReadableSpacing);

            var hideDifferentDatacenter = this.Config.HideDifferentDatacenter;
            if (SiGui.Checkbox(Strings.Modules_LoginStateModule_UI_HideDiffDC, Strings.Modules_LoginStateModule_UI_HideDiffDC_Description, ref hideDifferentDatacenter))
            {
                this.Config.HideDifferentDatacenter = hideDifferentDatacenter;
                this.Config.Save();
            }
            ImGui.Dummy(Spacing.SectionSpacing);

            // Messages
            SiGui.Heading(Strings.Modules_LoginStateModule_UI_MessageOptions);
            var loginMessage = this.Config.LoginMessage;
            if (SiGui.InputText(Strings.Modules_LoginStateModule_UI_LoginMessage, ref loginMessage, 256, true, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                switch (LoginStateModuleConfig.ValidateMessage(loginMessage))
                {
                    case true:
                        this.Config.LoginMessage = loginMessage.TrimAndSquish();
                        this.Config.Save();
                        break;
                    case false:
                        NotificationUtil.ShowErrorToast(Strings.Modules_LoginStateModule_UI_LoginMessage_Invalid);
                        break;
                }
            }
            SiGui.AddTooltip(Strings.Modules_LoginStateModule_UI_LoginMessage_Tooltip);

            var logoutMessage = this.Config.LogoutMessage;
            if (SiGui.InputText(Strings.Modules_LoginStateModule_UI_LogoutMessage, ref logoutMessage, 256, true, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                switch (LoginStateModuleConfig.ValidateMessage(logoutMessage))
                {
                    case true:
                        this.Config.LogoutMessage = logoutMessage.TrimAndSquish();
                        this.Config.Save();
                        break;
                    case false:
                        NotificationUtil.ShowErrorToast(Strings.Modules_LoginStateModule_UI_LogoutMessage_Invalid);
                        break;
                }
            }
            SiGui.AddTooltip(Strings.Modules_LoginStateModule_UI_LogoutMessage_Tooltip);
        }
    }

    /// <summary>
    ///     Called when a player state update is received.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="update"></param>
    private unsafe void HandlePlayerStreamMessage(object? sender, PlayerEventStreamUpdate update)
    {
        if (!this.Config.ReceiveEvents)
        {
            return;
        }
        DalamudInjections.Framework.RunOnFrameworkThread(() =>
        {
            var localPlayer = DalamudInjections.ClientState.LocalPlayer;
            if (localPlayer is null)
            {
                Logger.Verbose("Ignoring player state update as the local player is null (not logged in?).");
                return;
            }

            // Skip if the event player is not on the player's friends list.
            if (FriendUtil.GetFriendFromHash(this.cachedFriendList, update.ContentIdHash, update.ContentIdSalt) is not { } friendCharacterData)
            {
                Logger.Verbose(message: $"Ignoring player event as a friend could not be found from the received hash.");
                return;
            }

            // Ignore the event if the friend request is pending on the player's side.
            if (friendCharacterData.ExtraFlags is Constants.WaitingForFriendListApprovalStatus)
            {
                return;
            }

            // Ignore the event if the friend has no name.
            // This state shouldn't be possible, but it has been observed a few times.
            if (string.IsNullOrWhiteSpace(friendCharacterData.NameString))
            {
                Logger.Warning($"Login state event matched but the name was null or empty.");
                return;
            }

            // Evaluate eligibility.
            if (this.Config.HideSameFC && friendCharacterData.FCTagString == localPlayer.CompanyTag.TextValue)
            {
                Logger.Debug($"Ignoring login state update from {friendCharacterData.NameString} - same free company.");
                return;
            }
            if (this.Config.HideDifferentHomeworld && friendCharacterData.HomeWorld != this.currentHomeworldId)
            {
                Logger.Debug($"Ignoring login state update from {friendCharacterData.NameString} - different homeworld.");
                return;
            }
            if (this.Config.HideDifferentTerritory && update.TerritoryId != this.currentTerritoryId)
            {
                Logger.Debug($"Ignoring login state update from {friendCharacterData.NameString} - different territory.");
                return;
            }
            if (this.Config.HideDifferentWorld && update.WorldId != this.currentWorldId)
            {
                Logger.Debug($"Ignoring login state update from {friendCharacterData.NameString} - different world.");
                return;
            }
            if (this.Config.HideDifferentDatacenter && Services.WorldSheet.GetRow(update.WorldId).DataCenter.RowId != localPlayer.CurrentWorld.Value.DataCenter.RowId)
            {
                Logger.Debug($"Ignoring login state update from {friendCharacterData.NameString} - different data center.");
                return;
            }

            DalamudInjections.ChatGui.Print(update.LoggedIn
                ? this.Config.LoginMessage.Format(friendCharacterData.NameString)
                : this.Config.LogoutMessage.Format(friendCharacterData.NameString)
            );
        });
    }

    /// <summary>
    ///     Set stored values and send a login event.
    /// </summary>
    private void OnLogin()
    {
        this.SetStoredValues();
        Logger.Debug("Sending login event.");
        var salt = CryptoUtil.GenerateSalt();
        var hash = CryptoUtil.HashValueWithSalt(this.currentContentId, salt);
        new PostPlayerLoginStateRequest().Send(Services.HttpClient, new()
        {
            ContentIdHash = hash,
            ContentIdSalt = salt,
            LoggedIn = true,
            TerritoryId = this.currentTerritoryId,
            WorldId = this.currentWorldId,
        });
    }

    /// <summary>
    ///     Send a logout event and unset stored values.
    /// </summary>
    private void OnLogout(int type, int code)
    {
        var salt = CryptoUtil.GenerateSalt();
        var hash = CryptoUtil.HashValueWithSalt(this.currentContentId, salt);
        Logger.Debug("Sending logout event.");
        new PostPlayerLoginStateRequest().Send(Services.HttpClient, new()
        {
            ContentIdHash = hash,
            ContentIdSalt = salt,
            LoggedIn = false,
            TerritoryId = this.currentTerritoryId,
            WorldId = this.currentWorldId,
        });
        this.ClearStoredValues();
    }

    /// <summary>
    ///     Update stored data that can change during gameplay.
    /// </summary>
    private void OnFrameworkUpdate(IFramework framework)
    {
        // Don't run when not logged in.
        if (!DalamudInjections.ClientState.IsLoggedIn || DalamudInjections.ClientState.LocalPlayer is null)
        {
            return;
        }

        // Update friends list cache.
        var friendsList = FriendHelper.FriendList;
        if (!friendsList.IsEmpty && friendsList.Length != 0)
        {
            friendsList.CopyTo(this.cachedFriendList);
        }

        // Update current world.
        var currentWorld = DalamudInjections.ClientState.LocalPlayer?.CurrentWorld;
        if (currentWorld.HasValue && currentWorld.Value.RowId != 0 && currentWorld.Value.RowId != this.currentWorldId)
        {
            this.currentWorldId = currentWorld.Value.RowId;
        }

        // Update current territory.
        var currentTerritory = DalamudInjections.ClientState.TerritoryType;
        if (currentTerritory != 0 && currentTerritory != this.currentTerritoryId)
        {
            this.currentTerritoryId = currentTerritory;
        }
    }

    /// <summary>
    ///     Sets stored values to their current values, will error if the local player is null.
    /// </summary>
    private void SetStoredValues()
    {
        this.currentContentId = DalamudInjections.ClientState.LocalContentId;
        this.currentHomeworldId = DalamudInjections.ClientState.LocalPlayer!.HomeWorld.RowId;
        this.currentTerritoryId = DalamudInjections.ClientState.TerritoryType;
        this.currentWorldId = DalamudInjections.ClientState.LocalPlayer.CurrentWorld.RowId;

        Logger.Debug($"Set stored values: CID: {this.currentContentId}, HW: {this.currentHomeworldId}, T: {this.currentTerritoryId}, W: {this.currentWorldId}");
    }

    /// <summary>
    ///     Clears all stored values.
    /// </summary>
    private void ClearStoredValues()
    {
        this.currentContentId = 0;
        this.currentHomeworldId = 0;
        this.currentTerritoryId = 0;
        this.currentWorldId = 0;

        Logger.Debug("Cleared stored all values.");
    }

    /// <inheritdoc />
    private sealed class LoginStateModuleConfig : BaseModuleConfig
    {
        /// <inheritdoc />
        public override uint Version { get; protected set; }

        /// <inheritdoc />
        protected override string Identifier { get; set; } = "LoginStateModule";

        /// <summary>
        ///     Whether to receive login state events.
        /// </summary>
        public bool ReceiveEvents { get; set; } = true;

        /// <summary>
        ///     Whether to hide notifications for the same free company.
        /// </summary>
        public bool HideSameFC { get; set; } = true;

        /// <summary>
        ///     Whether to hide notifications from users from different homeworlds.
        /// </summary>
        public bool HideDifferentHomeworld { get; set; }

        /// <summary>
        ///     Whether to hide notifications from users in different territories.
        /// </summary>
        public bool HideDifferentTerritory { get; set; }

        /// <summary>
        ///     Whether to hide notifications from users in different worlds.
        /// </summary>
        public bool HideDifferentWorld { get; set; }

        /// <summary>
        ///     Whether to hide notifications from users in different data centers.
        /// </summary>
        public bool HideDifferentDatacenter { get; set; } = true;

        /// <summary>
        ///     The message to display when a friend logs in.
        /// </summary>
        public string LoginMessage { get; set; } = "{0} has logged in.";

        /// <summary>
        ///     The message to display when a friend logs out.
        /// </summary>
        public string LogoutMessage { get; set; } = "{0} has logged out.";

        /// <summary>
        ///     Validates a login/logout message.
        /// </summary>
        /// <param name="message">The message to validate.</param>
        /// <returns>Whether the message is valid.</returns>
        public static bool ValidateMessage(string message) => Validator.TestFormatString(message, 1);
    }
}
