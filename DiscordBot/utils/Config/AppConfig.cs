using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dietze.Utils.Config;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptOut,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct AppConfig
{
    [JsonProperty] public DiscordConfig Discord { get; set; }

    [JsonProperty] public TwitchConfig Twitch { get; set; }

    [JsonProperty] public LogConfig Log { get; set; }
}