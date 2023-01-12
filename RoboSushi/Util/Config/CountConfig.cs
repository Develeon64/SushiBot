using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Develeon64.RoboSushi.Util.Config;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct CountConfig
{
    [JsonProperty]
    public ulong Id { get; set; }

    [JsonProperty]
    public string? Prefix { get; set; }

    [JsonProperty]
    public string? Postfix { get; set; }
}
