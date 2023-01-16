using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dietze.Utils.Config;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct TwitchMessages
{
    [JsonProperty] public DateTime timestamp { get; set; }

    [JsonProperty] public string User { get; set; }
    [JsonProperty] public string Message { get; set; }
}