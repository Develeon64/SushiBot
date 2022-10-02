using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace Develeon64.RoboSushi.Util.Db;

[JsonObject(
	ItemRequired = Required.DisallowNull,
	MemberSerialization = MemberSerialization.OptIn,
	NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public class DiscordDb {
	private ulong? _notifyMessageId;

	[JsonProperty]
	public ulong? NotifyMessageId {
		get => this._notifyMessageId;
		set {
			this._notifyMessageId = value;
			AppDb.WriteFile();
		}
	}
}
