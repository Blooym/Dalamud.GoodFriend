using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Timers;
using System.Web;

namespace GoodFriend.Client.Http;

public enum SseConnectionState
{
    /// <summary>
    ///     The client is currently connected to the stream.
    /// </summary>
    Connected,

    /// <summary>
    ///     The client is currently connecting to the stream.
    /// </summary>
    Connecting,

    /// <summary>
    ///     The client is currently disconnecting from the stream.
    /// </summary>
    Disconnecting,

    /// <summary>
    ///     The client is not currently connected to the stream.
    /// </summary>
    Disconnected,

    /// <summary>
    ///     Something went wrong with the stream.
    /// </summary>
    Exception,
}

public readonly struct SseClientSettings
{
    /// <summary>
    ///     The minimum amount of time to wait between reconnection attempts
    /// </summary>
    public TimeSpan ReconnectDelayMin { get; init; }

    /// <summary>
    ///     The maximum amount of time to wait between reconnection attempts
    /// </summary>
    public TimeSpan ReconnectDelayMax { get; init; }

    /// <summary>
    ///     The reconnection delay increment value, every failed attempt will add this value to the reconnect attempt interval.
    /// </summary>
    public TimeSpan ReconnectDelayIncrement { get; init; }
}

/// <summary>
///     Represents a client for a server-sent event stream.
/// </summary>
/// <typeparam name="T">The type of data to expect from the stream.</typeparam>
public sealed class SseClient<T> : IDisposable where T : struct
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        IncludeFields = true,
    };

    private bool disposedValue;

    /// <summary>
    ///     The HTTP client to use for requests.
    /// </summary>
    private readonly HttpClient httpClient;

    /// <summary>
    ///     The URL to connect to and receive events from.
    /// </summary>
    private readonly string url;

    /// <summary>
    ///     The timer to use for reconnecting.
    /// </summary>
    private readonly Timer reconnectTimer;

    /// <summary>
    ///     The settings for this client.
    /// </summary>
    private readonly SseClientSettings settings;

    /// <summary>
    ///     The current connection state.
    /// </summary>
    public SseConnectionState ConnectionState { get; private set; } = SseConnectionState.Disconnected;

    /// <summary>
    ///     Fires when the stream is connected.
    /// </summary>
    public event Action<SseClient<T>>? OnStreamConnected;

    /// <summary>
    ///     Fires when the stream is disconnected.
    /// </summary>
    public event Action<SseClient<T>>? OnStreamDisconnected;

    /// <summary>
    ///     Fires when the stream sends a heartbeat.
    /// </summary>
    public event Action<SseClient<T>>? OnStreamHeartbeat;

    /// <summary>
    ///     Fires when the stream throws an exception.
    /// </summary>
    public event Action<SseClient<T>, Exception>? OnStreamException;

    /// <summary>
    ///     Fires when the stream sends a message.
    /// </summary>
    public event Action<SseClient<T>, T>? OnStreamMessage;

    /// <summary>
    ///     Creates a new SSE client.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for requests. This must be a unique client as it will be managed once initialized.</param>
    /// <param name="url">The url to connect to.</param>
    /// <param name="settings">The settings for this client.</param>
    public SseClient(HttpClient httpClient, string url, SseClientSettings settings)
    {
        this.httpClient = httpClient;
        this.url = url;
        this.settings = settings;

        // Reconnect handling
        this.reconnectTimer = new(this.settings.ReconnectDelayMin);
        this.reconnectTimer.Elapsed += this.HandleReconnectTimerElapse;
        this.OnStreamException += this.HandleStreamException;
        this.OnStreamConnected += this.HandleStreamConnected;
        this.OnStreamDisconnected += this.HandleStreamDisconnected;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (this.disposedValue)
        {
            return;
        }

        // Dispose of the timer.
        this.reconnectTimer.Elapsed -= this.HandleReconnectTimerElapse;
        this.OnStreamConnected -= this.HandleStreamConnected;
        this.OnStreamDisconnected -= this.HandleStreamDisconnected;
        this.OnStreamHeartbeat -= this.HandleStreamConnected;
        this.OnStreamException -= this.HandleStreamException;
        this.reconnectTimer.Dispose();

        // Disconnect from the stream.
        try
        {
            this.Disconnect();
        }
        catch
        {
            // ignored
        }

        // Dispose of the HTTP client.
        this.httpClient.CancelPendingRequests();
        this.httpClient.Dispose();

        this.disposedValue = true;
    }

    /// <summary>
    ///     Handles the reconnect timer elapsing.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HandleReconnectTimerElapse(object? sender, ElapsedEventArgs e)
    {
        // Already reconnected, reset and stop the timer.
        if (this.ConnectionState is not SseConnectionState.Exception)
        {
            this.reconnectTimer.Interval = this.settings.ReconnectDelayMin.TotalMilliseconds;
            this.reconnectTimer.Stop();
            return;
        }

        this.Connect();

        // Increment delay unless we're already at the maximum delay.
        if (this.reconnectTimer.Interval < this.settings.ReconnectDelayMax.TotalMilliseconds)
        {
            this.reconnectTimer.Interval += this.settings.ReconnectDelayIncrement.TotalMilliseconds;
        }
    }

    /// <summary>
    ///     Handles the stream throwing an exception.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="exception"></param>
    private void HandleStreamException(object? sender, Exception exception) => this.reconnectTimer.Start();

    /// <summary>
    ///     Handles the stream connecting.
    /// </summary>
    /// <param name="sender"></param>
    private void HandleStreamConnected(object? sender)
    {
        this.reconnectTimer.Interval = this.settings.ReconnectDelayMin.TotalMilliseconds;
        this.reconnectTimer.Stop();
    }

    /// <summary>
    ///     Handles the stream disconnecting.
    /// </summary>
    /// <param name="sender"></param>
    private void HandleStreamDisconnected(object? sender)
    {
        this.reconnectTimer.Interval = this.settings.ReconnectDelayMin.TotalMilliseconds;
        this.reconnectTimer.Stop();
    }

    /// <summary>
    ///     Connects to the event stream.
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    public async void Connect()
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, nameof(SseClient<T>));

        if (this.ConnectionState is SseConnectionState.Connecting or SseConnectionState.Connected)
        {
            return;
        }

        try
        {
            this.ConnectionState = SseConnectionState.Connecting;

            await using var stream = await this.httpClient.GetStreamAsync(this.url);
            using var reader = new StreamReader(stream);
            Exception? exception = null;

            this.ConnectionState = SseConnectionState.Connected;
            this.OnStreamConnected?.Invoke(this);

            while (!reader.EndOfStream && this.ConnectionState == SseConnectionState.Connected)
            {
                var message = await reader.ReadLineAsync();
                message = HttpUtility.UrlDecode(message);

                if (message is null || message.Trim() == ":")
                {
                    this.OnStreamHeartbeat?.Invoke(this);
                    continue;
                }

                message = message.Replace("data:", "").Trim();
                if (string.IsNullOrEmpty(message))
                {
                    continue;
                }

                try
                {
                    var data = JsonSerializer.Deserialize<T>(message, JsonSerializerOptions);
                    this.OnStreamMessage?.Invoke(this, data);
                }
                catch (JsonException)
                {
                }
                catch (Exception e)
                {
                    exception = e;
                    break;
                }
            }

            if (reader.EndOfStream && this.ConnectionState == SseConnectionState.Connected)
            {
                this.ConnectionState = SseConnectionState.Exception;
                this.OnStreamException?.Invoke(this, exception ?? new HttpRequestException("Connection to stream suddenly closed."));
            }
        }
        catch (Exception e)
        {
            this.ConnectionState = SseConnectionState.Exception;
            this.OnStreamException?.Invoke(this, e);
        }
    }

    /// <summary>
    ///     Disconnects from the event stream.
    /// </summary>
    /// <exception cref="ObjectDisposedException"></exception>
    public void Disconnect()
    {
        ObjectDisposedException.ThrowIf(this.disposedValue, nameof(SseClient<T>));
        if (this.ConnectionState is SseConnectionState.Disconnecting or SseConnectionState.Disconnected)
        {
            return;
        }
        this.ConnectionState = SseConnectionState.Disconnecting;
        this.httpClient.CancelPendingRequests();
        this.ConnectionState = SseConnectionState.Disconnected;
        this.OnStreamDisconnected?.Invoke(this);
    }
}
