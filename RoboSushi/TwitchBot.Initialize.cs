using Develeon64.RoboSushi.Util;
using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

namespace Develeon64.RoboSushi;

public partial class TwitchBot
{
    private readonly Timer _tokenTimer;

    private async void Initialize()
    {
        await this.InitializeApi();
        this.InitializeClient();
        this.InitializePubSub();

        this._client.Connect();
        this._pubsub.Connect();
    }

    private async Task InitializeApi()
    {
        this._api.Settings.ClientId = ConfigManager.Auth.Twitch.Client.Id;
        this._api.Settings.Secret = ConfigManager.Auth.Twitch.Client.Secret;
        this._api.Settings.AccessToken = ConfigManager.Auth.Twitch.Bot.Access;
        this._api.Settings.Scopes = ConfigManager.Auth.Twitch.Bot.GetScopes();

        this._tokenTimer.Change(30000, 300000);
        await this.CheckTokens(true);

        this._channel = (await this._api.Helix.Users.GetUsersAsync(logins: new() { ConfigManager.Config.Twitch.Channel })).Users[0];
        this._moderator = (await this._api.Helix.Users.GetUsersAsync(logins: new() { ConfigManager.Config.Twitch.Username })).Users[0];
    }

    private void InitializeClient()
    {
        this._client.Initialize(new(ConfigManager.Config.Twitch.Username, ConfigManager.Auth.Twitch.Bot.Access), ConfigManager.Config.Twitch.Channel);

        this._client.OnConnected += this.Client_Connected;
        this._client.OnJoinedChannel += this.Client_JoinedChannel;

        this._client.OnMessageSent += this.Client_MessageSent;
        this._client.OnMessageReceived += this.Client_MessageReceived;
    }

    private void InitializePubSub()
    {
        this._pubsub.OnPubSubServiceConnected += this.PubSub_ServiceConnected;
        this._pubsub.OnListenResponse += this.PubSub_ListenResponse;

        this._pubsub.OnStreamUp += this.PubSub_StreamUp;
        this._pubsub.OnStreamDown += this.PubSub_StreamDown;

        this._pubsub.OnBan += this.PubSub_Ban;
        this._pubsub.OnUnban += this.PubSub_Unban;
        this._pubsub.OnTimeout += this.PubSub_Timeout;
        this._pubsub.OnUntimeout += this.PubSub_Untimeout;

        /*this._pubsub.OnAutomodCaughtMessage += this.PubSub_AutomodCaughtMessage;
		this._pubsub.OnAutomodCaughtUserMessage += this.PubSub_AutomodCaughtUserMessage;*/

        this._pubsub.OnMessageDeleted += this.PubSub_MessageDeleted;
        this._pubsub.OnClear += this.PubSub_Clear;

        this._pubsub.OnSubscribersOnly += this.PubSub_SubscribersOnly;
        this._pubsub.OnSubscribersOnlyOff += this.PubSub_SubscribersOnlyOff;
        this._pubsub.OnEmoteOnly += this.PubSub_EmoteOnly;
        this._pubsub.OnEmoteOnlyOff += this.PubSub_EmoteOnlyOff;
        this._pubsub.OnR9kBeta += this.PubSub_R9kBeta;
        this._pubsub.OnR9kBetaOff += this.PubSub_R9kBetaOff;
    }

    private async void TokenTimer_Tick(object? stateInfo)
    {
        await this.CheckTokens();
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

        this._pubsub.ListenToBitsEventsV2(this._channel?.Id);
        this._pubsub.ListenToChannelPoints(this._channel?.Id);
        this._pubsub.ListenToFollows(this._channel?.Id);
        this._pubsub.ListenToLeaderboards(this._channel?.Id);
        this._pubsub.ListenToPredictions(this._channel?.Id);
        this._pubsub.ListenToRaid(this._channel?.Id);
        this._pubsub.ListenToSubscriptions(this._channel?.Id);
        this._pubsub.ListenToVideoPlayback(this._channel?.Id);
        this._pubsub.ListenToWhispers(this._channel?.Id);
        this._pubsub.SendTopics(ConfigManager.Auth.Twitch.Channel.Access);

        this._pubsub.ListenToAutomodQueue(this._moderator?.Id, this._channel?.Id);
        this._pubsub.ListenToChatModeratorActions(this._moderator?.Id, this._channel?.Id);
        this._pubsub.ListenToUserModerationNotifications(this._moderator?.Id, this._channel?.Id);
        this._pubsub.SendTopics(ConfigManager.Auth.Twitch.Bot.Access);

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
