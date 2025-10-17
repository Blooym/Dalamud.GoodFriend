using System.Text.Json.Serialization;
using GoodFriend.Client.Http.Enums;

namespace GoodFriend.Client.Http.Responses;

/// <summary>
///     Represents a response from the announcement stream.
/// </summary>
public record struct AnnouncementStreamUpdate
{
    /// <summary>
    ///     The message of the announcement.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }

    /// <summary>
    ///     The kind of the announcement.
    /// </summary>
    [JsonPropertyName("kind"), JsonConverter(typeof(JsonStringEnumConverter))]
    public AnnouncementKind Kind { get; set; }

    /// <summary>
    ///     The channel of the announcement.
    /// </summary>
    [JsonPropertyName("channel")]
    public string? Channel { get; set; }
}
