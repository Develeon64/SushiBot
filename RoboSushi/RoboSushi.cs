﻿using Develeon64.RoboSushi.Discord;
using Develeon64.RoboSushi.Util;

namespace Develeon64.RoboSushi;

public class RoboSushi {
	public static DiscordBot discordBot;
	public static TwitchBot twitchBot;

	public static void Main (string[] args) { RoboSushi.MainAsync(args).GetAwaiter().GetResult(); }

	public static async Task MainAsync (string[] args) {
		ConfigManager.Initialize();

		RoboSushi.discordBot = new();
		RoboSushi.twitchBot = new();
		
		await Task.Delay(-1);
	}
}
