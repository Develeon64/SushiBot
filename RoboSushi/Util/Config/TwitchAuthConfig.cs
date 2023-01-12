using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Develeon64.RoboSushi.Util.Config;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct TwitchAuthConfig
{
    [JsonProperty] public TwitchClientAuthConfig Client { get; set; }

    [JsonProperty] public TwitchTokenAuthConfig Channel { get; set; }

    [JsonProperty] public TwitchTokenAuthConfig Bot { get; set; }
}