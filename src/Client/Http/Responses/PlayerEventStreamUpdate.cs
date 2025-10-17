using System.Text.Json.Serialization;

namespace GoodFriend.Client.Http.Responses;

/// <summary>
///     Represents the response data for a player event stream update.
/// </summary>
public record struct PlayerEventStreamUpdate
{
    /// <summary>
    ///     The ContentID Hash of the player.
    /// </summary>
    [JsonPropertyName("content_id_hash")]
    public string ContentIdHash { get; set; }

    /// <summary>
    ///     The salt used when hashing the player's ContentID.
    /// </summary>
    [JsonPropertyName("content_id_salt")]
    public string ContentIdSalt { get; set; }

    /// <summary>
    ///     The state update type of the event.
    /// </summary>
    [JsonPropertyName("state_update_type")]
    public StateUpdateType StateUpdateType { get; set; }
}

/// <summary>
///     Represents the state update type of player event stream update.
/// </summary>
public record struct StateUpdateType
{
    /// <summary>
    ///     Represents the data received relating to a login state change.
    /// </summary>
    [JsonPropertyName("LoginStateChange")]
    public LoginStateChangeData? LoginStateChange { get; set; }
}

/// <summary>
///     Represents the data received relating to a login state change.
/// </summary>
public record struct LoginStateChangeData
{
    /// <summary>
    ///     Whether the event is a login or logout.
    /// </summary>
    [JsonPropertyName("logged_in")]
    public bool LoggedIn { get; set; }

    /// <summary>
    ///     The ID of the territory the event occured in.
    /// </summary>
    [JsonPropertyName("territory_id")]
    public ushort TerritoryId { get; set; }

    /// <summary>
    ///     The ID of the world the event occured in.
    /// </summary>
    [JsonPropertyName("world_id")]
    public uint WorldId { get; set; }
}
