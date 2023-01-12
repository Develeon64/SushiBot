using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using TwitchLib.Api.Core.Enums;

namespace Develeon64.RoboSushi.Util.Config;

[JsonObject(
    ItemRequired = Required.DisallowNull,
    MemberSerialization = MemberSerialization.OptIn,
    NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
public struct TwitchTokenAuthConfig
{
    [JsonProperty] public string Access { get; set; }

    [JsonProperty] public string Refresh { get; set; }

    [JsonProperty] public string[] Scopes { get; set; }

    public List<AuthScopes> GetScopes()
    {
        List<AuthScopes> scopes = new();

        foreach (var scope in Scopes)
        {
            switch (scope)
            {
                case "analytics:read:extensions":
                    scopes.Add(AuthScopes.Helix_Analytics_Read_Extensions);
                    break;
                case "analytics:read:games":
                    scopes.Add(AuthScopes.Helix_Analytics_Read_Games);
                    break;
                case "bits:read":
                    scopes.Add(AuthScopes.Helix_Bits_Read);
                    break;
                case "chat:edit":
                case "chat:read":
                    break;
                case "channel:edit:commercial":
                    scopes.Add(AuthScopes.Helix_Channel_Edit_Commercial);
                    break;
                case "channel:manage:broadcast":
                    scopes.Add(AuthScopes.Helix_Channel_Manage_Broadcast);
                    break;
                case "channel:manage:extensions":
                    scopes.Add(AuthScopes.Helix_Channel_Manage_Extensions);
                    break;
                case "chanel:manage:moderators":
                    scopes.Add(AuthScopes.Helix_Channel_Manage_Moderators);
                    break;
                case "channel:manage:polls":
                    scopes.Add(AuthScopes.Helix_Channel_Manage_Polls);
                    break;
                case "channel:manage:predictions":
                    scopes.Add(AuthScopes.Helix_Channel_Manage_Predictions);
                    break;
                case "channel:manage:raids":
                    //scopes.Add(AuthScopes.Helix_Channel_Manage_Raids);
                    break;
                case "channel:manage:redemptions":
                    scopes.Add(AuthScopes.Helix_Channel_Manage_Redemptions);
                    break;
                case "channel:manage:schedule":
                    scopes.Add(AuthScopes.Helix_Channel_Manage_Schedule);
                    break;
                case "channel:manage:videos":
                    scopes.Add(AuthScopes.Helix_Channel_Manage_Videos);
                    break;
                case "channel:manage:vips":
                    scopes.Add(AuthScopes.Helix_Channel_Manage_VIPs);
                    break;
                case "channel:moderate":
                    //scopes.Add(AuthScopes.Helix_Channel_Moderate);
                    break;
                case "channel:read:charity":
                    //scopes.Add(AuthScopes.Helix_Channel_Read_Charity);
                    break;
                case "channel:read:editors":
                    scopes.Add(AuthScopes.Helix_Channel_Read_Editors);
                    break;
                case "channel:read:goals":
                    scopes.Add(AuthScopes.Helix_Channel_Read_Goals);
                    break;
                case "channel:read:hype_train":
                    scopes.Add(AuthScopes.Helix_Channel_Read_Hype_Train);
                    break;
                case "channel:read:polls":
                    scopes.Add(AuthScopes.Helix_Channel_Read_Polls);
                    break;
                case "channel:read:predictions":
                    scopes.Add(AuthScopes.Helix_Channel_Read_Predictions);
                    break;
                case "channel:read:redemptions":
                    scopes.Add(AuthScopes.Helix_Channel_Read_Redemptions);
                    break;
                case "channel:read:stream_key":
                    scopes.Add(AuthScopes.Helix_Channel_Read_Stream_Key);
                    break;
                case "channel:read:subscriptions":
                    scopes.Add(AuthScopes.Helix_Channel_Read_Subscriptions);
                    break;
                case "channel:read:vips":
                    scopes.Add(AuthScopes.Helix_Channel_Read_VIPs);
                    break;
                case "clips:edit":
                    scopes.Add(AuthScopes.Helix_Clips_Edit);
                    break;
                case "moderation:read":
                    scopes.Add(AuthScopes.Helix_Moderation_Read);
                    break;
                case "moderator:manage:announcements":
                    scopes.Add(AuthScopes.Helix_Moderator_Manage_Announcements);
                    break;
                case "moderator:manage:automod":
                    scopes.Add(AuthScopes.Helix_Moderator_Manage_Automod);
                    break;
                case "moderator:manage:automod_settings":
                    scopes.Add(AuthScopes.Helix_Moderator_Manage_Automod_Settings);
                    break;
                case "moderator:manage:banned_users":
                    scopes.Add(AuthScopes.Helix_Moderator_Manage_Banned_Users);
                    break;
                case "moderator:manage:blocked_terms":
                    scopes.Add(AuthScopes.Helix_Moderator_Manage_Blocked_Terms);
                    break;
                case "moderator:manage:chat_messages":
                    scopes.Add(AuthScopes.Helix_moderator_Manage_Chat_Messages);
                    break;
                case "moderator:manage:chat_settings":
                    scopes.Add(AuthScopes.Helix_Moderator_Manage_Chat_Settings);
                    break;
                case "moderator:read:automod_settings":
                    scopes.Add(AuthScopes.Helix_Moderator_Read_Automod_Settings);
                    break;
                case "moderator:read:blocked_terms":
                    scopes.Add(AuthScopes.Helix_Moderator_Read_Blocked_Terms);
                    break;
                case "moderator:read:chat_settings":
                    scopes.Add(AuthScopes.Helix_Moderator_Read_Chat_Settings);
                    break;
                case "user:edit":
                    scopes.Add(AuthScopes.Helix_User_Edit);
                    break;
                case "user:edit:broadcast":
                    scopes.Add(AuthScopes.Helix_User_Edit_Broadcast);
                    break;
                case "user:edit:follows":
                    scopes.Add(AuthScopes.Helix_User_Edit_Follows);
                    break;
                case "user:manage:blocked_users":
                    scopes.Add(AuthScopes.Helix_User_Manage_BlockedUsers);
                    break;
                case "user:manage:chat_color":
                    scopes.Add(AuthScopes.Helix_User_Manage_Chat_Color);
                    break;
                case "user:manage:whispers":
                    scopes.Add(AuthScopes.Helix_User_Manage_Whispers);
                    break;
                case "user:read:blocked_users":
                    scopes.Add(AuthScopes.Helix_User_Read_BlockedUsers);
                    break;
                case "user:read:broadcast":
                    scopes.Add(AuthScopes.Helix_User_Read_Broadcast);
                    break;
                case "user:read:email":
                    scopes.Add(AuthScopes.Helix_User_Read_Email);
                    break;
                case "user:read:follows":
                    scopes.Add(AuthScopes.Helix_User_Read_Follows);
                    break;
                case "user:read:subscriptions":
                    scopes.Add(AuthScopes.Helix_User_Read_Subscriptions);
                    break;
                case "whispers:edit":
                case "whispers:read":
                    break;
            }

            if (scopes.Count == 0)
                scopes.Add(AuthScopes.None);
        }

        return scopes;
    }
}