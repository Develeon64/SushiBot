using Develeon64.RoboSushi.Util;
using Discord;
using Discord.WebSocket;
using Discord.Webhook;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

		this._client.ThreadCreated += this.Client_ThreadCreated;
		this._client.ThreadUpdated += this.Client_ThreadUpdated;
		this._client.SlashCommandExecuted += this.Client_SlashCommandExecuted;

		await this._client.LoginAsync(TokenType.Bot, ConfigManager.Config.Discord.Bot.Token);
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
		this._guild = this._client.Guilds.ToDictionary((SocketGuild guild) => { return guild.Id; })[ConfigManager.Config.Discord.Guild.Id];
		await this.Client_Log(new(LogSeverity.Info, "System", "Bot is ready!"));

		if (ConfigManager.Config.Discord.SyncCommands) {
			List<SlashCommandBuilder> slashCommandBuilders = new();
			slashCommandBuilders.Add(new() {
				Name = "close",
				Description = "Close the current Thread",
			});

			slashCommandBuilders.Add(new() {
				Name = "version",
				Description = "Show Version information of the bot.",
			});

			foreach (SlashCommandBuilder command in slashCommandBuilders)
				await this._guild.CreateApplicationCommandAsync(command.Build());
		}

		this._presenceTimer.Change(30000, 300000);
	}

	private Task Client_Log (LogMessage message) {
		//Console.WriteLine ($"{DateTime.Now:dd.MM.yyyy HH:mm:ss} | {message.Severity.ToString().PadRight(8).Substring(0, 8)} | {message.Source.PadRight(8).Substring(0, 8)} | {message.Message ?? message.Exception.Message}");
		return Task.CompletedTask;
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
					if (thread?.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel.Id && (thread?.Owner.Id == command.User.Id || this.IsModerator(this._guild.GetUser(command.User.Id)))) {
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
		if (thread.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel.Id) {
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
		if (thread.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel.Id && thread.IsArchived)
			await thread.DeleteAsync();
	}

	public async Task SendLiveNotification (string username, string game, string title, DateTime started, int viewerCount, string language, bool mature, string type, string streamUrl, string thumbnailUrl, string iconUrl) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = username, Url = $"https://www.twitch.tv/{username}/about", IconUrl = iconUrl },
			Description = $"**{this.Escape(username)}** is now on Twitch for *{viewerCount} viewers*!",
			ImageUrl = streamUrl,
			ThumbnailUrl = thumbnailUrl,
			Timestamp = started,
			Title = this.Escape(title),
			Url = $"https://www.twitch.tv/{username}",
		};
		embed.WithColorGreen();
		embed.AddField("__**Category**__", game, true);
		embed.AddField("__**Type**__", type, true);

		await new DiscordWebhookClient(ConfigManager.Config.Discord.NotifyChannel.Id, ConfigManager.Config.Discord.NotifyChannel.Token).SendMessageAsync("@everyone", embeds: new List<Embed>() { embed.Build() }, username: this._client.CurrentUser.Username, avatarUrl: this._client.CurrentUser.GetAvatarUrl());
	}

	public async Task SendOffNotification (string username, DateTime ended, string streamUrl, string iconUrl) {
		DiscordEmbedBuilder embed = new() {
			Author = new() { Name = username, Url = $"https://www.twitch.tv/{username}/about", IconUrl = iconUrl },
			Description = $"**{this.Escape(username)}** is now *offline* again.",
			ImageUrl = streamUrl,
			Timestamp = ended,
			Title = "",
			Url = $"https://www.twitch.tv/{username}",
		};
		embed.WithColorPurple();
		//embed.AddField("__**Category**__", game, true);
		//embed.AddField("__**Type**__", type, true);

		await new DiscordWebhookClient(ConfigManager.Config.Discord.NotifyChannel.Id, ConfigManager.Config.Discord.NotifyChannel.Token).SendMessageAsync(embeds: new List<Embed>() { embed.Build() }, username: this._client.CurrentUser.Username, avatarUrl: this._client.CurrentUser.GetAvatarUrl());
	}

	private string Escape (string text) {
		return text.Replace("<", "\\<").Replace("*", "\\*").Replace("_", "\\_").Replace("`", "\\`").Replace(":", "\\:");
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
