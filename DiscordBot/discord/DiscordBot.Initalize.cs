using Discord.WebSocket;
using Discord;
using Dietze.helper;

namespace Dietze.Discord;

public partial class DiscordBot
{
    private readonly DiscordSocketClient _client;
    private SocketGuild? _guild;

    public DiscordBot()
    {
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,
            GatewayIntents = GatewayIntents.All,
            LogLevel = LogSeverity.Info,
            MaxWaitBetweenGuildAvailablesBeforeReady = ConfigManager.Config.Discord.ReadyWait,
            TotalShards = 1,
            UseInteractionSnowflakeDate = false
        });

        Initialize();
    }

    private async void Initialize()
    {
        _client.Log += ClientLog;
        _client.Ready += Client_Ready;

        _client.UserJoined += ClientUserJoined;
        _client.UserLeft += ClientUserLeft;

        _client.ThreadCreated += ClientThreadCreated;
        _client.ThreadUpdated += ClientThreadUpdated;
        _client.SlashCommandExecuted += ClientSlashCommandExecuted;

        _client.SetStatusAsync(UserStatus.DoNotDisturb).Wait();
        _client.SetGameAsync("Why do Java programmers have to wear glasses? Because they don't C#", "", ActivityType.Watching).Wait();

        await _client.LoginAsync(TokenType.Bot, ConfigManager.Auth.Discord.Token);
        await _client.StartAsync();
    }
}

