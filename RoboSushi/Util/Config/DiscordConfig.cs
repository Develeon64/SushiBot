namespace Develeon64.RoboSushi.Util.Config;

public struct DiscordConfig {
	private int readyWait;

	public BotConfig Bot { get; set; }
	public GuildConfig Guild { get; set; }
	public bool SyncCommands { get; set; }
	public MentalChannelConfig MentalChannel { get; set; }
	public ulong[] AdminRoles { get; set; }

	public int ReadyWait {
		get => this.readyWait;
		set => this.readyWait = value * 1000;
	}
}
