using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GoodFriend.Client.Http.Interfaces;
using MessagePack;

namespace GoodFriend.Client.Http.Requests;

/// <summary>
///     Represents the request data for sending a player login state.
/// </summary>
public sealed class PostPlayerLoginStateRequest : IHttpRequestHandler<PostPlayerLoginStateRequest.RequestData, HttpResponseMessage>
{
    private const string EndpointUrl = "api/event";


    [MessagePackObject(AllowPrivate = true)]
    internal readonly struct RequestBody
    {
        [Key(0)]
        public required byte[] ContentIdHash { get; init; }
        [Key(1)]
        public required byte[] ContentIdSalt { get; init; }
        [Key(2)]
        public required bool LoggedIn { get; init; }
        [Key(3)]
        public required ushort TerritoryId { get; init; }
        [Key(4)]
        public required uint WorldId { get; init; }
    }

    public readonly record struct RequestData
    {
        private readonly byte[] contentIdHashBackingField;

        /// <summary>
        ///     The hash of the player's ContentId.
        /// </summary>
        /// <remarks>
        ///     - This value must be 32 bytes in length.<br/>
        /// </remarks>
        public required byte[] ContentIdHash
        {
            get => this.contentIdHashBackingField; init
            {
                if ((uint)value.Length is RequestConstants.Validation.ContentIdHashLength)
                {
                    throw new ArgumentException("ContentIdHash must be exactly 32 bytes in length");
                }
                this.contentIdHashBackingField = value;
            }
        }

        private readonly byte[] contentIdSaltBackingField;

        /// <summary>
        ///     The salt used when hashing the player's ContentId.
        /// </summary>
        /// <remarks>
        ///     - This value must be 16 bytes in length.
        /// </remarks>
        public required byte[] ContentIdSalt
        {
            get => this.contentIdSaltBackingField; init
            {
                if ((uint)value.Length is RequestConstants.Validation.ContentIdSaltLength)
                {
                    throw new ArgumentException("ContentIdSalt must be exactly 16 bytes in length");
                }
                this.contentIdSaltBackingField = value;
            }
        }

        /// <summary>
        ///     Whether the player is now logged in.
        /// </summary>
        public required bool LoggedIn { get; init; }

        /// <summary>
        ///     The ID of the player's current Territory.
        /// </summary>
        public required ushort TerritoryId { get; init; }

        /// <summary>
        ///     The ID player's current World.
        /// </summary>
        public required uint WorldId { get; init; }
    }

    /// <summary>
    ///     Builds the request message.
    /// </summary>
    /// <param name="requestData"></param>
    /// <returns></returns>
    private static HttpRequestMessage BuildMessage(RequestData requestData) => new(HttpMethod.Post, EndpointUrl)
    {
        Content = new ByteArrayContent(MessagePackSerializer.Serialize(new RequestBody()
        {
            LoggedIn = requestData.LoggedIn,
            TerritoryId = requestData.TerritoryId,
            WorldId = requestData.WorldId,
            ContentIdHash = requestData.ContentIdHash,
            ContentIdSalt = requestData.ContentIdSalt
        }))
        {
            Headers = {
                ContentType = new MediaTypeHeaderValue("application/msgpack")
            }
        },
    };

    /// <inheritdoc />
    public HttpResponseMessage Send(HttpClient httpClient, RequestData requestData)
    {
        var message = BuildMessage(requestData);
        return httpClient.Send(message);
    }

    /// <inheritdoc />
    public Task<HttpResponseMessage> SendAsync(HttpClient httpClient, RequestData requestData)
    {
        var message = BuildMessage(requestData);
        return httpClient.SendAsync(message);
    }
}
