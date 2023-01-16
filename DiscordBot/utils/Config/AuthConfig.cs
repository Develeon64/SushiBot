using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dietze.Utils.Config;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct AuthConfig
{
    [JsonProperty] public DiscordAuthConfig Discord { get; set; }

    [JsonProperty] public TwitchAuthConfig Twitch { get; set; }
}