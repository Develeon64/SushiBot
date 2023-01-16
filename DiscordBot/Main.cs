using Dietze.Twitch;
using Dietze.Discord;
using Dietze.helper;

namespace Dietze;

public class MainDiscordBot
{
    public static void Main()
    {
        MainAsync().GetAwaiter().GetResult();
    }

    public static async Task MainAsync()
    {
        ConfigManager.Initialize();
        ClassHelper.DiscordBot = new DiscordBot();
        ClassHelper.TwitchBot = new TwitchBot();

        await Task.Delay(-1);
    }
}