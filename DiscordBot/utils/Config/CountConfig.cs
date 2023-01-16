using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dietze.Utils.Config;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct CountConfig
{
    [JsonProperty] public ulong Id { get; set; }

    [JsonProperty] public ulong[]? NoMembers { get; set; }

    [JsonProperty] public string? Prefix { get; set; }

    [JsonProperty] public string? Postfix { get; set; }
}