using System;
using System.Net;
using System.Net.Http;
using Dalamud.Networking.Http;
using GoodFriend.Client.Http;
using GoodFriend.Client.Http.Requests;
using GoodFriend.Client.Http.Responses;
using GoodFriend.Plugin.Configuration;
using GoodFriend.Plugin.Localization;
using GoodFriend.Plugin.ModuleSystem;
using GoodFriend.Plugin.UserInterface;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace GoodFriend.Plugin.Base;

/// <summary>
///     Provides access to necessary instances and services.
/// </summary>
internal static class Services
{
    // SSE
    private static readonly TimeSpan SseReconnectDelayMin = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan SseReconnectDelayMax = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan SseReconnectDelayIncrement = TimeSpan.FromSeconds(30);
    public static StreamClient<PlayerEventStreamUpdate> PlayerEventSseStream { get; private set; } = null!;

    private static WindowingService WindowingService { get; set; } = null!;
    private static LocalizationManager LocalizationService { get; set; } = null!;
    private static HappyEyeballsCallback HappyEyeballsCallback { get; set; } = null!;
    public static PluginConfiguration PluginConfiguration { get; private set; } = null!;
    public static ModuleService ModuleService { get; private set; } = null!;
    public static ExcelSheet<World> WorldSheet { get; private set; } = null!;
    public static HttpClient HttpClient { get; private set; } = null!;

    /// <summary>
    ///     Initializes the service class.
    /// </summary>
    internal static void Initialize()
    {
        HappyEyeballsCallback = new HappyEyeballsCallback();
        PluginConfiguration = PluginConfiguration.Load();
        LocalizationService = new LocalizationManager();
        HttpClient = CreateHttpClient(HappyEyeballsCallback);
        PlayerEventSseStream = GetPlayerEventStreamRequest.CreateSseClient(CreateHttpClient(HappyEyeballsCallback), new()
        {
            ReconnectDelayMin = SseReconnectDelayMin,
            ReconnectDelayMax = SseReconnectDelayMax,
            ReconnectDelayIncrement = SseReconnectDelayIncrement
        });
        WorldSheet = DalamudInjections.DataManager.GetExcelSheet<World>();
        ModuleService = new ModuleService();
        WindowingService = new WindowingService();
    }

    internal static void Dispose()
    {
        LocalizationService.Dispose();
        WindowingService.Dispose();
        ModuleService.Dispose();
        HttpClient.Dispose();
        PlayerEventSseStream.Dispose();
        HappyEyeballsCallback.Dispose();
    }

    /// <summary>
    ///     Creates a new <see cref="HttpClient" /> with the given <see cref="HappyEyeballsCallback" />.
    /// </summary>
    /// <param name="happyEyeballsCallback"></param>
    /// <returns></returns>
    private static HttpClient CreateHttpClient(HappyEyeballsCallback happyEyeballsCallback) => new(new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        ConnectCallback = happyEyeballsCallback.ConnectCallback,
    })
    {
        BaseAddress = PluginConfiguration.ApiConfig.BaseUrl,
        Timeout = TimeSpan.FromSeconds(15),
        DefaultRequestHeaders =
            {
                { "User-Agent", HttpConstants.UserAgent },
            },
        DefaultRequestVersion = HttpVersion.Version20,
        DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
    };
}
