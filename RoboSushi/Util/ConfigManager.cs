using Develeon64.RoboSushi.Util.Config;
using Newtonsoft.Json.Linq;

namespace Develeon64.RoboSushi.Util;

public static class ConfigManager {
	public static AppConfig Config { get; set; }
	private static string path = "Var/Config/Discord.json";

	public static void Initialize (string? filePath = null) {
		ConfigManager.path = filePath ?? ConfigManager.path;
		Config = JToken.Parse(File.ReadAllText(ConfigManager.path)).ToObject<AppConfig>();
	}
}
