using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.PubSub;

namespace Develeon64.RoboSushi;

public partial class TwitchBot {
	private readonly TwitchClient _client = new();
	private readonly TwitchPubSub _pubsub = new();
	private readonly TwitchAPI _api = new();

	private User? _channel;
	private User? _moderator;

	public TwitchBot () {
		this.Initialize();
	}
}
