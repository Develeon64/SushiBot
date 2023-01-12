using Develeon64.RoboSushi.Util;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.PubSub.Events;

namespace Develeon64.RoboSushi;

public partial class TwitchBot
{
    private readonly Timer _tokenTimer;

    private async void Initialize()
    {
        await InitializeApi();
        InitializeClient();
        InitializePubSub();

        _client.Connect();
        _pubsub.Connect();
    }

    private async Task InitializeApi()
    {
        _api.Settings.ClientId = ConfigManager.Auth.Twitch.Client.Id;
        _api.Settings.Secret = ConfigManager.Auth.Twitch.Client.Secret;
        _api.Settings.AccessToken = ConfigManager.Auth.Twitch.Bot.Access;
        _api.Settings.Scopes = ConfigManager.Auth.Twitch.Bot.GetScopes();

        _tokenTimer.Change(30000, 300000);
        await CheckTokens(true);

        _channel = (await _api.Helix.Users.GetUsersAsync(logins: new List<string>
            { ConfigManager.Config.Twitch.Channel })).Users[0];
        _moderator =
            (await _api.Helix.Users.GetUsersAsync(logins: new List<string> { ConfigManager.Config.Twitch.Username }))
            .Users[0];
    }

    private void InitializeClient()
    {
        _client.Initialize(
            new ConnectionCredentials(ConfigManager.Config.Twitch.Username, ConfigManager.Auth.Twitch.Bot.Access),
            ConfigManager.Config.Twitch.Channel);

        _client.OnConnected += Client_Connected;
        _client.OnJoinedChannel += Client_JoinedChannel;

        _client.OnMessageSent += Client_MessageSent;
        _client.OnMessageReceived += Client_MessageReceived;
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

        /*this._pubsub.OnAutomodCaughtMessage += this.PubSub_AutomodCaughtMessage;
		this._pubsub.OnAutomodCaughtUserMessage += this.PubSub_AutomodCaughtUserMessage;*/

        _pubsub.OnMessageDeleted += PubSub_MessageDeleted;
        _pubsub.OnClear += PubSub_Clear;

        _pubsub.OnSubscribersOnly += PubSub_SubscribersOnly;
        _pubsub.OnSubscribersOnlyOff += PubSub_SubscribersOnlyOff;
        _pubsub.OnEmoteOnly += PubSub_EmoteOnly;
        _pubsub.OnEmoteOnlyOff += PubSub_EmoteOnlyOff;
        _pubsub.OnR9kBeta += PubSub_R9kBeta;
        _pubsub.OnR9kBetaOff += PubSub_R9kBetaOff;
    }

    private async void TokenTimer_Tick(object? stateInfo)
    {
        await CheckTokens();
    }

    private void Client_JoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Console.WriteLine("Joined channel " + e.Channel);
    }

    private void Client_Connected(object? sender, OnConnectedArgs e)
    {
        Console.WriteLine("Twitch connected as " + e.BotUsername);
    }

    private void PubSub_ServiceConnected(object? sender, EventArgs e)
    {
        Console.WriteLine("PubSub connected!");
        ////this._pubsub.ListenToBitsEvents(this._channel?.Id);
        ////this._pubsub.ListenToChannelExtensionBroadcast(this._channel?.Id, this._extension?.Id);
        ////this._pubsub.ListenToRewards(this._channel?.Id);

        _pubsub.ListenToBitsEventsV2(_channel?.Id);
        _pubsub.ListenToChannelPoints(_channel?.Id);
        _pubsub.ListenToFollows(_channel?.Id);
        _pubsub.ListenToLeaderboards(_channel?.Id);
        _pubsub.ListenToPredictions(_channel?.Id);
        _pubsub.ListenToRaid(_channel?.Id);
        _pubsub.ListenToSubscriptions(_channel?.Id);
        _pubsub.ListenToVideoPlayback(_channel?.Id);
        _pubsub.ListenToWhispers(_channel?.Id);
        _pubsub.SendTopics(ConfigManager.Auth.Twitch.Channel.Access);

        _pubsub.ListenToAutomodQueue(_moderator?.Id, _channel?.Id);
        _pubsub.ListenToChatModeratorActions(_moderator?.Id, _channel?.Id);
        _pubsub.ListenToUserModerationNotifications(_moderator?.Id, _channel?.Id);
        _pubsub.SendTopics(ConfigManager.Auth.Twitch.Bot.Access);

        //Thread.Sleep(30000);
        //this.PubSub_StreamUp(null, new() { ChannelId = this._channel?.Id });
        //Thread.Sleep(300000);
        //this.PubSub_StreamDown(null, new() { ChannelId = this._channel?.Id });
    }

    private void PubSub_ListenResponse(object? sender, OnListenResponseArgs e)
    {
        //Console.WriteLine($"Listen-Response: {e.Topic} ({e.Successful}): {e.Response.Error}");
    }
}