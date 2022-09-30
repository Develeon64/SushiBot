using Develeon64.RoboSushi.Util;
using TwitchLib.Api;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace Develeon64.RoboSushi;

public partial class TwitchBot {
	private readonly TwitchClient _client = new();
	private readonly TwitchPubSub _pubsub = new();
	private readonly TwitchAPI _api = new();

	private User? _channel;
	private User? _moderator;

	public TwitchBot () {
		this._tokenTimer = new(this.TokenTimer_Tick, null, Timeout.Infinite, 300000);
		this.Initialize();
	}

	private void Client_MessageReceived (object? sender, OnMessageReceivedArgs e) {
		Console.WriteLine("New Message from " + e.ChatMessage.DisplayName + "\n" + e.ChatMessage.Message);
	}

	private void Client_MessageSent (object? sender, OnMessageSentArgs e) {
		Console.WriteLine("New Message from " + e.SentMessage.DisplayName + "\n" + e.SentMessage.Message);
	}

	private async void PubSub_StreamUp (object? sender, OnStreamUpArgs e) {
		Console.WriteLine(e.ChannelId + " went live!");

		var info = await this._api.Helix.Streams.GetStreamsAsync(userIds: new() { e.ChannelId });
		if (info != null && info.Streams != null && info.Streams.Length >= 1) {
			var stream = info.Streams[0];
			string userIcon = (await this._api.Helix.Users.GetUsersAsync(ids: new() { stream.UserId })).Users[0].ProfileImageUrl;
			string gameThumbnail = this.EncodeImageUrl((await this._api.Helix.Games.GetGamesAsync(gameIds: new() { stream.GameId })).Games[0].BoxArtUrl);
			await RoboSushi.discordBot.SendLiveNotification(stream.UserName, stream.GameName, stream.Title, stream.StartedAt, stream.ViewerCount, stream.Language, stream.IsMature, $"{stream.Type.Substring(0, 1).ToUpper()}{stream.Type.Substring(1)}", this.EncodeImageUrl(stream.ThumbnailUrl), gameThumbnail, userIcon);
		}
	}

	private void PubSub_StreamDown (object? sender, OnStreamDownArgs e) {
		Console.WriteLine(e.ChannelId + " is off!");

		/*var user = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		await RoboSushi.discordBot.SendOffNotification(user.DisplayName, DateTime.Now, this.EncodeImageUrl(user.OfflineImageUrl), this.EncodeImageUrl(user.ProfileImageUrl));*/
	}

	private async void PubSub_Ban (object? sender, OnBanArgs e) {
		Console.WriteLine($"{e.BannedBy} banned {e.BannedUser} for \"{e.BanReason}\"");

		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var banner = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.BannedByUserId })).Users[0];
		string bannerName = banner.DisplayName;
		string bannerIcon = this.EncodeImageUrl(banner.ProfileImageUrl);
		var user = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.BannedUserId })).Users[0];
		string userName = user.DisplayName;
		string userIcon = this.EncodeImageUrl(user.ProfileImageUrl);

		await RoboSushi.discordBot.SendBanNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon, e.BanReason);
	}

	private async void PubSub_Unban (object? sender, OnUnbanArgs e) {
		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var banner = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.UnbannedByUserId })).Users[0];
		string bannerName = banner.DisplayName;
		string bannerIcon = this.EncodeImageUrl(banner.ProfileImageUrl);
		var user = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.UnbannedUserId })).Users[0];
		string userName = user.DisplayName;
		string userIcon = this.EncodeImageUrl(user.ProfileImageUrl);

		await RoboSushi.discordBot.SendUnbanNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon);
	}

	private async void PubSub_Timeout (object? sender, OnTimeoutArgs e) {
		Console.WriteLine($"{e.TimedoutBy} banned {e.TimedoutUser} for \"{e.TimeoutReason}\"");

		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var banner = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.TimedoutById })).Users[0];
		string bannerName = banner.DisplayName;
		string bannerIcon = this.EncodeImageUrl(banner.ProfileImageUrl);
		var user = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.TimedoutUserId })).Users[0];
		string userName = user.DisplayName;
		string userIcon = this.EncodeImageUrl(user.ProfileImageUrl);

		await RoboSushi.discordBot.SendTimeoutNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon, e.TimeoutDuration, e.TimeoutReason);
	}

	private async void PubSub_Untimeout (object? sender, OnUntimeoutArgs e) {
		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var banner = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.UntimeoutedByUserId })).Users[0];
		string bannerName = banner.DisplayName;
		string bannerIcon = this.EncodeImageUrl(banner.ProfileImageUrl);
		var user = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.UntimeoutedUserId })).Users[0];
		string userName = user.DisplayName;
		string userIcon = this.EncodeImageUrl(user.ProfileImageUrl);

		await RoboSushi.discordBot.SendUntimeoutNotification(channelName, channelIcon, bannerName, bannerIcon, userName, userIcon);
	}

	/*private void PubSub_AutomodCaughtMessage (object? sender, OnAutomodCaughtMessageArgs e) {
		Console.WriteLine($"AutomodCaughtMessage: [{e.AutomodCaughtMessage.Message.Sender.DisplayName} ({e.AutomodCaughtMessage.ResolverLogin})]: {e.AutomodCaughtMessage.Message}\n{e.AutomodCaughtMessage.Status}: {e.AutomodCaughtMessage.ReasonCode} ({e.AutomodCaughtMessage.ContentClassification.Level} - {e.AutomodCaughtMessage.ContentClassification.Category})");
	}

	private void PubSub_AutomodCaughtUserMessage (object? sender, OnAutomodCaughtUserMessage e) {
		Console.WriteLine($"AutomodCaughtUserMessage: [{e.AutomodCaughtMessage.Status}]: ({e.UserId}) {e.AutomodCaughtMessage.MessageId}");
	}*/

	private async void PubSub_MessageDeleted (object? sender, OnMessageDeletedArgs e) {
		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var deleter = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.DeletedByUserId })).Users[0];
		string deleterName = deleter.DisplayName;
		string deleterIcon = this.EncodeImageUrl(deleter.ProfileImageUrl);
		var user = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.TargetUserId })).Users[0];
		string userName = user.DisplayName;
		string userIcon = this.EncodeImageUrl(user.ProfileImageUrl);

		await RoboSushi.discordBot.SendMessageDeletedNotification(channelName, channelIcon, deleterName, deleterIcon, userName, userIcon, e.Message);
	}

	private async void PubSub_Clear (object? sender, OnClearArgs e) {
		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var clearer = (await this._api.Helix.Users.GetUsersAsync(logins: new() { e.Moderator })).Users[0];
		string clearerName = clearer.DisplayName;
		string clearerIcon = this.EncodeImageUrl(clearer.ProfileImageUrl);

		await RoboSushi.discordBot.SendChatClearedNotification(channelName, channelIcon, clearerName, clearerIcon);
	}

	private async void PubSub_SubscribersOnly (object? sender, OnSubscribersOnlyArgs e) {
		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var moderator = (await this._api.Helix.Users.GetUsersAsync(logins: new() { e.Moderator })).Users[0];
		string modName = moderator.DisplayName;
		string modIcon = this.EncodeImageUrl(moderator.ProfileImageUrl);

		await RoboSushi.discordBot.SendSubscriberOnlyNotification(channelName, channelIcon, modName, modIcon, true);
	}

	private async void PubSub_SubscribersOnlyOff (object? sender, OnSubscribersOnlyOffArgs e) {
		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var moderator = (await this._api.Helix.Users.GetUsersAsync(logins: new() { e.Moderator })).Users[0];
		string modName = moderator.DisplayName;
		string modIcon = this.EncodeImageUrl(moderator.ProfileImageUrl);

		await RoboSushi.discordBot.SendSubscriberOnlyNotification(channelName, channelIcon, modName, modIcon, false);
	}

	private async void PubSub_EmoteOnly (object? sender, TwitchLib.PubSub.Events.OnEmoteOnlyArgs e) {
		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var moderator = (await this._api.Helix.Users.GetUsersAsync(logins: new() { e.Moderator })).Users[0];
		string modName = moderator.DisplayName;
		string modIcon = this.EncodeImageUrl(moderator.ProfileImageUrl);

		await RoboSushi.discordBot.SendEmoteOnlyNotification(channelName, channelIcon, modName, modIcon, true);
	}

	private async void PubSub_EmoteOnlyOff (object? sender, OnEmoteOnlyOffArgs e) {
		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var moderator = (await this._api.Helix.Users.GetUsersAsync(logins: new() { e.Moderator })).Users[0];
		string modName = moderator.DisplayName;
		string modIcon = this.EncodeImageUrl(moderator.ProfileImageUrl);

		await RoboSushi.discordBot.SendEmoteOnlyNotification(channelName, channelIcon, modName, modIcon, false);
	}

	private async void PubSub_R9kBeta (object? sender, OnR9kBetaArgs e) {
		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var moderator = (await this._api.Helix.Users.GetUsersAsync(logins: new() { e.Moderator })).Users[0];
		string modName = moderator.DisplayName;
		string modIcon = this.EncodeImageUrl(moderator.ProfileImageUrl);

		await RoboSushi.discordBot.SendR9kBetaNotification(channelName, channelIcon, modName, modIcon, true);
	}

	private async void PubSub_R9kBetaOff (object? sender, OnR9kBetaOffArgs e) {
		var channel = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		string channelName = channel.DisplayName;
		string channelIcon = this.EncodeImageUrl(channel.ProfileImageUrl);
		var moderator = (await this._api.Helix.Users.GetUsersAsync(logins: new() { e.Moderator })).Users[0];
		string modName = moderator.DisplayName;
		string modIcon = this.EncodeImageUrl(moderator.ProfileImageUrl);

		await RoboSushi.discordBot.SendR9kBetaNotification(channelName, channelIcon, modName, modIcon, false);
	}

	private async Task CheckTokens (bool force = false) {
		var valid = await this._api.Auth.ValidateAccessTokenAsync(ConfigManager.Auth.Twitch.Bot.Access);
		if (valid == null || valid.ExpiresIn > 300 || force == true) {
			var tokens = await this._api.Auth.RefreshAuthTokenAsync(ConfigManager.Auth.Twitch.Bot.Refresh, ConfigManager.Auth.Twitch.Client.Secret, ConfigManager.Auth.Twitch.Client.Id);
			ConfigManager.RefreshTwitchBotTokens(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);
			this._api.Settings.AccessToken = tokens.AccessToken;
		}

		valid = await this._api.Auth.ValidateAccessTokenAsync(ConfigManager.Auth.Twitch.Channel.Access);
		if (valid == null || valid.ExpiresIn > 300 || force == true) {
			var tokens = await this._api.Auth.RefreshAuthTokenAsync(ConfigManager.Auth.Twitch.Channel.Refresh, ConfigManager.Auth.Twitch.Client.Secret, ConfigManager.Auth.Twitch.Client.Id);
			ConfigManager.RefreshTwitchChannelTokens(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);
		}
	}

	private string EncodeImageUrl (string url) {
		return $"{url.Replace("-{width}x{height}", null)}?{DateTimeOffset.Now.ToUnixTimeSeconds()}";
	}
}
