using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using GoodFriend.Client.Http.Enums;
using GoodFriend.Client.Http.Interfaces;
using MessagePack;

namespace GoodFriend.Client.Http.Requests;

public sealed class PostAnnouncementRequest : IHttpRequestHandler<PostAnnouncementRequest.RequestData, HttpResponseMessage>
{
    private const string EndpointUrl = "api/announcements/send";

    [MessagePackObject]
    public readonly record struct RequestData
    {
        /// <summary>
        ///     The authentication token to use with the request.
        /// </summary>
        [IgnoreMember]
        public string AuthenticationToken { get; init; }

        /// <summary>
        ///     The message to send.
        /// </summary>
        [Key(0)]
        public required string Message { get; init; }

        /// <summary>
        ///     The kind of announcement to send.
        /// </summary>
        [Key(1)]
        public required AnnouncementKind Kind { get; init; }

        /// <summary>
        ///     The channel for the announcement.
        /// </summary>
        [Key(2)]
        public string? Channel { get; init; }
    }

    private static HttpRequestMessage BuildMessage(RequestData requestData) => new(HttpMethod.Post, EndpointUrl)
    {
        Content = new ByteArrayContent(MessagePackSerializer.Serialize(requestData))
        {
            Headers = {
                ContentType = new MediaTypeHeaderValue("application/x-msgpack")
            }
        },
        Headers =
        {
            { GlobalRequestData.Headers.AuthenticationToken, requestData.AuthenticationToken },
        },
    };

    /// <inheritdoc/>
    public HttpResponseMessage Send(HttpClient httpClient, RequestData requestData) => httpClient.Send(BuildMessage(requestData));

    /// <inheritdoc/>
    public Task<HttpResponseMessage> SendAsync(HttpClient httpClient, RequestData requestData) => httpClient.SendAsync(BuildMessage(requestData));
}
