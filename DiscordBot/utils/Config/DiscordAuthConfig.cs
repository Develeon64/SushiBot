using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dietze.Utils.Config;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct DiscordAuthConfig
{
    [JsonProperty] public ulong Id { get; set; }

    [JsonProperty] public string Key { get; set; }

    [JsonProperty] public string Secret { get; set; }

    [JsonProperty] public string Token { get; set; }

    [JsonProperty] public string Username { get; set; }

    [JsonProperty] public int Discriminator { get; set; }
}