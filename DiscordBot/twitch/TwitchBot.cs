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

    private List<string?> _channelId;
    private User? _moderator;

    private void Client_JoinedChannel(object? sender, OnJoinedChannelArgs e)
    {
        Console.WriteLine("Joined channel " + e.Channel);
    }
    private void _client_OnLeftChannel(object? sender, OnLeftChannelArgs e)
    {
        Console.WriteLine("Left channel " + e.Channel);

        if (!_client.IsConnected)
            InitializeClient();
    }

    private void Client_Connected(object? sender, OnConnectedArgs e)
    {
        Console.WriteLine("Twitch connected as " + e.BotUsername);
    }

    private void PubSub_ServiceConnected(object? sender, EventArgs e)
    {
        Console.WriteLine("PubSub connected!");

        foreach (var id in _channelId)
        {
            _pubsub.ListenToBitsEventsV2(id);
            _pubsub.ListenToChannelPoints(id);
            _pubsub.ListenToFollows(id);
            _pubsub.ListenToLeaderboards(id);
            _pubsub.ListenToPredictions(id);
            _pubsub.ListenToRaid(id);
            _pubsub.ListenToSubscriptions(id);
            _pubsub.ListenToVideoPlayback(id);
            _pubsub.ListenToWhispers(id);
            _pubsub.SendTopics(ConfigManager.Auth.Twitch.Channel.Access);

            _pubsub.ListenToAutomodQueue(_moderator?.Id, id);
            _pubsub.ListenToChatModeratorActions(_moderator?.Id, id);
            _pubsub.ListenToUserModerationNotifications(_moderator?.Id, id);
            _pubsub.SendTopics(ConfigManager.Auth.Twitch.Bot.Access);
        }
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
            User = e.ChatMessage.Username,
            Message = e.ChatMessage.Message
        };

        ChatMessages[0] = _Message;
        ChatMessages = ChatMessages.OrderBy(x => x.timestamp).ToArray();

        try
        {
            var user = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChatMessage.UserId })).Users[0];
            var userIcon = EncodeImageUrl(user.ProfileImageUrl); 
            if (e.ChatMessage.DisplayName != ConfigManager.Config.Twitch.Username)
            {
                await ClassHelper.DiscordBot?.SendTwitchMessageToDiscord(e.ChatMessage.DisplayName, e.ChatMessage, userIcon)!;
            }
        }
        catch (Exception ex)
        {

        }

        //Console.WriteLine("New Message from " + e.ChatMessage.DisplayName + "\n" + e.ChatMessage.Message);
    }

    public void SendDiscordMessageToTwitch(string username, string message)
    {
        _client.SendMessage(ConfigManager.Config.Twitch.Channel[0], $"[{username}] {message}");
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
                    stream.StartedAt, stream.ViewerCount, $"{stream.Type[..1].ToUpper()}{stream.Type[1..]}",
                    EncodeImageUrl(stream.ThumbnailUrl), gameThumbnail, userIcon)!;
        }

        //await ClassHelper.DiscordBot?.UpdateMemberCount()!;
    }

    private async void PubSub_StreamDown(object? sender, OnStreamDownArgs e)
    {
        Console.WriteLine(e.ChannelId + " is off!");

        var user = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        await ClassHelper.DiscordBot?.SendOfflineNotification(user.DisplayName, DateTime.Now,
            EncodeImageUrl(user.OfflineImageUrl), EncodeImageUrl(user.ProfileImageUrl))!;

        //await ClassHelper.DiscordBot.UpdateMemberCount();
    }

    private async void PubSub_Ban(object? sender, OnBanArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var banner = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.BannedByUserId })).Users[0];
        var bannerName = banner.DisplayName;
        var bannerIcon = EncodeImageUrl(banner.ProfileImageUrl);
        var user = await _api.Helix.Users.GetUsersAsync(new List<string> { e.BannedUserId });

        if(user.Users.Length != 0)
        {
            var userName = user.Users.First().DisplayName;
            var userIcon = EncodeImageUrl(user.Users.First().ProfileImageUrl);
            var userCreated = user.Users.First().CreatedAt.AddHours(2);
            var lastMessage = ChatMessages.ToList().Where(x => x.User == e.BannedUser).Reverse().Take(4).Reverse().ToList();
            var lastMessages = string.Join("\n \u2022 ", lastMessage.Select(x => x.Message).Reverse().Take(3).Reverse().ToArray());
            var followerTime = (await _api.Helix.Users.GetUsersFollowsAsync(fromId: e.BannedUserId, toId: e.ChannelId)).Follows.FirstOrDefault()?.FollowedAt;

            await DiscordBot.SendBanNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon, userCreated, lastMessages, followerTime, e.BanReason);
        }
    }

    private async void PubSub_Unban(object? sender, OnUnbanArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var banner = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.UnbannedByUserId })).Users[0];
        var bannerName = banner.DisplayName;
        var bannerIcon = EncodeImageUrl(banner.ProfileImageUrl);
        var user = await _api.Helix.Users.GetUsersAsync(new List<string> { e.UnbannedUserId });

        if (user.Users.Length != 0)
        {
            var userName = user.Users.First().DisplayName;
            var userIcon = EncodeImageUrl(user.Users.First().ProfileImageUrl);
            var userCreated = user.Users.First().CreatedAt.AddHours(2);

            await DiscordBot.SendUnbanNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon, userCreated);
        }
    }

    private async void PubSub_Timeout(object? sender, OnTimeoutArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var banner = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.TimedoutById })).Users[0];
        var bannerName = banner.DisplayName;
        var bannerIcon = EncodeImageUrl(banner.ProfileImageUrl);
        var user = await _api.Helix.Users.GetUsersAsync(new List<string> { e.TimedoutUserId });

        if (user.Users.Length != 0)
        {
            var userName = user.Users.First().DisplayName;
            var userIcon = EncodeImageUrl(user.Users.First().ProfileImageUrl);
            var userCreated = user.Users.First().CreatedAt.AddHours(2);
            var lastMessage = ChatMessages.ToList().Where(x => x.User == e.TimedoutUser).Reverse().Take(4).Reverse().ToList();
            var lastMessages = string.Join("\n \u2022 ", lastMessage.Select(x => x.Message).Reverse().Take(3).Reverse().ToArray());
            var followerTime = (await _api.Helix.Users.GetUsersFollowsAsync(fromId: e.TimedoutUserId, toId: e.ChannelId)).Follows.FirstOrDefault()?.FollowedAt;

            await DiscordBot.SendTimeoutNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon, userCreated, lastMessages, followerTime, e.TimeoutDuration, e.TimeoutReason);
        }
    }

    private async void PubSub_Untimeout(object? sender, OnUntimeoutArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var banner = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.UntimeoutedByUserId })).Users[0];
        var bannerName = banner.DisplayName;
        var bannerIcon = EncodeImageUrl(banner.ProfileImageUrl);
        var user = await _api.Helix.Users.GetUsersAsync(new List<string> { e.UntimeoutedUserId });

        if (user.Users.Length != 0)
        {
            var userName = user.Users.First().DisplayName;
            var userIcon = EncodeImageUrl(user.Users.First().ProfileImageUrl);
            var userCreated = user.Users.First().CreatedAt.AddHours(2);

            await DiscordBot.SendUntimeoutNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon, userCreated);
        }
    }

    private async void PubSub_MessageDeleted(object? sender, OnMessageDeletedArgs e)
    {
        var channel = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.ChannelId })).Users[0];
        var channelName = channel.DisplayName;
        var channelIcon = EncodeImageUrl(channel.ProfileImageUrl);
        var deleter = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.DeletedByUserId })).Users[0];
        var deleterName = deleter.DisplayName;
        var deleterIcon = EncodeImageUrl(deleter.ProfileImageUrl);
        var user = (await _api.Helix.Users.GetUsersAsync(new List<string> { e.TargetUserId }))?.Users[0];
        if (user != null)
        {
            var userName = user.DisplayName;
            var userIcon = EncodeImageUrl(user.ProfileImageUrl);
            var userCreated = user.CreatedAt.AddHours(2);
            var followerTime = (await _api.Helix.Users.GetUsersFollowsAsync(fromId: e.TargetUserId, toId: e.ChannelId)).Follows.FirstOrDefault()?.FollowedAt;

            await DiscordBot.SendMessageDeletedNotification(channelName, channelIcon, deleterName, deleterIcon, userName, userIcon, userCreated, e.Message, followerTime);
        }
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
        var id = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { ConfigManager.Config.Twitch.Channel[0] });
        return (await _api.Helix.Users.GetUsersFollowsAsync(toId: id.Users[0].Id)).TotalFollows;
    }

    private static string EncodeImageUrl(string url)
    {
        return string.IsNullOrWhiteSpace(url)
            ? url
            : $"{url.Replace("-{width}x{height}", null)}?{DateTimeOffset.Now.ToUnixTimeSeconds()}";
    }
}