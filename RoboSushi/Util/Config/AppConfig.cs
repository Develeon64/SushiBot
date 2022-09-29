namespace Develeon64.RoboSushi.Util.Config;

public struct AppConfig {
	public DiscordConfig Discord { get; set; }
	public TwitchConfig Twitch { get; set; }
	public LogConfig Log { get; set; }
}
