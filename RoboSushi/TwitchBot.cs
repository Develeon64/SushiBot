using Develeon64.RoboSushi.Util;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using OnEmoteOnlyArgs = TwitchLib.PubSub.Events.OnEmoteOnlyArgs;

namespace Develeon64.RoboSushi;

public partial class TwitchBot
{
    private readonly TwitchAPI _api = new();
    private readonly TwitchClient _client = new();
    private readonly TwitchPubSub _pubsub = new();

    private User? _channel;
    private User? _moderator;

    public TwitchBot()
    {
        _tokenTimer = new Timer(TokenTimer_Tick, null, Timeout.Infinite, 300000);
        Initialize();
    }

    private static void Client_MessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        Console.WriteLine("New Message from " + e.ChatMessage.DisplayName + "\n" + e.ChatMessage.Message);
    }

    public void Client_MessageSent(object? sender, OnMessageSentArgs e)
    {
        Console.WriteLine("New Message from " + e.SentMessage.DisplayName + "\n" + e.SentMessage.Message);
    }

    private async void PubSub_StreamUp(object? sender, OnStreamUpArgs e)
    {
        Console.WriteLine(e.ChannelId + " went live!");
        await Task.Delay(30000);

        var info = await _api.Helix.Streams.GetStreamsAsync(userIds: new List<string> { e.ChannelId });
        if (info is { Streams.Length: >= 1 })
        {
            var stream = info.Streams[0];
            var userIcon = (await _api.Helix.Users.GetUsersAsync(new List<string> { stream.UserId })).Users[0]
                .ProfileImageUrl;
            var gameThumbnail =
                EncodeImageUrl((await _api.Helix.Games.GetGamesAsync(new List<string> { stream.GameId })).Games[0]
                    .BoxArtUrl);
            if (stream.Type.Length >= 1)
                await RoboSushi.DiscordBot?.SendLiveNotification(stream.UserName, stream.GameName, stream.Title,
                    stream.StartedAt, stream.ViewerCount, stream.Language, stream.IsMature,
                    $"{stream.Type[..1].ToUpper()}{stream.Type[1..]}",
                    EncodeImageUrl(stream.ThumbnailUrl), gameThumbnail, userIcon)!;
        }

        await RoboSushi.DiscordBot?.UpdateMemberCount()!;
    }

    private async void PubSub_StreamDown(object? sender, OnStreamDownArgs e)
    {
        Console.WriteLine(e.ChannelId + " is off!");

        var user = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        await RoboSushi.DiscordBot?.SendOffNotification(user.DisplayName, DateTime.Now,
            EncodeImageUrl(user.OfflineImageUrl), EncodeImageUrl(user.ProfileImageUrl))!;

        await RoboSushi.DiscordBot.UpdateMemberCount();
    }

    private async void PubSub_Ban(object? sender, OnBanArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var banner = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.BannedByUserId })).Users[0];
        var bannerName = banner.DisplayName;
        var bannerIcon = EncodeImageUrl(banner.ProfileImageUrl);
        var user = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.BannedUserId })).Users[0];
        var userName = user.DisplayName;
        var userIcon = EncodeImageUrl(user.ProfileImageUrl);
        var userCreated = user.CreatedAt.AddHours(2);

        await DiscordBot.SendBanNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon,
            userCreated, e.BanReason);
    }

    private async void PubSub_Unban(object? sender, OnUnbanArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var banner = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.UnbannedByUserId })).Users[0];
        var bannerName = banner.DisplayName;
        var bannerIcon = EncodeImageUrl(banner.ProfileImageUrl);
        var user = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.UnbannedUserId })).Users[0];
        var userName = user.DisplayName;
        var userIcon = EncodeImageUrl(user.ProfileImageUrl);
        var userCreated = user.CreatedAt.AddHours(2);

        await DiscordBot.SendUnbanNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon,
            userCreated);
    }

    private async void PubSub_Timeout(object? sender, OnTimeoutArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var banner = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.TimedoutById })).Users[0];
        var bannerName = banner.DisplayName;
        var bannerIcon = EncodeImageUrl(banner.ProfileImageUrl);
        var user = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.TimedoutUserId })).Users[0];
        var userName = user.DisplayName;
        var userIcon = EncodeImageUrl(user.ProfileImageUrl);
        var userCreated = user.CreatedAt.AddHours(2);

        await DiscordBot.SendTimeoutNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon,
            userCreated, e.TimeoutDuration, e.TimeoutReason);
    }

    private async void PubSub_Untimeout(object? sender, OnUntimeoutArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var banner = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.UntimeoutedByUserId })).Users[0];
        var bannerName = banner.DisplayName;
        var bannerIcon = EncodeImageUrl(banner.ProfileImageUrl);
        var user = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.UntimeoutedUserId })).Users[0];
        var userName = user.DisplayName;
        var userIcon = EncodeImageUrl(user.ProfileImageUrl);
        var userCreated = user.CreatedAt.AddHours(2);

        await DiscordBot.SendUntimeoutNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon,
            userCreated);
    }

    /*private void PubSub_AutomodCaughtMessage (object? sender, OnAutomodCaughtMessageArgs e) {
		Console.WriteLine($"AutomodCaughtMessage: [{e.AutomodCaughtMessage.Message.Sender.DisplayName} ({e.AutomodCaughtMessage.ResolverLogin})]: {e.AutomodCaughtMessage.Message}\n{e.AutomodCaughtMessage.Status}: {e.AutomodCaughtMessage.ReasonCode} ({e.AutomodCaughtMessage.ContentClassification.Level} - {e.AutomodCaughtMessage.ContentClassification.Category})");
	}

	private void PubSub_AutomodCaughtUserMessage (object? sender, OnAutomodCaughtUserMessage e) {
		Console.WriteLine($"AutomodCaughtUserMessage: [{e.AutomodCaughtMessage.Status}]: ({e.UserId}) {e.AutomodCaughtMessage.MessageId}");
	}*/

    private async void PubSub_MessageDeleted(object? sender, OnMessageDeletedArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var deleter = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.DeletedByUserId })).Users[0];
        var deleterName = deleter.DisplayName;
        var deleterIcon = EncodeImageUrl(deleter.ProfileImageUrl);
        var user = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.TargetUserId })).Users[0];
        var userName = user.DisplayName;
        var userIcon = EncodeImageUrl(user.ProfileImageUrl);
        var userCreated = user.CreatedAt.AddHours(2);

        await DiscordBot.SendMessageDeletedNotification(channelName, channelIcon, deleterName, deleterIcon, userName,
            userIcon, userCreated, e.Message);
    }

    private async void PubSub_Clear(object? sender, OnClearArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var clearer = (await _api.Helix.Users.GetUsersAsync(logins: new List<string> { e.Moderator })).Users[0];
        var clearerName = clearer.DisplayName;
        var clearerIcon = EncodeImageUrl(clearer.ProfileImageUrl);

        await DiscordBot.SendChatClearedNotification(channelName, channelIcon, clearerName, clearerIcon);
    }

    private async void PubSub_SubscribersOnly(object? sender, OnSubscribersOnlyArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var moderator = (await _api.Helix.Users.GetUsersAsync(logins: new List<string> { e.Moderator })).Users[0];
        var modName = moderator.DisplayName;
        var modIcon = EncodeImageUrl(moderator.ProfileImageUrl);

        await DiscordBot.SendSubscriberOnlyNotification(channelName, channelIcon, modName, modIcon, true);
    }

    private async void PubSub_SubscribersOnlyOff(object? sender, OnSubscribersOnlyOffArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var moderator = (await _api.Helix.Users.GetUsersAsync(logins: new List<string> { e.Moderator })).Users[0];
        var modName = moderator.DisplayName;
        var modIcon = EncodeImageUrl(moderator.ProfileImageUrl);

        await DiscordBot.SendSubscriberOnlyNotification(channelName, channelIcon, modName, modIcon, false);
    }

    private async void PubSub_EmoteOnly(object? sender, OnEmoteOnlyArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var moderator = (await _api.Helix.Users.GetUsersAsync(logins: new List<string> { e.Moderator })).Users[0];
        var modName = moderator.DisplayName;
        var modIcon = EncodeImageUrl(moderator.ProfileImageUrl);

        await DiscordBot.SendEmoteOnlyNotification(channelName, channelIcon, modName, modIcon, true);
    }

    private async void PubSub_EmoteOnlyOff(object? sender, OnEmoteOnlyOffArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var moderator = (await _api.Helix.Users.GetUsersAsync(logins: new List<string> { e.Moderator })).Users[0];
        var modName = moderator.DisplayName;
        var modIcon = EncodeImageUrl(moderator.ProfileImageUrl);

        await DiscordBot.SendEmoteOnlyNotification(channelName, channelIcon, modName, modIcon, false);
    }

    private async void PubSub_R9kBeta(object? sender, OnR9kBetaArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var moderator = (await _api.Helix.Users.GetUsersAsync(logins: new List<string> { e.Moderator })).Users[0];
        var modName = moderator.DisplayName;
        var modIcon = EncodeImageUrl(moderator.ProfileImageUrl);

        await DiscordBot.SendR9KBetaNotification(channelName, channelIcon, modName, modIcon, true);
    }

    private async void PubSub_R9kBetaOff(object? sender, OnR9kBetaOffArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var moderator = (await _api.Helix.Users.GetUsersAsync(logins: new List<string> { e.Moderator })).Users[0];
        var modName = moderator.DisplayName;
        var modIcon = EncodeImageUrl(moderator.ProfileImageUrl);

        await DiscordBot.SendR9KBetaNotification(channelName, channelIcon, modName, modIcon, false);
    }

    public async Task<long> GetFollowerCount()
    {
        var id = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { ConfigManager.Config.Twitch.Channel });
        return (await _api.Helix.Users.GetUsersFollowsAsync(toId: id.Users[0].Id)).TotalFollows;
    }

    private async Task CheckTokens(bool force = false)
    {
        var valid = await _api.Auth.ValidateAccessTokenAsync(ConfigManager.Auth.Twitch.Bot.Access);
        if (valid == null || valid.ExpiresIn > 300 || force)
        {
            var tokens = await _api.Auth.RefreshAuthTokenAsync(ConfigManager.Auth.Twitch.Bot.Refresh,
                ConfigManager.Auth.Twitch.Client.Secret, ConfigManager.Auth.Twitch.Client.Id);
            ConfigManager.RefreshTwitchBotTokens(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);
            _api.Settings.AccessToken = tokens.AccessToken;
        }

        valid = await _api.Auth.ValidateAccessTokenAsync(ConfigManager.Auth.Twitch.Channel.Access);
        if (valid == null || valid.ExpiresIn > 300 || force)
        {
            var tokens = await _api.Auth.RefreshAuthTokenAsync(ConfigManager.Auth.Twitch.Channel.Refresh,
                ConfigManager.Auth.Twitch.Client.Secret, ConfigManager.Auth.Twitch.Client.Id);
            ConfigManager.RefreshTwitchChannelTokens(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);
        }

        await RoboSushi.DiscordBot?.UpdateMemberCount()!;
    }

    private static string EncodeImageUrl(string url)
    {
        return string.IsNullOrWhiteSpace(url)
            ? url
            : $"{url.Replace("-{width}x{height}", null)}?{DateTimeOffset.Now.ToUnixTimeSeconds()}";
    }
}