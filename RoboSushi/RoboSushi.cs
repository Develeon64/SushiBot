using Develeon64.RoboSushi.Discord;
using Newtonsoft.Json.Linq;

namespace Develeon64.RoboSushi;

public class RoboSushi {
	public static void Main (string[] args) {
		RoboSushi.MainAsync(args).GetAwaiter().GetResult();
	}

	public static async Task MainAsync (string[] args) {
		JObject config = JObject.Parse(File.ReadAllText("token.json"));

		DiscordBot discord = new(config["token"].ToString());
		await Task.Delay(-1);
	}
}
