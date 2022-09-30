namespace Develeon64.RoboSushi.Util.Config;

public struct AuthConfig {
	public DiscordAuthConfig Discord { get; set; }
	public TwitchAuthConfig Twitch { get; set; }
}
