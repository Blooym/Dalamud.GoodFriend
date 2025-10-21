using System;
using Dalamud.Bindings.ImGui;
using GoodFriend.Client.Http;
using GoodFriend.Client.Http.Responses;
using GoodFriend.Plugin.Base;
using GoodFriend.Plugin.Localization;
using Sirensong.UserInterface;
using Sirensong.UserInterface.Style;

namespace GoodFriend.Plugin.ModuleSystem.Modules;

internal sealed class PlayerEventStreamModule : BaseModule
{
    /// <summary>
    ///     The last time a heartbeat was received from the player event stream.
    /// </summary>
    private DateTime LastHeartbeatTime { get; set; } = DateTime.MinValue;

    /// <summary>
    ///     The last time an event was received from the player event stream.
    /// </summary>
    private DateTime LasEventTime { get; set; } = DateTime.MinValue;

    /// <summary>
    ///     The number of events received from the player event stream.
    /// </summary>
    private uint EventsReceived { get; set; }

    /// <summary>
    ///     The number of heartbeats received from the player event stream.
    /// </summary>
    private uint HeartbeatsReceived { get; set; }

    /// <inheritdoc />
    public override string Name => Strings.Modules_PlayerStreamConnectionModule_Name;

    /// <inheritdoc />
    public override ModuleTag Tag => ModuleTag.Connectivity;

    /// <inheritdoc />
    public override uint LoadPriority { get; } = 1;

    /// <inheritdoc />
    protected override void OnEnable()
    {
        DalamudInjections.Framework.RunOnFrameworkThread(() =>
        {
            if (DalamudInjections.ClientState.IsLoggedIn && IsPlayerStreamDisconnected())
            {
                Services.PlayerEventSseStream.Connect();
            }
        });
        DalamudInjections.ClientState.Login += this.OnLogin;
        DalamudInjections.ClientState.Logout += this.OnLogout;
        Services.PlayerEventSseStream.OnStreamHeartbeat += this.OnPlayerStreamHeartbeat;
        Services.PlayerEventSseStream.OnStreamMessage += this.OnPlayerStreamEvent;
        Services.PlayerEventSseStream.OnStreamException += this.OnPlayerStreamException;
    }

    /// <inheritdoc />
    protected override void OnDisable()
    {
        if (IsPlayerStreamConnected())
        {
            Services.PlayerEventSseStream.Connect();
        }
        DalamudInjections.ClientState.Login -= this.OnLogin;
        DalamudInjections.ClientState.Logout -= this.OnLogout;
        Services.PlayerEventSseStream.OnStreamHeartbeat -= this.OnPlayerStreamHeartbeat;
        Services.PlayerEventSseStream.OnStreamMessage -= this.OnPlayerStreamEvent;
        Services.PlayerEventSseStream.OnStreamException -= this.OnPlayerStreamException;
    }

    /// <inheritdoc />
    protected override void DrawModule()
    {
        SiGui.TextWrapped(Strings.Modules_PlayerStreamConnectionModule_ConnectionStatus);
        ImGui.SameLine();
        switch (Services.PlayerEventSseStream.ConnectionState)
        {
            case StreamClientConnectionState.Connected:
                SiGui.TextColoured(Colours.Success, Strings.Modules_PlayerStreamConnectionModule_ConnectionStatus_Connected);
                break;
            case StreamClientConnectionState.Connecting:
                SiGui.TextColoured(Colours.Warning, Strings.Modules_PlayerStreamConnectionModule_ConnectionStatus_Connecting);
                break;
            case StreamClientConnectionState.Disconnected:
                SiGui.TextColoured(Colours.Error, Strings.Modules_PlayerStreamConnectionModule_ConnectionStatus_Disconnected);
                break;
            case StreamClientConnectionState.Disconnecting:
                SiGui.TextColoured(Colours.Warning, Strings.Modules_PlayerStreamConnectionModule_ConnectionStatus_Disconnecting);
                break;
            case StreamClientConnectionState.Exception:
                SiGui.TextColoured(Colours.Error, Strings.Modules_PlayerStreamConnectionModule_ConnectionStatus_Exception);
                break;
        }
        ImGui.Dummy(Spacing.SectionSpacing);
        SiGui.Heading(Strings.Modules_PlayerStreamConnectionModule_ConnectionStatistics);
        SiGui.TextWrapped(string.Format(Strings.Modules_PlayerStreamConnectionModule_ConnectionStatistics_EventsTotal, this.EventsReceived));
        SiGui.TextWrapped(string.Format(Strings.Modules_PlayerStreamConnectionModule_ConnectionStatistics_LastEvent, $"{this.LasEventTime:HH:mm:ss}"));
        SiGui.TextWrapped(string.Format(Strings.Modules_PlayerStreamConnectionModule_ConnectionStatistics_HeartbeatsTotal, this.HeartbeatsReceived));
        SiGui.TextWrapped(string.Format(Strings.Modules_PlayerStreamConnectionModule_ConnectionStatistics_LastHeartbeat, $"{this.LastHeartbeatTime:HH:mm:ss}"));
    }

    /// <summary>
    ///     Attempts to connect to relevant event streams when the player logs in.
    /// </summary>
    private void OnLogin()
    {
        if (IsPlayerStreamDisconnected())
        {
            Services.PlayerEventSseStream.Connect();
        }
    }

    /// <summary>
    ///     Attempts to disconnect from relevant event streams when the player logs out.
    /// </summary>
    private void OnLogout(int type, int code)
    {
        if (IsPlayerStreamConnected())
        {
            Services.PlayerEventSseStream.Disconnect();
        }
    }

    /// <summary>
    ///     Handles a heartbeat from the player event stream.
    /// </summary>
    /// <param name="sender"></param>
    private void OnPlayerStreamHeartbeat(object? sender)
    {
        Logger.Verbose("Heartbeat received from event stream.");
        this.LastHeartbeatTime = DateTime.Now;
        this.HeartbeatsReceived++;
    }

    /// <summary>
    ///     Handles an event from the player event stream.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnPlayerStreamEvent(object? sender, PlayerEventStreamUpdate e)
    {
        Logger.Verbose($"Event received from player event stream.");
        this.LasEventTime = DateTime.Now;
        this.EventsReceived++;
    }

    /// <summary>
    ///     Handles an exception from the player event stream.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnPlayerStreamException(object? sender, Exception e) => Logger.Error($"Exception received from event stream: {e}");

    /// <summary>
    ///     If the player event stream is connected or connecting.
    /// </summary>
    /// <returns></returns>
    private static bool IsPlayerStreamConnected() => Services.PlayerEventSseStream.ConnectionState is StreamClientConnectionState.Connected or StreamClientConnectionState.Connecting;

    /// <summary>
    ///     If the player event stream is disconnected or disconnecting.
    /// </summary>
    /// <returns></returns>
    private static bool IsPlayerStreamDisconnected() => Services.PlayerEventSseStream.ConnectionState is StreamClientConnectionState.Disconnected or StreamClientConnectionState.Disconnecting;
}
