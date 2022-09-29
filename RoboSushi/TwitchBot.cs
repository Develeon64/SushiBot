﻿using TwitchLib.Api;
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

	private async void PubSub_StreamDown (object? sender, OnStreamDownArgs e) {
		Console.WriteLine(e.ChannelId + " is off!");

		var user = (await this._api.Helix.Users.GetUsersAsync(ids: new() { e.ChannelId })).Users[0];
		await RoboSushi.discordBot.SendOffNotification(user.DisplayName, DateTime.Now, this.EncodeImageUrl(user.OfflineImageUrl), this.EncodeImageUrl(user.ProfileImageUrl));
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

	private string EncodeImageUrl (string url) {
		return $"{url.Replace("-{width}x{height}", null)}?{DateTimeOffset.Now.ToUnixTimeSeconds()}";
	}
}