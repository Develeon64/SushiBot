using System.Globalization;
using Develeon64.RoboSushi.Util;
using Develeon64.RoboSushi.Util.Db;
using Discord;
using Discord.Webhook;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VerboseTimespan;

namespace Develeon64.RoboSushi;

public class DiscordBot
{
    private readonly DiscordSocketClient _client;

    private readonly Timer _presenceTimer;
    private SocketGuild? _guild;
    private byte _presenceState;


    public DiscordBot()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            GatewayIntents = GatewayIntents.All,
            LogLevel = LogSeverity.Info,
            MaxWaitBetweenGuildAvailablesBeforeReady = ConfigManager.Config.Discord.ReadyWait,
            TotalShards = 1
        });

        _presenceTimer = new Timer(PresenceTimer_Tick, null, Timeout.Infinite, 300000);
        Initialize();
    }

    private async void Initialize()
    {
        _client.Log += Client_Log;
        _client.Ready += Client_Ready;

        _client.UserJoined += Client_UserJoined;
        _client.UserLeft += Client_UserLeft;

        _client.ThreadCreated += Client_ThreadCreated;
        _client.ThreadUpdated += Client_ThreadUpdated;
        _client.SlashCommandExecuted += Client_SlashCommandExecuted;

        await _client.LoginAsync(TokenType.Bot, ConfigManager.Auth.Discord.Token);
        await _client.StartAsync();
    }

    private async void PresenceTimer_Tick(object? stateInfo)
    {
        switch (_presenceState)
        {
            case 1:
                await _client.SetStatusAsync(UserStatus.Online);
                await _client.SetGameAsync("SushiAims", "https://www.twitch.tv/sushiaims", ActivityType.Streaming);
                break;
            case 2:
                await _client.SetStatusAsync(UserStatus.AFK);
                await _client.SetGameAsync("Develeon64", "", ActivityType.Listening);
                break;
            case 3:
                await _client.SetStatusAsync(UserStatus.DoNotDisturb);
                await _client.SetGameAsync("WIP!", null, ActivityType.Watching);
                break;
            default:
                await _client.SetStatusAsync(UserStatus.Online);
                await _client.SetGameAsync(" with your mother");
                break;
        }

        _presenceState = (byte)(_presenceState < 3 ? _presenceState + 1 : 0);
    }

    private async Task Client_Ready()
    {
        _guild = _client.Guilds.ToDictionary(guild => guild.Id)[ConfigManager.Config.Discord.Guild];
        await Client_Log(new LogMessage(LogSeverity.Info, "System", "Bot is ready!"));

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
                    Name = "version",
                    Description = "Show Version information of the bot."
                }
            };

            foreach (var command in slashCommandBuilders)
                await _guild.CreateApplicationCommandAsync(command.Build());
        }

        _presenceTimer.Change(30000, 300000);

        await UpdateMemberCount("Member-Count checkup at boot");
    }

    private static Task Client_Log(LogMessage message)
    {
        Console.WriteLine(
            $"{DateTime.Now:dd.MM.yyyy HH:mm:ss} | {message.Severity.ToString().PadRight(8)[..8]} | {message.Source.PadRight(8)[..8]} | {message.Message ?? message.Exception.Message}");
        return Task.CompletedTask;
    }

    private async Task Client_UserJoined(SocketGuildUser member)
    {
        await UpdateMemberCount($"New Member-Count: Member joined: {member.Username}#{member.Discriminator}");
    }

    private async Task Client_UserLeft(SocketGuild guild, SocketUser user)
    {
        await UpdateMemberCount($"New Member-Count: Member left: {user.Username}#{user.Discriminator}");
    }

    private async Task Client_SlashCommandExecuted(SocketSlashCommand command)
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
            case "version":
            {
                HttpClient http = new();
                DiscordEmbedBuilder builder = new(_client.CurrentUser)
                {
                    Description =
                        (await http.GetStringAsync(
                            "https://raw.githubusercontent.com/Develeon64/SushiBot/main/README.md"))
                        .Split("# Robo-Sushi")[1].Split('#')[0].Trim(),
                    ImageUrl = (await http.GetStringAsync(
                            $"https://github.com/Develeon64/SushiBot/commit/{(await http.GetStringAsync($"https://github.com/Develeon64/SushiBot/releases/tag/{VersionManager.GitVersion}")).Split("/Develeon64/SushiBot/commit/")[1].Split('"')[0]}"))
                        .Split("og:image")[1].Split('"')[2],
                    ThumbnailUrl = _client.CurrentUser.GetDefaultAvatarUrl(),
                    Title = $"__Version: {VersionManager.FullVersion}__"
                };
                builder.AddField("__Changelog__",
                    await http.GetStringAsync("https://github.com/Develeon64/SushiBot/blob/main/Var/Changelog.txt"));
                builder.AddField("__Author__",
                    "I'm being developed by\n<@298215920709664768> (Develeon#1010)\nGitHub: [Develeon64](https://github.com/Develeon64)",
                    true);
                builder.AddField("__Code__",
                    "My code can be found on GitHub under\n[Develeon64/SushiBot](https://github.com/Develeon64/SushiBot)",
                    true);

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

    public async Task Client_ThreadCreated(SocketThreadChannel thread)
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

    private static async Task Client_ThreadUpdated(Cacheable<SocketThreadChannel, ulong> old, SocketThreadChannel thread)
    {
        if (thread.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel?.Id && thread.IsArchived)
            await thread.DeleteAsync();
    }

    public async Task UpdateMemberCount(string reason = "")
    {
        {
            long memberCount = 0;
            List<string> counted = new();

            await foreach (var members in _guild?.GetUsersAsync()!)
            foreach (var member in members)
                if (!member.IsBot && member.Id != 372108947303301124 && member.Id != 344136468068958218 &&
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

            var followerCount = await RoboSushi.TwitchBot?.GetFollowerCount()!;
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

    public async Task SendLiveNotification(string username, string game, string title, DateTime started,
        int viewerCount, string language, bool mature, string type, string streamUrl, string thumbnailUrl,
        string iconUrl)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder
                { Name = username, Url = $"https://www.twitch.tv/{username}/about", IconUrl = iconUrl },
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
        embed.AddField("__**Instagram**__", "[@sushiiaims](https://www.instagram.com/sushiiaims/)", true);
        embed.AddField("__**TikTok**__", "[@sushiaims](https://www.tiktok.com/@sushiaims/)", true);

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

    public async Task SendOffNotification(string username, DateTime ended, string streamUrl, string iconUrl)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder
                { Name = username, Url = $"https://www.twitch.tv/{username}/about", IconUrl = iconUrl },
            Description =
                $"**[{EscapeMessage(username)}](https://www.twitch.tv/{username})** is *offline* right now.\nBut you can watch the latest [VODs](https://www.twitch.tv/{username}/videos).",
            ImageUrl = streamUrl,
            Timestamp = ended,
            Title = "Stream is down",
            Url = $"https://www.twitch.tv/{username}/schedule"
        };
        embed.WithColorPurple();
        //embed.AddField("__**Category**__", game, true);
        //embed.AddField("__**Type**__", type, true);
        //embed.AddField("__**Socials**__", DiscordEmbedBuilder.BlankChar, false);
        embed.AddField("__**Instagram**__", "[@sushiiaims](https://www.instagram.com/sushiiaims/)", true);
        embed.AddField("__**TikTok**__", "[@sushiaims](https://www.tiktok.com/@sushiaims/)", true);

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

    public async Task SendDiscordStreamNotification(DiscordEmbedBuilder embed, string? everyone = null)
    {
        var channel = _guild?.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0);
        var messageId = ConfigManager.Db.NotifyMessageId;
        if (messageId is not 0) await channel?.DeleteMessageAsync(messageId)!;
        var message = await channel?.SendMessageAsync(everyone, embed: embed.Build())!;
        ConfigManager.Db.NotifyMessageId = message.Id;
        AppDb.WriteFile();
    }

    public static async Task SendBanNotification(string channelName, string channelIcon, string bannerName,
        string bannerIcon, string userName, string userIcon, DateTime userCreated, string? reason = null)
    {
        DiscordEmbedBuilder embed = new()
        {
            Author = new EmbedAuthorBuilder { Name = bannerName, IconUrl = bannerIcon },
            ThumbnailUrl = userIcon,
            Title = $"{userName} was **BANNED**!",
            Url = $"https://www.twitch.tv/popout/{channelName}/viewercard/{userName}"
        };
        if (reason != null)
            embed.WithDescription(EscapeMessage(reason));

        embed.AddField("__Created__", $"{userCreated:dd.MM.yyyy HH:mm:ss}\n{FormatTimeSpan(userCreated)} ago");
        embed.WithColorPink();

        if (ConfigManager.Config.Discord.ModRoles != null)
            await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
                ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(
                $"**BAN**\n<@&{string.Join("> <@&", ConfigManager.Config.Discord.ModRoles)}>",
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

        embed.AddField("__Created__", $"{userCreated:dd.MM.yyyy HH:mm:ss}\n{FormatTimeSpan(userCreated)} ago");
        embed.WithColorLime();

        await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
            ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed> { embed.Build() },
            username: channelName, avatarUrl: channelIcon);
    }

    public static async Task SendTimeoutNotification(string channelName, string channelIcon, string bannerName,
        string bannerIcon, string userName, string userIcon, DateTime userCreated, TimeSpan duration,
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

        embed.AddField("__Created__", $"{userCreated:dd.MM.yyyy HH:mm:ss}\n{FormatTimeSpan(userCreated)} ago");
        embed.AddField("__Duration__",
            $"{duration.TotalSeconds} Seconds\n{ConvertTimeoutDuration(duration)}\n{ConvertTimeoutTime(duration)}");
        embed.WithColorYellow();

        if (ConfigManager.Config.Discord.ModRoles != null)
            await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0,
                ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(
                $"**BAN**\n<@&{string.Join("> <@&", ConfigManager.Config.Discord.ModRoles)}>",
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

        embed.AddField("__Created__", $"{userCreated:dd.MM.yyyy HH:mm:ss}\n{FormatTimeSpan(userCreated)} ago");
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

        embed.AddField("__Created__", $"{userCreated:dd.MM.yyyy HH:mm:ss}\n{FormatTimeSpan(userCreated)} ago");
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
        if (on) embed.WithColorPink();
        else embed.WithColorLime();

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
        if (on) embed.WithColorYellow();
        else embed.WithColorLime();

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

    private static string FormatTimeSpan(DateTime dateTime)
    {
        return DateTime.Now.Subtract(dateTime).ToVerboseString().Replace(" And ", " ");
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