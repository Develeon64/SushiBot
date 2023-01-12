using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Develeon64.RoboSushi.Util.Config;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct DiscordConfig
{
    private int readyWait;

    [JsonProperty]
    public ulong Guild { get; set; }

    [JsonProperty]
    public bool? SyncCommands { get; set; }

    [JsonProperty]
    public ChannelConfig? MentalChannel { get; set; }

    [JsonProperty]
    public ChannelConfig? NotifyChannel { get; set; }

    [JsonProperty]
    public CountConfig? CountChannel { get; set; }

    [JsonProperty]
    public CountConfig? FollowerChannel { get; set; }

    [JsonProperty]
    public ChannelConfig? ModChannel { get; set; }

    [JsonProperty]
    public ulong[]? AdminRoles { get; set; }

    [JsonProperty]
    public ulong[]? ModRoles { get; set; }

    [JsonIgnore]
    public int ReadyWait
    {
        get => this.readyWait;
        set => this.readyWait = value * 1000;
    }
}
