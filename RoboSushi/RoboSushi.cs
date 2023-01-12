using Develeon64.RoboSushi.Discord;
using Develeon64.RoboSushi.Util;

namespace Develeon64.RoboSushi;

public class RoboSushi
{
    public static DiscordBot? DiscordBot;
    public static TwitchBot? TwitchBot;

    public static void Main() { MainAsync().GetAwaiter().GetResult(); }

    public static async Task MainAsync()
    {
        ConfigManager.Initialize();

        DiscordBot = new();
        TwitchBot = new();

        await Task.Delay(-1);
    }
}
