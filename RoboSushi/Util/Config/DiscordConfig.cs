namespace Develeon64.RoboSushi.Util.Config;

public struct DiscordConfig {
	private int readyWait;

	public GuildConfig Guild { get; set; }
	public bool SyncCommands { get; set; }
	public ChannelConfig MentalChannel { get; set; }
	public ChannelConfig NotifyChannel { get; set; }
	public ChannelConfig ModChannel { get; set; }
	public ulong[] AdminRoles { get; set; }

	public int ReadyWait {
		get => this.readyWait;
		set => this.readyWait = value * 1000;
	}
}
