using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;

namespace Develeon64.RoboSushi.Util.Db;

[JsonObject(
	ItemRequired = Required.DisallowNull,
	MemberSerialization = MemberSerialization.OptIn,
	NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class AppDb {
	private DiscordDb _discord;

	[JsonProperty]
	public DiscordDb Discord {
		get => this._discord;
		set {
			this._discord = value;
			AppDb.WriteFile();
		}
	}

	public static void WriteFile () {
		File.WriteAllText("Var/DB/" + "Database.jsonc", JsonConvert.SerializeObject(ConfigManager.Db, ConfigManager.JsonSettings), Encoding.UTF8);
	}
}
