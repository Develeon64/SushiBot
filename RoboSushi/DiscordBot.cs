using Develeon64.RoboSushi.Util;
using Discord;
using Discord.WebSocket;
using Discord.Webhook;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VerboseTimespan;

namespace Develeon64.RoboSushi.Discord;

public class DiscordBot {
	private DiscordSocketClient _client;
	private SocketGuild _guild;

	private Timer _presenceTimer;
	private byte _presenceState = 0;


	public DiscordBot () {
		this._client = new(new() {
			AlwaysDownloadUsers = true,
			GatewayIntents = GatewayIntents.All,
			LogLevel = LogSeverity.Info,
			MaxWaitBetweenGuildAvailablesBeforeReady = ConfigManager.Config.Discord.ReadyWait,
			TotalShards = 1,
		});

		this._presenceTimer = new(this.PresenceTimer_Tick, null, Timeout.Infinite, 300000);
		this.Initialize();
	}

	private async void Initialize () {
		this._client.Log += this.Client_Log;
		this._client.Ready += this.Client_Ready;

		this._client.UserJoined += this.Client_UserJoined;
		this._client.UserLeft += this.Client_UserLeft;

		this._client.ThreadCreated += this.Client_ThreadCreated;
		this._client.ThreadUpdated += this.Client_ThreadUpdated;
		this._client.SlashCommandExecuted += this.Client_SlashCommandExecuted;

		await this._client.LoginAsync(TokenType.Bot, ConfigManager.Auth.Discord.Token);
		await this._client.StartAsync();
	}

	private async void PresenceTimer_Tick (object? stateInfo) {
		switch (this._presenceState) {
			case 1:
				await this._client.SetStatusAsync(UserStatus.Online);
				await this._client.SetGameAsync("SushiAims", "https://www.twitch.tv/sushiaims", ActivityType.Streaming);
				break;
			case 2:
				await this._client.SetStatusAsync(UserStatus.AFK);
				await this._client.SetGameAsync("Develeon64", "", ActivityType.Listening);
				break;
			case 3:
				await this._client.SetStatusAsync(UserStatus.DoNotDisturb);
				await this._client.SetGameAsync("WIP!", null, ActivityType.Watching);
				break;
			default:
				await this._client.SetStatusAsync(UserStatus.Online);
				await this._client.SetGameAsync(VersionManager.FullVersion, null, ActivityType.Playing);
				break;
		}
		this._presenceState = (byte)(this._presenceState < 3 ? this._presenceState + 1 : 0);
	}

	private async Task Client_Ready () {
		this._guild = this._client.Guilds.ToDictionary((SocketGuild guild) => { return guild.Id; })[ConfigManager.Config.Discord.Guild];
		await this.Client_Log(new(LogSeverity.Info, "System", "Bot is ready!"));

		if (ConfigManager.Config.Discord.SyncCommands == true) {
			List<SlashCommandBuilder> slashCommandBuilders = new() {
				new() {
					Name = "close",
					Description = "Close the current Thread",
				},
				new() {
					Name = "version",
					Description = "Show Version information of the bot.",
				}
			};

			foreach (SlashCommandBuilder command in slashCommandBuilders)
				await this._guild.CreateApplicationCommandAsync(command.Build());
		}

		this._presenceTimer.Change(30000, 300000);

		await this.UpdateMemberCount("Member-Count checkup at boot");
	}

	private Task Client_Log (LogMessage message) {
		Console.WriteLine ($"{DateTime.Now:dd.MM.yyyy HH:mm:ss} | {message.Severity.ToString().PadRight(8).Substring(0, 8)} | {message.Source.PadRight(8).Substring(0, 8)} | {message.Message ?? message.Exception.Message}");
		return Task.CompletedTask;
	}

	private async Task Client_UserJoined (SocketGuildUser member) {
		await this.UpdateMemberCount($"New Member-Count: Member joined: {member.Username}#{member.Discriminator}");
	}

	private async Task Client_UserLeft (SocketGuild guild, SocketUser user) {
		await this.UpdateMemberCount($"New Member-Count: Member left: {user.Username}#{user.Discriminator}");
	}

	private async Task Client_MessageReceived (SocketMessage message) {
		Console.WriteLine(message.Content);
		if (!message.Author.IsBot && message.Channel.GetChannelType() != ChannelType.PublicThread)
			await message.Channel.SendMessageAsync("Ok, habe ich so mit!");
	}

	private async Task Client_SlashCommandExecuted (SocketSlashCommand command) {
		switch (command.CommandName) {
			case "close": {
				if (command.Channel.GetChannelType() == ChannelType.PublicThread) {
					SocketThreadChannel thread = command.Channel as SocketThreadChannel;
					if (thread?.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel?.Id && (thread?.Owner.Id == command.User.Id || this.IsModerator(this._guild.GetUser(command.User.Id)))) {
						await thread.DeleteAsync();
						await command.RespondWithModalAsync(new ModalBuilder("Thread wurde gelöscht.", "thread_deletion_modal").Build());
					}
					else {
						await command.RespondAsync("You don't have the permission to close this Thread!", ephemeral: true);
					}
				}
				else {
					await command.RespondAsync("There is nothing to close here.", ephemeral: true);
				}
				break;
			}
			case "version": {
				HttpClient http = new();
				DiscordEmbedBuilder builder = new(this._client.CurrentUser) {
					Description = (await http.GetStringAsync("https://raw.githubusercontent.com/Develeon64/SushiBot/main/README.md")).Split("# Robo-Sushi")[1].Split('#')[0].Trim(),
					ImageUrl = (await http.GetStringAsync($"https://github.com/Develeon64/SushiBot/commit/{(await http.GetStringAsync($"https://github.com/Develeon64/SushiBot/releases/tag/{VersionManager.GitVersion}")).Split("/Develeon64/SushiBot/commit/")[1].Split('"')[0]}")).Split("og:image")[1].Split('"')[2],
					ThumbnailUrl = this._client.CurrentUser.GetDefaultAvatarUrl(),
					Title = $"__Version: {VersionManager.FullVersion}__",
				};
				builder.AddField("__Changelog__", await http.GetStringAsync("https://github.com/Develeon64/SushiBot/blob/main/Var/Changelog.txt"), false);
				builder.AddField("__Author__", "I'm being developed by\n<@298215920709664768> (Develeon#1010)\nGitHub: [Develeon64](https://github.com/Develeon64)", true);
				builder.AddField("__Code__", "My code can be found on GitHub under\n[Develeon64/SushiBot](https://github.com/Develeon64/SushiBot)", true);

				await command.RespondAsync(embed: builder.Build());
				break;
			}
			default: {
				await command.RespondAsync("Unrecognized Command", ephemeral: true);
				break;
			}
		}
	}

	private async Task Client_ThreadCreated (SocketThreadChannel thread) {
		if (thread.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel?.Id) {
			await thread.JoinAsync();

			JObject newThread = new() {
				{ "id", thread.Id },
				{ "date", DateTime.Now.ToString() },
			};

			JArray threads = JObject.Parse(File.ReadAllText("threads.json"))["threads"] as JArray ?? new();
			threads.Add(newThread);
			File.WriteAllText("threads.json", threads.ToString(Formatting.Indented));
		}
	}

	private async Task Client_ThreadUpdated (Cacheable<SocketThreadChannel, ulong> old, SocketThreadChannel thread) {
		if (thread.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel?.Id && thread.IsArchived)
			await thread.DeleteAsync();
	}

	public async Task UpdateMemberCount (string reason = "") {
		if (this._guild != null) {
			long memberCount = 0;
			List<string> counted = new();

			await foreach (var members in this._guild.GetUsersAsync()) {
				foreach (var member in members) {
					if (!member.IsBot && member.Id != 372108947303301124 && member.Id != 344136468068958218 && !counted.Contains(member.Username.ToLower()) && !counted.Contains(member.Nickname?.ToLower() ?? String.Empty)) {
						memberCount += 1;
						counted.Add(member.Username.ToLower());
						if (!String.IsNullOrWhiteSpace(member.Nickname))
							counted.Add(member.Nickname.ToLower());
					}
				}
			}
			string memberString = String.IsNullOrWhiteSpace(ConfigManager.Config.Discord.CountChannel?.Prefix) ? String.Empty : $"{ConfigManager.Config.Discord.CountChannel?.Prefix}: ";
			memberString += memberCount;
			if (!String.IsNullOrWhiteSpace(ConfigManager.Config.Discord.CountChannel?.Postfix)) memberString += $" {ConfigManager.Config.Discord.CountChannel?.Postfix}";
			await this._guild.GetChannel(ConfigManager.Config.Discord.CountChannel?.Id ?? 0).ModifyAsync((props) => { props.Name = memberString; }, new() { AuditLogReason = reason });

			long followerCount = await RoboSushi.twitchBot.GetFollowerCount();
			string followerString = String.IsNullOrWhiteSpace(ConfigManager.Config.Discord.FollowerChannel?.Prefix) ? String.Empty : $"{ConfigManager.Config.Discord.FollowerChannel?.Prefix}: ";
			followerString += followerCount;
			if (!String.IsNullOrWhiteSpace(ConfigManager.Config.Discord.FollowerChannel?.Postfix)) followerString += $" {ConfigManager.Config.Discord.FollowerChannel?.Postfix}";
			await this._guild.GetChannel(ConfigManager.Config.Discord.FollowerChannel?.Id ?? 0).ModifyAsync((props) => { props.Name = followerString; }, new() { AuditLogReason = reason });
		}
	}

	public async Task SendLiveNotification (string username, string game, string title, DateTime started, int viewerCount, string language, bool mature, string type, string streamUrl, string thumbnailUrl, string iconUrl) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = username, Url = $"https://www.twitch.tv/{username}/about", IconUrl = iconUrl },
			Description = $"**{this.EscapeMessage(username)}** is now on Twitch!",
			ImageUrl = streamUrl,
			ThumbnailUrl = thumbnailUrl,
			Timestamp = started,
			Title = this.EscapeMessage(title),
			Url = $"https://www.twitch.tv/{username}",
		};
		embed.WithColorGreen();
		embed.AddField("__**Category**__", game, true);
		embed.AddField("__**Type**__", type, true);
		embed.AddBlankField(false);
		embed.AddField("__**Instagram**__", "[@sushiiaims](https://www.instagram.com/sushiiaims/)", true);
		embed.AddField("__**TikTok**__", "[@sushiaims](https://www.tiktok.com/@sushiaims/)", true);

		string everyone = $"@everyone look at https://www.twitch.tv/{username.ToLower()}";
		if (ConfigManager.Config.Discord.NotifyChannel?.Token != null) {
			await new DiscordWebhookClient(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0, ConfigManager.Config.Discord.NotifyChannel?.Token).SendMessageAsync($"@everyone look at https://www.twitch.tv/{username}", embeds: new List<Embed>() { embed.Build() }, username: this._client.CurrentUser.Username, avatarUrl: this._client.CurrentUser.GetAvatarUrl());
		}
		else if (ConfigManager.Config.Discord.NotifyChannel?.MessageId != null) {
			await this._guild.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0).ModifyMessageAsync(ConfigManager.Config.Discord.NotifyChannel?.MessageId ?? 0, (props) => {
				props.Content = everyone;
				props.Embed = embed.Build();
			});
			await (await this._guild.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0).SendMessageAsync(everyone)).DeleteAsync();
		}
		else {
			var channel = this._guild.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0);
			if (ConfigManager.Db.Discord.NotifyMessageId is not null and not 0) await channel.DeleteMessageAsync(ConfigManager.Db.Discord.NotifyMessageId ?? 0);
			ConfigManager.Db.Discord.NotifyMessageId = (await this._guild.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0).SendMessageAsync(everyone, false, embed.Build())).Id;
		}
	}

	public async Task SendOffNotification (string username, DateTime ended, string streamUrl, string iconUrl) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = username, Url = $"https://www.twitch.tv/{username}/about", IconUrl = iconUrl },
			Description = $"**[{this.EscapeMessage(username)}](https://www.twitch.tv/{username})** is *offline* right now.\nBut you can watch the latest [VODs](https://www.twitch.tv/{username}/videos).",
			ImageUrl = streamUrl,
			Timestamp = ended,
			Title = "Stream is down",
			Url = $"https://www.twitch.tv/{username}/schedule",
		};
		embed.WithColorPurple();
		//embed.AddField("__**Category**__", game, true);
		//embed.AddField("__**Type**__", type, true);
		//embed.AddField("__**Socials**__", DiscordEmbedBuilder.BlankChar, false);
		embed.AddField("__**Instagram**__", "[@sushiiaims](https://www.instagram.com/sushiiaims/)", true);
		embed.AddField("__**TikTok**__", "[@sushiaims](https://www.tiktok.com/@sushiaims/)", true);

		if (ConfigManager.Config.Discord.NotifyChannel?.Token != null) {
			await new DiscordWebhookClient(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0, ConfigManager.Config.Discord.NotifyChannel?.Token).SendMessageAsync(embeds: new List<Embed>() { embed.Build() }, username: this._client.CurrentUser.Username, avatarUrl: this._client.CurrentUser.GetAvatarUrl());
		}
		else if (ConfigManager.Db.Discord.NotifyMessageId is not null and not 0) {
			await this._guild.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0).ModifyMessageAsync(ConfigManager.Db.Discord.NotifyMessageId ?? 0, (props) => {
				props.Content = "";
				props.Embed = embed.Build();
			});
		}
		else if (ConfigManager.Config.Discord.NotifyChannel?.MessageId != null) {
			await this._guild.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0).ModifyMessageAsync(ConfigManager.Config.Discord.NotifyChannel?.MessageId ?? 0, (props) => {
				props.Content = "";
				props.Embed = embed.Build();
			});
		}
		else {
			await this._guild.GetTextChannel(ConfigManager.Config.Discord.NotifyChannel?.Id ?? 0).SendMessageAsync(embed: embed.Build());
		}
	}

	public async Task SendBanNotification (string channelName, string channelIcon, string bannerName, string bannerIcon, string userName, string userIcon, DateTime userCreated, string? reason = null) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = bannerName, IconUrl = bannerIcon },
			ThumbnailUrl = userIcon,
			Title = $"{userName} was **BANNED**!",
			Url = $"https://www.twitch.tv/popout/{channelName}/viewercard/{userName}",
		};
		if (reason != null)
			embed.WithDescription(this.EscapeMessage(reason));

		embed.AddField("__Created__", $"{userCreated:dd.MM.yyyy HH:mm:ss}\n{this.FormatTimeSpan(userCreated)} ago");
		embed.WithColorPink();

		await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0, ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync($"**BAN**\n<@&{String.Join("> <@&", ConfigManager.Config.Discord.ModRoles)}>", embeds: new List<Embed>() { embed.Build() }, username: channelName, avatarUrl: channelIcon);
	}

	public async Task SendUnbanNotification (string channelName, string channelIcon, string bannerName, string bannerIcon, string userName, string userIcon, DateTime userCreated) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = bannerName, IconUrl = bannerIcon },
			ThumbnailUrl = userIcon,
			Title = $"{userName} was **UNBANNED**!",
			Url = $"https://www.twitch.tv/popout/{channelName}/viewercard/{userName}",
		};

		embed.AddField("__Created__", $"{userCreated:dd.MM.yyyy HH:mm:ss}\n{this.FormatTimeSpan(userCreated)} ago");
		embed.WithColorLime();

		await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0, ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed>() { embed.Build() }, username: channelName, avatarUrl: channelIcon);	}

	public async Task SendTimeoutNotification (string channelName, string channelIcon, string bannerName, string bannerIcon, string userName, string userIcon, DateTime userCreated, TimeSpan duration, string? reason = null) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = bannerName, IconUrl = bannerIcon },
			ThumbnailUrl = userIcon,
			Title = $"{userName} was **TIMEDOUT**!",
			Url = $"https://www.twitch.tv/popout/{channelName}/viewercard/{userName}",
		};
		if (reason != null)
			embed.WithDescription(this.EscapeMessage(reason));
		
		embed.AddField("__Created__", $"{userCreated:dd.MM.yyyy HH:mm:ss}\n{this.FormatTimeSpan(userCreated)} ago");
		embed.AddField("__Duration__", $"{duration.TotalSeconds} Seconds\n{this.ConvertTimeoutDuration(duration)}\n{this.ConvertTimeoutTime(duration)}");
		embed.WithColorYellow();

		await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0, ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync($"**BAN**\n<@&{String.Join("> <@&", ConfigManager.Config.Discord.ModRoles)}>", embeds: new List<Embed>() { embed.Build() }, username: channelName, avatarUrl: channelIcon);
	}

	public async Task SendUntimeoutNotification (string channelName, string channelIcon, string bannerName, string bannerIcon, string userName, string userIcon, DateTime userCreated) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = bannerName, IconUrl = bannerIcon },
			ThumbnailUrl = userIcon,
			Title = $"{userName} was **UNTIMEOUTED**!",
			Url = $"https://www.twitch.tv/popout/{channelName}/viewercard/{userName}",
		};

		embed.AddField("__Created__", $"{userCreated:dd.MM.yyyy HH:mm:ss}\n{this.FormatTimeSpan(userCreated)} ago");
		embed.WithColorLime();

		await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0, ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed>() { embed.Build() }, username: channelName, avatarUrl: channelIcon);
	}

	public async Task SendMessageDeletedNotification (string channelName, string channelIcon, string deleterName, string deleterIcon, string userName, string userIcon, DateTime userCreated, string message) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = deleterName, IconUrl = deleterIcon },
			Description = this.EscapeMessage(message),
			ThumbnailUrl = userIcon,
			Title = $"A message of {userName} was **DELETED**!",
			Url = $"https://www.twitch.tv/popout/{channelName}/viewercard/{userName}",
		};

		embed.AddField("__Created__", $"{userCreated:dd.MM.yyyy HH:mm:ss}\n{this.FormatTimeSpan(userCreated)} ago");
		embed.WithColorPink();

		await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0, ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed>() { embed.Build() }, username: channelName, avatarUrl: channelIcon);
	}

	public async Task SendChatClearedNotification (string channelName, string channelIcon, string clearerName, string clearerIcon) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = clearerName, IconUrl = clearerIcon },
			Title = $"The chat was **CLEARED**!",
			Url = $"https://www.twitch.tv/moderator/{channelName}",
		};
		embed.WithColorPink();

		await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0, ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed>() { embed.Build() }, username: channelName, avatarUrl: channelIcon);
	}

	public async Task SendSubscriberOnlyNotification (string channelName, string channelIcon, string modName, string modIcon, bool on) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = modName, IconUrl = modIcon },
			Title = $"**Subscriber only** mode is now **{(on ? "ON" : "OFF")}**!",
			Url = $"https://www.twitch.tv/moderator/{channelName}",
		};
		if (on) embed.WithColorPink();
		else embed.WithColorLime();

		await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0, ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed>() { embed.Build() }, username: channelName, avatarUrl: channelIcon);
	}

	public async Task SendEmoteOnlyNotification (string channelName, string channelIcon, string modName, string modIcon, bool on) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = modName, IconUrl = modIcon },
			Title = $"**Emote only** mode is now **{(on ? "ON" : "OFF")}**!",
			Url = $"https://www.twitch.tv/moderator/{channelName}",
		};
		if (on) embed.WithColorYellow();
		else embed.WithColorLime();

		await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0, ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed>() { embed.Build() }, username: channelName, avatarUrl: channelIcon);
	}

	public async Task SendR9kBetaNotification (string channelName, string channelIcon, string modName, string modIcon, bool on) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = modName, IconUrl = modIcon },
			Title = $"**R9k** mode is now **{(on ? "ON" : "OFF")}**!",
			Url = $"https://www.twitch.tv/moderator/{channelName}",
		};
		if (on) embed.WithColorPurple();
		else embed.WithColorLime();

		await new DiscordWebhookClient(ConfigManager.Config.Discord.ModChannel?.Id ?? 0, ConfigManager.Config.Discord.ModChannel?.Token).SendMessageAsync(embeds: new List<Embed>() { embed.Build() }, username: channelName, avatarUrl: channelIcon);
	}

	private string EscapeMessage (string text) {
		return text.Replace("<", "\\<").Replace("*", "\\*").Replace("_", "\\_").Replace("`", "\\`").Replace(":", "\\:");
	}

	private string FormatTimeSpan (DateTime dateTime) {
		return DateTime.Now.Subtract(dateTime).ToVerboseString().Replace(" And ", " ");
	}

	private string ConvertTimeoutDuration (TimeSpan duration) {
		string value = String.Empty;

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

	private string ConvertTimeoutTime (TimeSpan duration) {
		DateTime date = DateTime.Now.Add(duration);
		string value = date.DayOfWeek.ToString() + ", ";
		value += date.Day.ToString().PadLeft(2, '0') + ".";
		value += date.Month.ToString().PadLeft(2, '0') + ".";
		value += date.Year.ToString().PadLeft(4, '0') + " ";
		value += date.Hour.ToString().PadLeft(2, '0') + ":";
		value += date.Minute.ToString().PadLeft(2, '0') + ":";
		value += date.Second.ToString().PadLeft(2, '0');
		return value.Trim();
	}

	private string ConvertTimeoutDate (int day) {
		switch (day) {
			case 1: return "Monday";
			case 2: return "Tuesday";
			case 3: return "Wednesday";
			case 4: return "Thursday";
			case 5: return "Friday";
			case 6: return "Saturday";
			default: return "Sunday";
		}
	}

	private bool IsModerator (SocketGuildUser user) {
		if (user.Id == this._guild.Owner.Id)
			return true;

		bool isModerator = false;

		foreach (ulong adminRoleId in ConfigManager.Config.Discord.AdminRoles) {
			foreach (SocketRole userRole in user.Roles) {
				if (userRole.Id == adminRoleId) isModerator = true;
			}
		}

		return isModerator;
	}
}
