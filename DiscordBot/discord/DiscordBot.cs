using System.Globalization;
using VerboseTimespan;
using Discord.Webhook;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Discord;

using Dietze.Utils.Db;
using Dietze.helper;
using TwitchLib.Client.Models;

namespace Dietze.Discord;

public partial class DiscordBot
{
    private async Task Client_Ready()
    {
        _guild = _client.Guilds.ToDictionary(guild => guild.Id)[ConfigManager.Config.Discord.Guild];
        await ClientLog(new LogMessage(LogSeverity.Info, "System", "Bot is ready!"));

        if (ConfigManager.Config.Discord.SyncCommands == true)
        {
            List<SlashCommandBuilder> slashCommandBuilders = new()
            {
                new SlashCommandBuilder
                {
                    Name = "close",
                    Description = "Close the current Thread"
                },
                new SlashCommandBuilder
                {
                    Name = "ping",
                    Description = "Pings the bot to get the latency"
                },
                new SlashCommandBuilder
                {
                    Name = "version",
                    Description = "Show Version information of the bot."
                }
            };

            foreach (var command in slashCommandBuilders)
                await _guild.CreateApplicationCommandAsync(command.Build());
        }

        await UpdateMemberCount("Member-Count checkup at boot");
    }

    private static Task ClientLog(LogMessage message)
    {
        Console.WriteLine(
            $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} | {message.Severity.ToString().PadRight(8)[..8]} | {message.Source.PadRight(8)[..8]} | {message.Message ?? message.Exception.Message}");
        return Task.CompletedTask;
    }

    private async Task ClientUserJoined(SocketGuildUser member)
    {
        await UpdateMemberCount($"New Member-Count: Member joined: {member.Username}#{member.Discriminator}");
    }

    private async Task ClientUserLeft(SocketGuild guild, SocketUser user)
    {
        await UpdateMemberCount($"New Member-Count: Member left: {user.Username}#{user.Discriminator}");
    }

    private async Task ClientMessageRecieved(SocketMessage message)
    {
        if(message.Channel.Id == ConfigManager.Config.Discord.ChatChannel?.ChatId && message.Source != MessageSource.Webhook && message.Author.Id != ConfigManager.Auth.Discord.Id)
        {
            ClassHelper.TwitchBot?.SendDiscordMessageToTwitch(message.Author.Username, message.CleanContent);
        }
    }

    public async Task SendTwitchMessageToDiscord(string Username, ChatMessage message, string userIcon)
    {
       await new DiscordWebhookClient(ConfigManager.Config.Discord.ChatChannel?.Id ?? 0, ConfigManager.Config.Discord.ChatChannel?.Token).SendMessageAsync(message.Message, username: message.DisplayName, avatarUrl: userIcon);
    }

    private async Task ClientSlashCommandExecuted(SocketSlashCommand command)
    {
        switch (command.CommandName)
        {
            case "close":
                {
                    if (command.Channel.GetChannelType() == ChannelType.PublicThread)
                    {
                        var thread = command.Channel as SocketThreadChannel;
                        if (thread?.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel?.Id &&
                            (thread?.Owner.Id == command.User.Id || IsModerator(_guild?.GetUser(command.User.Id))))
                        {
                            await thread?.DeleteAsync()!;
                            await command.RespondWithModalAsync(
                                new ModalBuilder("Thread wurde gelöscht.", "thread_deletion_modal").Build());
                        }
                        else
                        {
                            await command.RespondAsync("You don't have the permission to close this Thread!",
                                ephemeral: true);
                        }
                    }
                    else
                    {
                        await command.RespondAsync("There is nothing to close here.", ephemeral: true);
                    }

                    break;
                }
            case "ping":
                {
                    DiscordEmbedBuilder builder = new(_client.CurrentUser)
                    {
                        Description = "Ping Pong",
                        Title = $"__Ping latency: {_client.Latency} ms__"
                    };

                    await command.RespondAsync(embed: builder.Build());

                    break;
                }
            case "version":
                {
                    _ = new HttpClient();
                    DiscordEmbedBuilder builder = new(_client.CurrentUser)
                    {
                        Description = "Der Bot ist von mir an dich",
                        ImageUrl = "",
                        ThumbnailUrl = "",
                        Title = $"__Version: {VersionManager.FullVersion}__"
                    };

                    await command.RespondAsync(embed: builder.Build());
                    break;
                }
            default:
                {
                    await command.RespondAsync("Unrecognized Command", ephemeral: true);
                    break;
                }
        }
    }

    public static async Task ClientThreadCreated(SocketThreadChannel thread)
    {
        if (thread.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel?.Id)
        {
            await thread.JoinAsync();

            JObject newThread = new()
            {
                { "id", thread.Id },
                { "date", DateTime.Now.ToString(CultureInfo.InvariantCulture) }
            };

            var threads = JObject.Parse(await File.ReadAllTextAsync("threads.json"))["threads"] as JArray ?? new JArray();
            threads.Add(newThread);
            await File.WriteAllTextAsync("threads.json", threads.ToString(Formatting.Indented));
        }
    }

    private static async Task ClientThreadUpdated(Cacheable<SocketThreadChannel, ulong> old, SocketThreadChannel thread)
    {
        if (thread.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel?.Id && thread.IsArchived)
            await thread.DeleteAsync();
    }

    public async Task UpdateMemberCount(string reason = "")
    {
        {
            long memberCount = 0;
            List<string> counted = new();
            var NoMembers = ConfigManager.Config.Discord.CountChannel?.NoMembers ?? new ulong[] { 0 };

            if (_guild?.GetUsersAsync() != null)
            {
                await foreach (var members in _guild?.GetUsersAsync())
                    foreach (var member in members)
                        if (!member.IsBot && !NoMembers.Contains(member.Id) &&
                            !counted.Contains(member.Username.ToLower()) &&
                            !counted.Contains(member.Nickname?.ToLower() ?? string.Empty))
                        {
                            memberCount += 1;
                            counted.Add(member.Username.ToLower());
                            if (!string.IsNullOrWhiteSpace(member.Nickname))
                                counted.Add(member.Nickname.ToLower());
                        }

                var memberString = string.IsNullOrWhiteSpace(ConfigManager.Config.Discord.CountChannel?.Prefix)
                    ? string.Empty
                    : $"{ConfigManager.Config.Discord.CountChannel?.Prefix}: ";
                memberString += memberCount;
                if (!string.IsNullOrWhiteSpace(ConfigManager.Config.Discord.CountChannel?.Postfix))
                    memberString += $" {ConfigManager.Config.Discord.CountChannel?.Postfix}";
                await _guild.GetChannel(ConfigManager.Config.Discord.CountChannel?.Id ?? 0)
                    .ModifyAsync(props => { props.Name = memberString; }, new RequestOptions { AuditLogReason = reason });

                var followerCount = await ClassHelper.TwitchBot?.GetFollowerCount()!;
                var followerString = string.IsNullOrWhiteSpace(ConfigManager.Config.Discord.FollowerChannel?.Prefix)
                    ? string.Empty
                    : $"{ConfigManager.Config.Discord.FollowerChannel?.Prefix}: ";
                followerString += followerCount;
                if (!string.IsNullOrWhiteSpace(ConfigManager.Config.Discord.FollowerChannel?.Postfix))
                    followerString += $" {ConfigManager.Config.Discord.FollowerChannel?.Postfix}";
                await _guild.GetChannel(ConfigManager.Config.Discord.FollowerChannel?.Id ?? 0)
                    .ModifyAsync(props => { props.Name = followerString; }, new RequestOptions { AuditLogReason = reason });

            }
        }
    }


    public async Task SendLiveNotification(string username, string game, string title, DateTime started,
        int viewerCount, string type, string streamUrl, string thumbnailUrl, string iconUrl)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder
            { 
                Name = username, Url = $"https://www.twitch.tv/{username}/about", IconUrl = iconUrl },
                Description = $"**{EscapeMessage(username)}** is now on Twitch!",
                ImageUrl = streamUrl,
                ThumbnailUrl = thumbnailUrl,
                Timestamp = started,
                Title = EscapeMessage(title),
                Url = $"https://www.twitch.tv/{username}"
        };
        embed.WithColorGreen();
        embed.AddField("__**Category**__", game, true);
        embed.AddField("__**Type**__", type, true);
        embed.AddField("__**ViewerCount**__", viewerCount, true);
        embed.AddBlankField();
        embed.AddField("__**Instagram**__", "[@erynation.xyz](https://www.instagram.com/erynation.xyz/)", true);
        embed.AddField("__**Twitter**__", "[@erynation](https://www.twitter.com/@erynation/)", true);

        var everyone = $"@everyone look at https://www.twitch.tv/{username.ToLower()}";
        if (ConfigManager.Config.Discord.NotifyChannel?.Token != null)
        {
            await new DiscordWebhookClient(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0,
                ConfigManager.Config.Discord.NotifyChannel?.Token).SendMessageAsync(
                $"@everyone look at https://www.twitch.tv/{username}", embeds: new List<Embed> { embed.Build() },
                username: _client.CurrentUser.Username, avatarUrl: _client.CurrentUser.GetAvatarUrl());
        }
        else if (ConfigManager.Config.Discord.NotifyChannel?.MessageId != null)
        {
            await _guild?.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0).ModifyMessageAsync(
                ConfigManager.Config.Discord.NotifyChannel?.MessageId ?? 0, props =>
                {
                    props.Content = everyone;
                    props.Embed = embed.Build();
                })!;
            await (await _guild.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0)
                .SendMessageAsync(everyone)).DeleteAsync();
        }
        else
        {
            await SendDiscordStreamNotification(embed, everyone);
        }
    }

    public async Task SendOfflineNotification(string username, DateTime ended, string streamUrl, string iconUrl)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder
            { 
                Name = username, Url = $"https://www.twitch.tv/{username}/about", IconUrl = iconUrl 
            },
            Description =
                $"**[{EscapeMessage(username)}](https://www.twitch.tv/{username})** is *offline* right now.\nBut you can watch the latest [VODs](https://www.twitch.tv/{username}/videos).",
            ImageUrl = streamUrl,
            Timestamp = ended,
            Title = "Stream is down",
            Url = $"https://www.twitch.tv/{username}/schedule"
        };

        embed.WithColorGrey();
        embed.AddField("__**Instagram**__", "[@erynation.xyz](https://www.instagram.com/erynation.xyz/)", true);
        embed.AddField("__**Twitter**__", "[@erynation](https://www.twitter.com/@erynation/)", true);

        if (ConfigManager.Config.Discord.NotifyChannel?.Token != null)
            await new DiscordWebhookClient(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0,
                ConfigManager.Config.Discord.NotifyChannel?.Token).SendMessageAsync(
                embeds: new List<Embed> { embed.Build() }, username: _client.CurrentUser.Username,
                avatarUrl: _client.CurrentUser.GetAvatarUrl());
        else if (ConfigManager.Config.Discord.NotifyChannel?.MessageId != null)
            await _guild?.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0).ModifyMessageAsync(
                ConfigManager.Config.Discord.NotifyChannel?.MessageId ?? 0, props =>
                {
                    props.Content = "";
                    props.Embed = embed.Build();
                })!;
        else
            await SendDiscordStreamNotification(embed);
    }

    public async Task SendDiscordStreamNotification(DiscordEmbedBuilder embed, string everyone = "")
    {
        var channel = _guild?.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0);
        ulong messageId = ConfigManager.Db.NotifyMessageId;
        if (messageId is not 0) await channel?.DeleteMessageAsync(messageId)!;
        var message = await channel?.SendMessageAsync(text: everyone, embed: embed.Build())!;
        ConfigManager.Db.NotifyMessageId = message.Id;
        AppDb.WriteFile();
    }

    public static async Task SendBanNotification(string channelName, string channelIcon, string bannerName,
        string bannerIcon, string userName, string userIcon, DateTime userCreated, string lastMessage, DateTime? followerTime, string? reason = null)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder { Name = bannerName, IconUrl = bannerIcon },
            ThumbnailUrl = userIcon,
            Title = $"{userName} was **BANNED**!",
            Url = $"https://www.twitch.tv/popout/{channelName}/viewercard/{userName}"
        };

        if (reason != null)
        {
            reason = "Reason: " + reason;
            embed.WithDescription(EscapeMessage(reason));
        }

        embed.AddField("__Created__", $"User created: {userCreated:dd.MM.yyyy HH:mm:ss}");

        if (followerTime != null)
            embed.AddField("__Followed since__", $"{followerTime}");

        if (lastMessage != null)
            embed.AddField("__Last message__", $"{lastMessage}");

        embed.WithColorPink();

        if (ConfigManager.Config.Discord.ModRoles != null)
            await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
                ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(
                embeds: new List<Embed> { embed.Build() }, username: channelName, avatarUrl: channelIcon);
    }

    public static async Task SendUnbanNotification(string channelName, string channelIcon, string bannerName,
        string bannerIcon, string userName, string userIcon, DateTime userCreated)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder { Name = bannerName, IconUrl = bannerIcon },
            ThumbnailUrl = userIcon,
            Title = $"{userName} was **UNBANNED**!",
            Url = $"https://www.twitch.tv/popout/{channelName}/viewercard/{userName}"
        };

        embed.AddField("__Created__", $"User created: {userCreated:dd.MM.yyyy HH:mm:ss}");
        embed.WithColorLime();

        await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
            ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed> { embed.Build() },
            username: channelName, avatarUrl: channelIcon);
    }

    public static async Task SendTimeoutNotification(string channelName, string channelIcon, string bannerName,
        string bannerIcon, string userName, string userIcon, DateTime userCreated, string lastMessage, DateTime? followerTime, TimeSpan duration,
        string? reason = null)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder { Name = bannerName, IconUrl = bannerIcon },
            ThumbnailUrl = userIcon,
            Title = $"{userName} was **TIMEDOUT**!",
            Url = $"https://www.twitch.tv/popout/{channelName}/viewercard/{userName}"
        };
        if (reason != null)
            embed.WithDescription(EscapeMessage(reason));

        embed.AddField("__Created__", 
            $"User created: {userCreated:dd.MM.yyyy HH:mm:ss}");

        embed.AddField("__Duration__",
            $"{duration.TotalSeconds} Seconds\n{ConvertTimeoutDuration(duration)}\n{ConvertTimeoutTime(duration)}");

        if (followerTime != null)
            embed.AddField("__Followed since__", $"{followerTime}");

        if (lastMessage != null)
            embed.AddField("__Last message__", $"{lastMessage}");

        embed.WithColorYellow();

        if (ConfigManager.Config.Discord.ModRoles != null)
            await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
                ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(
                embeds: new List<Embed> { embed.Build() }, username: channelName, avatarUrl: channelIcon);
    }

    public static async Task SendUntimeoutNotification(string channelName, string channelIcon, string bannerName,
        string bannerIcon, string userName, string userIcon, DateTime userCreated)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder { Name = bannerName, IconUrl = bannerIcon },
            ThumbnailUrl = userIcon,
            Title = $"{userName} was **UNTIMEOUTED**!",
            Url = $"https://www.twitch.tv/popout/{channelName}/viewercard/{userName}"
        };

        embed.AddField("__Created__", 
            $"User created: {userCreated:dd.MM.yyyy HH:mm:ss}");

        embed.WithColorLime();

        await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
            ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed> { embed.Build() },
            username: channelName, avatarUrl: channelIcon);
    }

    public static async Task SendMessageDeletedNotification(string channelName, string channelIcon, string deleterName,
        string deleterIcon, string userName, string userIcon, DateTime userCreated, string message)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder { Name = deleterName, IconUrl = deleterIcon },
            Description = EscapeMessage(message),
            ThumbnailUrl = userIcon,
            Title = $"A message of {userName} was **DELETED**!",
            Url = $"https://www.twitch.tv/popout/{channelName}/viewercard/{userName}"
        };

        embed.AddField("__Created__", 
            $"User created: {userCreated:dd.MM.yyyy HH:mm:ss}");

        embed.WithColorPink();

        await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
            ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed> { embed.Build() },
            username: channelName, avatarUrl: channelIcon);
    }

    public static async Task SendChatClearedNotification(string channelName, string channelIcon, string clearerName,
        string clearerIcon)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder { Name = clearerName, IconUrl = clearerIcon },
            Title = "The chat was **CLEARED**!",
            Url = $"https://www.twitch.tv/moderator/{channelName}"
        };
        embed.WithColorPink();

        await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
            ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed> { embed.Build() },
            username: channelName, avatarUrl: channelIcon);
    }

    public static async Task SendSubscriberOnlyNotification(string channelName, string channelIcon, string modName,
        string modIcon, bool on)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder { Name = modName, IconUrl = modIcon },
            Title = $"**Subscriber only** mode is now **{(on ? "ON" : "OFF")}**!",
            Url = $"https://www.twitch.tv/moderator/{channelName}"
        };
        if (on) embed.WithColorLime();
        else embed.WithColorGrey();

        await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
            ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed> { embed.Build() },
            username: channelName, avatarUrl: channelIcon);
    }

    public static async Task SendEmoteOnlyNotification(string channelName, string channelIcon, string modName,
        string modIcon, bool on)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder { Name = modName, IconUrl = modIcon },
            Title = $"**Emote only** mode is now **{(on ? "ON" : "OFF")}**!",
            Url = $"https://www.twitch.tv/moderator/{channelName}"
        };
        if (on) embed.WithColorLime();
        else embed.WithColorGrey();

        await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
            ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed> { embed.Build() },
            username: channelName, avatarUrl: channelIcon);
    }

    public static async Task SendR9KBetaNotification(string channelName, string channelIcon, string modName,
        string modIcon, bool on)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder { Name = modName, IconUrl = modIcon },
            Title = $"**R9k** mode is now **{(on ? "ON" : "OFF")}**!",
            Url = $"https://www.twitch.tv/moderator/{channelName}"
        };
        if (on) embed.WithColorPurple();
        else embed.WithColorLime();

        await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
            ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed> { embed.Build() },
            username: channelName, avatarUrl: channelIcon);
    }

    private static string EscapeMessage(string text)
    {
        return text.Replace("<", "\\<").Replace("*", "\\*").Replace("_", "\\_").Replace("`", "\\`").Replace(":", "\\:");
    }

    private static string ConvertTimeoutDuration(TimeSpan duration)
    {
        var value = string.Empty;

        if (duration.Days >= 1)
            value += duration.Days + " Days ";
        if (duration.Hours >= 1)
            value += duration.Hours + " Hours ";
        if (duration.Minutes >= 1)
            value += duration.Minutes + " Minutes ";
        if (duration.Seconds >= 1)
            value += duration.Seconds + " Seconds";

        return value.Trim();
    }

    private static string ConvertTimeoutTime(TimeSpan duration)
    {
        var date = DateTime.Now.Add(duration);
        var value = date.DayOfWeek + ", ";
        value += date.Day.ToString().PadLeft(2, '0') + ".";
        value += date.Month.ToString().PadLeft(2, '0') + ".";
        value += date.Year.ToString().PadLeft(4, '0') + " ";
        value += date.Hour.ToString().PadLeft(2, '0') + ":";
        value += date.Minute.ToString().PadLeft(2, '0') + ":";
        value += date.Second.ToString().PadLeft(2, '0');
        return value.Trim();
    }

    private bool IsModerator(SocketGuildUser? user)
    {
        if (_guild != null && user != null && user.Id == _guild.Owner.Id)
            return true;

        var isModerator = false;

        if (ConfigManager.Config.Discord.AdminRoles == null) return isModerator;
        foreach (var adminRoleId in ConfigManager.Config.Discord.AdminRoles)
            if (user?.Roles != null)
                foreach (var userRole in user.Roles!)
                    if (userRole.Id == adminRoleId)
                        isModerator = true;

        return isModerator;
    }
}
