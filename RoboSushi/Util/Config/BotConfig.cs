namespace Develeon64.RoboSushi.Util.Config;

public struct BotConfig {
	public ulong Id { get; set; }
	public string Key { get; set; }
	public string Secret { get; set; }
	public string Token { get; set; }
	public string Username { get; set; }
	public int Discriminator { get; set; }
}
