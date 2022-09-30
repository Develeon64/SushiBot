using TwitchLib.Api.Core.Enums;

namespace Develeon64.RoboSushi.Util.Config;

public struct TwitchAuthConfig {
	public TwitchClientAuthConfig Client { get; set; }
	public TwitchTokenAuthConfig Channel { get; set; }
	public TwitchTokenAuthConfig Bot { get; set; }
}
