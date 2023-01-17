using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using OnEmoteOnlyArgs = TwitchLib.PubSub.Events.OnEmoteOnlyArgs;

using Dietze.Discord;
using Dietze.helper;
using Newtonsoft.Json.Linq;
using Dietze.Utils.Config;
using TwitchLib.Client.Models;
using System.Threading;

namespace Dietze.Twitch;

public partial class TwitchBot
{
    private readonly TwitchAPI _api = new();
    private readonly TwitchClient _client = new();
    private readonly TwitchPubSub _pubsub = new();

    private User? _channel;
    private User? _moderator;

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
    }

    private void PubSub_ListenResponse(object? sender, OnListenResponseArgs e)
    {
        Console.WriteLine($"Listen-Response: {e.Topic} ({e.Successful}): {e.Response.Error}");
    }

    private async void Client_MessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        TwitchMessages _Message = new()
        {
            timestamp = DateTime.Now,
            User = e.ChatMessage.DisplayName,
            Message = e.ChatMessage.Message
        };

        var x = ChatMessages.OrderByDescending(x => x.timestamp).ToArray();
        x[^1] = _Message;
        ChatMessages = x;

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
                await ClassHelper.DiscordBot?.SendLiveNotification(stream.UserName, stream.GameName, stream.Title,
                    stream.StartedAt, stream.ViewerCount, stream.Language, stream.IsMature,
                    $"{stream.Type[..1].ToUpper()}{stream.Type[1..]}",
                    EncodeImageUrl(stream.ThumbnailUrl), gameThumbnail, userIcon)!;
        }

        await ClassHelper.DiscordBot?.UpdateMemberCount()!;
    }

    private async void PubSub_StreamDown(object? sender, OnStreamDownArgs e)
    {
        Console.WriteLine(e.ChannelId + " is off!");

        var user = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        await ClassHelper.DiscordBot?.SendOfflineNotification(user.DisplayName, DateTime.Now,
            EncodeImageUrl(user.OfflineImageUrl), EncodeImageUrl(user.ProfileImageUrl))!;

        await ClassHelper.DiscordBot.UpdateMemberCount();
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
        var lastMessage = ChatMessages.ToList().Where(x => x.User == e.BannedUser).LastOrDefault().Message;

        var followerTime = (await _api.Helix.Users.GetUsersFollowsAsync(fromId: e.BannedUserId, toId: e.ChannelId)).Follows.FirstOrDefault().FollowedAt;

        await DiscordBot.SendBanNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon,
            userCreated, lastMessage, followerTime, e.BanReason);
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
        var lastMessage = ChatMessages.ToList().Where(x => x.User == e.TimedoutUser).LastOrDefault().Message;
        var followerTime = (await _api.Helix.Users.GetUsersFollowsAsync(fromId: e.TimedoutUserId, toId: e.ChannelId)).Follows.FirstOrDefault().FollowedAt;

        await DiscordBot.SendTimeoutNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon,
            userCreated, lastMessage, followerTime, e.TimeoutDuration, e.TimeoutReason);
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

    private static string EncodeImageUrl(string url)
    {
        return string.IsNullOrWhiteSpace(url)
            ? url
            : $"{url.Replace("-{width}x{height}", null)}?{DateTimeOffset.Now.ToUnixTimeSeconds()}";
    }
}