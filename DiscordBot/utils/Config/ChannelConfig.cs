using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dietze.Utils.Config;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct ChannelConfig
{
    [JsonProperty] public ulong Id { get; set; }

    [JsonProperty] public ulong? MessageId { get; set; }

    [JsonProperty] public string? Token { get; set; }
}