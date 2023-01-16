using System.Text;
using Dietze.helper;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dietze.Utils.Db;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class AppDb
{
    [JsonProperty] public ulong NotifyMessageId { get; set; }

    public static void WriteFile()
    {
        File.WriteAllText("Var/DB/Database.json", JsonConvert.SerializeObject(ConfigManager.Db, ConfigManager.JsonSettings), Encoding.UTF8);
    }
}