using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Develeon64.RoboSushi.Discord;

public class DiscordBot {
	DiscordSocketClient _client;
	SocketGuild _guild;
	System.Timers.Timer _timer = new(100000);

	bool isReady = false;

	public DiscordBot (string token) {
		this._timer.Elapsed += this.Timer_Elapsed;
		this._timer.Start();

		this._client = new(new() {
			AlwaysDownloadUsers = true,
			GatewayIntents = GatewayIntents.All,
			LogLevel = LogSeverity.Info,
			MaxWaitBetweenGuildAvailablesBeforeReady = 3000,
			TotalShards = 1,
		});

		this.Initialize(token);
	}

	private async void Timer_Elapsed (object? sender, System.Timers.ElapsedEventArgs e) {
		if (this.isReady) {
			foreach (SocketThreadChannel thread in this._client.Guilds.ToDictionary((SocketGuild guild) => { return guild.Id; })[1022939762337775616].ThreadChannels) {
				if (thread.IsArchived)
					await thread.DeleteAsync();
			}
		}
	}

	private async void Initialize (string token) {
		this._client.Log += this.Client_Log;
		this._client.Ready += this.Client_Ready;

		this._client.ThreadCreated += this.Client_ThreadCreated;
		this._client.ThreadUpdated += this.Client_ThreadUpdated;
		this._client.SlashCommandExecuted += this.Client_SlashCommandExecuted;

		await this._client.LoginAsync(TokenType.Bot, token);
		await this._client.StartAsync();
	}

	private async Task Client_ThreadUpdated (Cacheable<SocketThreadChannel, ulong> old, SocketThreadChannel thread) {
		if (thread.ParentChannel.Id == 1022960981816643585 && thread.IsArchived)
			await thread.DeleteAsync();
	}

	private async Task Client_SlashCommandExecuted (SocketSlashCommand command) {
		if (command.Channel.GetChannelType() == ChannelType.PublicThread) {
			SocketThreadChannel thread = command.Channel as SocketThreadChannel;
			if (thread?.ParentChannel.Id == 1022960981816643585 && (thread?.Owner.Id == command.User.Id || this.IsModerator(this._guild.GetUser(command.User.Id)))) {
				await thread.DeleteAsync();
				await command.RespondWithModalAsync(new ModalBuilder("Thread wurde gelöscht.", "thread_deletion_modal").Build());
			}
			else {
				await command.RespondAsync("You don't have the permission to close this Thread :(", null, false, true);
			}
		}

	}

	private async Task Client_ThreadCreated (SocketThreadChannel thread) {
		if (thread.ParentChannel.Id == 1022960981816643585) {
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
		this._guild = this._client.Guilds.ToDictionary((SocketGuild guild) => { return guild.Id; })[1022939762337775616];
		await this.Client_Log(new(LogSeverity.Info, "System", "Bot is ready!"));

		SlashCommandBuilder command = new() {
			Name = "close",
			Description = "Close the current Thread",
		};
		await this._guild.CreateApplicationCommandAsync(command.Build());
		this.isReady = true;
	}

	private bool IsModerator (SocketGuildUser user) {
		var roles = this._guild.Roles.ToDictionary((SocketRole role) => { return role.Id; });
		return user.Roles.Contains(roles[1022990009390870588]) || user.Roles.Contains(roles[1022990072477392928]) || user.Roles.Contains(roles[1022990009390870588]);
	}
}
