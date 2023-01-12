using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace Develeon64.RoboSushi.Util.Db;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class AppDb
{
    [JsonProperty]
    public ulong notify_message_id { get; set; }

    public static void WriteFile()
    {
        File.WriteAllText("Var/DB/" + "Database.json", JsonConvert.SerializeObject(ConfigManager.Db, ConfigManager.JsonSettings), Encoding.UTF8);
    }
}
