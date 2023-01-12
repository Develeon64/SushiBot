using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Develeon64.RoboSushi.Util.Config;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct TwitchConfig
{
    [JsonProperty]
    public string Username { get; set; }

    [JsonProperty]
    public string Channel { get; set; }
}
