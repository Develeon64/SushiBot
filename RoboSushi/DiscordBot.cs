using Develeon64.RoboSushi.Util;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Develeon64.RoboSushi.Discord;

public class DiscordBot {
	DiscordSocketClient _client;
	SocketGuild _guild;

	public DiscordBot () {
		this._client = new(new() {
			AlwaysDownloadUsers = true,
			GatewayIntents = GatewayIntents.All,
			LogLevel = LogSeverity.Info,
			MaxWaitBetweenGuildAvailablesBeforeReady = ConfigManager.Config.Discord.ReadyWait,
			TotalShards = 1,
		});

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

	private async Task Client_ThreadUpdated (Cacheable<SocketThreadChannel, ulong> old, SocketThreadChannel thread) {
		if (thread.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel.Id && thread.IsArchived)
			await thread.DeleteAsync();
	}

	private async Task Client_SlashCommandExecuted (SocketSlashCommand command) {
		if (command.Channel.GetChannelType() == ChannelType.PublicThread) {
			SocketThreadChannel thread = command.Channel as SocketThreadChannel;
			if (thread?.ParentChannel.Id == ConfigManager.Config.Discord.MentalChannel.Id && (thread?.Owner.Id == command.User.Id || this.IsModerator(this._guild.GetUser(command.User.Id)))) {
				await thread.DeleteAsync();
				await command.RespondWithModalAsync(new ModalBuilder("Thread wurde gelöscht.", "thread_deletion_modal").Build());
			}
			else {
				await command.RespondAsync("You don't have the permission to close this Thread :(", null, false, true);
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

	private async Task Client_MessageReceived (SocketMessage message) {
		Console.WriteLine(message.Content);
		if (!message.Author.IsBot && message.Channel.GetChannelType() != ChannelType.PublicThread)
			await message.Channel.SendMessageAsync("Ok, habe ich so mit!");
	}

	private Task Client_Log (LogMessage message) {
		Console.WriteLine ($"{DateTime.Now:dd.MM.yyyy HH:mm:ss} | {message.Severity.ToString().PadRight(8).Substring(0, 8)} | {message.Source.PadRight(8).Substring(0, 8)} | {message.Message ?? message.Exception.Message}");
		return Task.CompletedTask;
	}

	private async Task Client_Ready () {
		this._guild = this._client.Guilds.ToDictionary((SocketGuild guild) => { return guild.Id; })[ConfigManager.Config.Discord.Guild.Id];
		await this.Client_Log(new(LogSeverity.Info, "System", "Bot is ready!"));

		SlashCommandBuilder command = new() {
			Name = "close",
			Description = "Close the current Thread",
		};
		await this._guild.CreateApplicationCommandAsync(command.Build());
	}

	private bool IsModerator (SocketGuildUser user) {
		bool isModerator = false;

		foreach (ulong adminRoleId in ConfigManager.Config.Discord.AdminRoles) {
			foreach (SocketRole userRole in user.Roles) {
				if (userRole.Id == adminRoleId) isModerator = true;
			}
		}

		return isModerator;
	}
}
