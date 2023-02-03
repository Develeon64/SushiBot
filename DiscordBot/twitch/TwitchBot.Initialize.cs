using TwitchLib.Client.Models;
using Dietze.helper;
using Dietze.Utils.Config;

namespace Dietze.Twitch;

public partial class TwitchBot
{
    private readonly Timer _tokenTimer;

    public TwitchBot()
    {
        _tokenTimer = new Timer(TokenTimer_Tick, null, Timeout.Infinite, 300000);
        Initialize();
    }

    private async void TokenTimer_Tick(object? stateInfo)
    {
        await TwitchTokenCheck.CheckTokens(_api);
    }


    TwitchMessages[] ChatMessages = Enumerable.Repeat(new TwitchMessages(), 100).ToArray();

    private async void Initialize()
    {
        await InitializeApi();
        InitializeClient();
        InitializePubSub();
    }

    private async Task InitializeApi()
    {
        _api.Settings.ClientId = ConfigManager.Auth.Twitch.Client.Id;
        _api.Settings.Secret = ConfigManager.Auth.Twitch.Client.Secret;
        _api.Settings.AccessToken = ConfigManager.Auth.Twitch.Bot.Access;
        _api.Settings.Scopes = ConfigManager.Auth.Twitch.Bot.GetScopes();

        _tokenTimer.Change(30000, 300000);
        await TwitchTokenCheck.CheckTokens(_api, true);

        _channel = (await _api.Helix.Users.GetUsersAsync(logins: new List<string>
            { ConfigManager.Config.Twitch.Channel })).Users[0];
        _moderator =
            (await _api.Helix.Users.GetUsersAsync(logins: new List<string> { ConfigManager.Config.Twitch.Username }))
            .Users[0];
    }

    public void InitializeClient()
    {
        _client.Initialize(
            new ConnectionCredentials(ConfigManager.Config.Twitch.Username, ConfigManager.Auth.Twitch.Bot.Access),
            ConfigManager.Config.Twitch.Channel);

        _client.OnConnected += Client_Connected;
        _client.OnJoinedChannel += Client_JoinedChannel;


        _client.OnMessageSent += Client_MessageSent;
        _client.OnMessageReceived += Client_MessageReceived;

        _client.Connect();
    }

    private void InitializePubSub()
    {
        _pubsub.OnPubSubServiceConnected += PubSub_ServiceConnected;
        _pubsub.OnListenResponse += PubSub_ListenResponse;

        _pubsub.OnStreamUp += PubSub_StreamUp;
        _pubsub.OnStreamDown += PubSub_StreamDown;

        _pubsub.OnBan += PubSub_Ban;
        _pubsub.OnUnban += PubSub_Unban;
        _pubsub.OnTimeout += PubSub_Timeout;
        _pubsub.OnUntimeout += PubSub_Untimeout;

        _pubsub.OnMessageDeleted += PubSub_MessageDeleted;
        _pubsub.OnClear += PubSub_Clear;

        _pubsub.OnSubscribersOnly += PubSub_SubscribersOnly;
        _pubsub.OnSubscribersOnlyOff += PubSub_SubscribersOnlyOff;
        _pubsub.OnEmoteOnly += PubSub_EmoteOnly;
        _pubsub.OnEmoteOnlyOff += PubSub_EmoteOnlyOff;
        _pubsub.OnR9kBeta += PubSub_R9kBeta;
        _pubsub.OnR9kBetaOff += PubSub_R9kBetaOff;

        _pubsub.Connect();
    }
}