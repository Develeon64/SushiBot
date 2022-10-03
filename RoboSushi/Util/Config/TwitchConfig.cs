using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Develeon64.RoboSushi.Util.Config;

[JsonObject(
	ItemRequired = Required.DisallowNull,
	MemberSerialization = MemberSerialization.OptIn,
	NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct TwitchConfig {
	[JsonProperty]
	public string Username { get; set; }

	[JsonProperty]
	public string Channel { get; set; }
}
