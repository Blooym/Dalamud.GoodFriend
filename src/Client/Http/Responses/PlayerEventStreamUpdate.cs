using MessagePack;

namespace GoodFriend.Client.Http.Responses;

/// <summary>
///     Represents the response data for a player event stream update.
/// </summary>
[MessagePackObject(AllowPrivate = true)]
public record struct PlayerEventStreamUpdate
{
    /// <summary>
    ///     The ContentID Hash of the player.
    /// </summary>
    [Key(0)]
    public string ContentIdHash { get; set; }

    /// <summary>
    ///     The salt used when hashing the player's ContentID.
    /// </summary>
    [Key(1)]
    public string ContentIdSalt { get; set; }

    /// <summary>
    ///     The state update type of the event.
    /// </summary>
    [Key(2)]
    public StateUpdateType StateUpdateType { get; set; }
}

/// <summary>
///     Represents the state update type of player event stream update.
/// </summary>
[MessagePackObject(AllowPrivate = true)]
public record struct StateUpdateType
{
    /// <summary>
    ///     Represents the data received relating to a login state change.
    /// </summary>
    [Key(0)]
    public LoginStateChangeData? LoginStateChange { get; set; }
}

/// <summary>
///     Represents the data received relating to a login state change.
/// </summary>
[MessagePackObject(AllowPrivate = true)]
public record struct LoginStateChangeData
{
    /// <summary>
    ///     Whether the event is a login or logout.
    /// </summary>
    [Key(0)]
    public bool LoggedIn { get; set; }

    /// <summary>
    ///     The ID of the territory the event occured in.
    /// </summary>
    [Key(1)]
    public ushort TerritoryId { get; set; }

    /// <summary>
    ///     The ID of the world the event occured in.
    /// </summary>
    [Key(2)]
    public uint WorldId { get; set; }
}
