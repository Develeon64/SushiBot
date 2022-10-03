using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Develeon64.RoboSushi.Util.Config;

[JsonObject(
	ItemRequired = Required.DisallowNull,
	MemberSerialization = MemberSerialization.OptIn,
	NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct TwitchAuthConfig {
	[JsonProperty]
	public TwitchClientAuthConfig Client { get; set; }

	[JsonProperty]
	public TwitchTokenAuthConfig Channel { get; set; }

	[JsonProperty]
	public TwitchTokenAuthConfig Bot { get; set; }
}
