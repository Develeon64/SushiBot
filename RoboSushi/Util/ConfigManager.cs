﻿using Develeon64.RoboSushi.Util.Config;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace Develeon64.RoboSushi.Util;

public static class ConfigManager {
	public static AppConfig Config { get; set; }
	public static AuthConfig Auth { get; set; }
	private static string confPath = "Var/Config/Configuration.json";
	private static string authPath = "Var/Config/Authentification.json";

	public static void Initialize (string? filePath = null) {
		ConfigManager.confPath = filePath ?? ConfigManager.confPath;
		ConfigManager.authPath = filePath ?? ConfigManager.authPath;
		ConfigManager.Config = JToken.Parse(File.ReadAllText(ConfigManager.confPath)).ToObject<AppConfig>();
		ConfigManager.Auth = JToken.Parse(File.ReadAllText(ConfigManager.authPath)).ToObject<AuthConfig>();
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
