using System.Net.Http;
using GoodFriend.Client.Http.Responses;

namespace GoodFriend.Client.Http.Requests
{
    /// <summary>
    ///     Represents a request for the player event stream.
    /// </summary>
    public static class GetPlayerEventStreamRequest
    {
        private const string EndpointUrl = "api/playerevents/stream";

        /// <summary>
        ///     Creates a new SSE client for the player event stream using its endpoint URL.
        /// </summary>
        /// <param name="httpClient">The HTTP client to use for requests. This must be a unique client as it will be managed once initialized.</param>
        /// <param name="settings">The settings for this client.</param>
        /// <returns></returns>
        public static SseClient<PlayerEventStreamUpdate> CreateSSEClient(HttpClient httpClient, SSEClientSettings settings) => new(httpClient, EndpointUrl, settings);

    }
}
