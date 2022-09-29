using TwitchLib.Api.Core.Enums;

namespace Develeon64.RoboSushi.Util.Config;

public struct TwitchAuthConfig {
	public TwitchClientAuthConfig Client { get; set; }
	public TwitchTokenAuthConfig Channel { get; set; }
	public TwitchTokenAuthConfig Bot { get; set; }
	public List<AuthScopes> Scope { get; set; }
	public string[] Scopes {
		set {
			Scope = new();
			foreach (string scope in value) {
				switch (scope) {
					case "analytics:read:extensions":
						Scope.Add(AuthScopes.Helix_Analytics_Read_Extensions);
						break;
					case "analytics:read:games":
						Scope.Add(AuthScopes.Helix_Analytics_Read_Games);
						break;
					case "bits:read":
						Scope.Add(AuthScopes.Helix_Bits_Read);
						break;
					case "chat:edit":
					case "chat:read":
						break;
					case "channel:edit:commercial":
						Scope.Add(AuthScopes.Helix_Channel_Edit_Commercial);
						break;
					case "channel:manage:broadcast":
						Scope.Add(AuthScopes.Helix_Channel_Manage_Broadcast);
						break;
					case "channel:manage:extensions":
						Scope.Add(AuthScopes.Helix_Channel_Manage_Extensions);
						break;
					case "chanel:manage:moderators":
						Scope.Add(AuthScopes.Helix_Channel_Manage_Moderators);
						break;
					case "channel:manage:polls":
						Scope.Add(AuthScopes.Helix_Channel_Manage_Polls);
						break;
					case "channel:manage:predictions":
						Scope.Add(AuthScopes.Helix_Channel_Manage_Predictions);
						break;
					case "channel:manage:raids":
						//Scope.Add(AuthScopes.Helix_Channel_Manage_Raids);
						break;
					case "channel:manage:redemptions":
						Scope.Add(AuthScopes.Helix_Channel_Manage_Redemptions);
						break;
					case "channel:manage:schedule":
						Scope.Add(AuthScopes.Helix_Channel_Manage_Schedule);
						break;
					case "channel:manage:videos":
						Scope.Add(AuthScopes.Helix_Channel_Manage_Videos);
						break;
					case "channel:manage:vips":
						Scope.Add(AuthScopes.Helix_Channel_Manage_VIPs);
						break;
					case "channel:moderate":
						//Scope.Add(AuthScopes.Helix_Channel_Moderate);
						break;
					case "channel:read:charity":
						//Scope.Add(AuthScopes.Helix_Channel_Read_Charity);
						break;
					case "channel:read:editors":
						Scope.Add(AuthScopes.Helix_Channel_Read_Editors);
						break;
					case "channel:read:goals":
						Scope.Add(AuthScopes.Helix_Channel_Read_Goals);
						break;
					case "channel:read:hype_train":
						Scope.Add(AuthScopes.Helix_Channel_Read_Hype_Train);
						break;
					case "channel:read:polls":
						Scope.Add(AuthScopes.Helix_Channel_Read_Polls);
						break;
					case "channel:read:predictions":
						Scope.Add(AuthScopes.Helix_Channel_Read_Predictions);
						break;
					case "channel:read:redemptions":
						Scope.Add(AuthScopes.Helix_Channel_Read_Redemptions);
						break;
					case "channel:read:stream_key":
						Scope.Add(AuthScopes.Helix_Channel_Read_Stream_Key);
						break;
					case "channel:read:subscriptions":
						Scope.Add(AuthScopes.Helix_Channel_Read_Subscriptions);
						break;
					case "channel:read:vips":
						Scope.Add(AuthScopes.Helix_Channel_Read_VIPs);
						break;
					case "clips:edit":
						Scope.Add(AuthScopes.Helix_Clips_Edit);
						break;
					case "moderation:read":
						Scope.Add(AuthScopes.Helix_Moderation_Read);
						break;
					case "moderator:manage:announcements":
						Scope.Add(AuthScopes.Helix_Moderator_Manage_Announcements);
						break;
					case "moderator:manage:automod":
						Scope.Add(AuthScopes.Helix_Moderator_Manage_Automod);
						break;
					case "moderator:manage:automod_settings":
						Scope.Add(AuthScopes.Helix_Moderator_Manage_Automod_Settings);
						break;
					case "moderator:manage:banned_users":
						Scope.Add(AuthScopes.Helix_Moderator_Manage_Banned_Users);
						break;
					case "moderator:manage:blocked_terms":
						Scope.Add(AuthScopes.Helix_Moderator_Manage_Blocked_Terms);
						break;
					case "moderator:manage:chat_messages":
						Scope.Add(AuthScopes.Helix_moderator_Manage_Chat_Messages);
						break;
					case "moderator:manage:chat_settings":
						Scope.Add(AuthScopes.Helix_Moderator_Manage_Chat_Settings);
						break;
					case "moderator:read:automod_settings":
						Scope.Add(AuthScopes.Helix_Moderator_Read_Automod_Settings);
						break;
					case "moderator:read:blocked_terms":
						Scope.Add(AuthScopes.Helix_Moderator_Read_Blocked_Terms);
						break;
					case "moderator:read:chat_settings":
						Scope.Add(AuthScopes.Helix_Moderator_Read_Chat_Settings);
						break;
					case "user:edit":
						Scope.Add(AuthScopes.Helix_User_Edit);
						break;
					case "user:edit:broadcast":
						Scope.Add(AuthScopes.Helix_User_Edit_Broadcast);
						break;
					case "user:edit:follows":
						Scope.Add(AuthScopes.Helix_User_Edit_Follows);
						break;
					case "user:manage:blocked_users":
						Scope.Add(AuthScopes.Helix_User_Manage_BlockedUsers);
						break;
					case "user:manage:chat_color":
						Scope.Add(AuthScopes.Helix_User_Manage_Chat_Color);
						break;
					case "user:manage:whispers":
						Scope.Add(AuthScopes.Helix_User_Manage_Whispers);
						break;
					case "user:read:blocked_users":
						Scope.Add(AuthScopes.Helix_User_Read_BlockedUsers);
						break;
					case "user:read:broadcast":
						Scope.Add(AuthScopes.Helix_User_Read_Broadcast);
						break;
					case "user:read:email":
						Scope.Add(AuthScopes.Helix_User_Read_Email);
						break;
					case "user:read:follows":
						Scope.Add(AuthScopes.Helix_User_Read_Follows);
						break;
					case "user:read:subscriptions":
						Scope.Add(AuthScopes.Helix_User_Read_Subscriptions);
						break;
					case "whispers:edit":
					case "whispers:read":
						break;
				}

				if (Scope.Count == 0)
					Scope.Add(AuthScopes.None);
			}
		}
	}
}
