using Develeon64.RoboSushi.Util.Config;
using Develeon64.RoboSushi.Util.Db;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Develeon64.RoboSushi.Util;

public static class ConfigManager {
	private static string confPath = "Var/Config/Configuration.jsonc";
	private static string authPath = "Var/Config/Authentification.jsonc";
	private static string dbPath = "Var/DB/";
	private static string dbName = "Database.json";

	public static JsonSerializerSettings JsonSettings { get; } = new() {
		DefaultValueHandling = DefaultValueHandling.Populate,
		FloatFormatHandling = FloatFormatHandling.DefaultValue,
		Formatting = Formatting.None,
		StringEscapeHandling = StringEscapeHandling.EscapeNonAscii,
	};

	public static AppConfig Config { get; set; }
	public static AuthConfig Auth { get; set; }
	public static AppDb Db { get; set; }

	public static void Initialize (string? filePath = null) {
		ConfigManager.confPath = filePath ?? ConfigManager.confPath;
		ConfigManager.authPath = filePath ?? ConfigManager.authPath;
		//ConfigManager.Config = JToken.Parse(File.ReadAllText(ConfigManager.confPath)).ToObject<AppConfig>();
		ConfigManager.Config = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(ConfigManager.confPath, Encoding.UTF8), JsonSettings);
		//ConfigManager.Auth = JToken.Parse(File.ReadAllText(ConfigManager.authPath)).ToObject<AuthConfig>();
		ConfigManager.Auth = JsonConvert.DeserializeObject<AuthConfig>(File.ReadAllText(ConfigManager.authPath, Encoding.UTF8), JsonSettings);
		ConfigManager.Db = JsonConvert.DeserializeObject<AppDb>(File.ReadAllText("Var/DB/" + "Database.json"), JsonSettings);
		AppDb.WriteFile();
	}

	public static void RefreshTwitchBotTokens (string accessToken, string refreshToken, int? expiresIn = 0) {
		var auth = ConfigManager.Auth;
		var twitch = auth.Twitch;
		var bot = twitch.Bot;

		bot.Access = accessToken;
		bot.Refresh = refreshToken;

		twitch.Bot = bot;
		auth.Twitch = twitch;
		ConfigManager.Auth = auth;

		ConfigManager.WriteAuthFile();
	}

	public static void RefreshTwitchChannelTokens (string accessToken, string refreshToken, int? expiresIn = 0) {
		var auth = ConfigManager.Auth;
		var twitch = auth.Twitch;
		var channel = twitch.Channel;

		channel.Access = accessToken;
		channel.Refresh = refreshToken;

		twitch.Channel = channel;
		auth.Twitch = twitch;
		ConfigManager.Auth = auth;

		ConfigManager.WriteAuthFile();
	}

	private static void WriteAuthFile () {
		File.WriteAllText(ConfigManager.authPath, JObject.FromObject(ConfigManager.Auth).ToString(Formatting.Indented), Encoding.UTF8);
	}
}
