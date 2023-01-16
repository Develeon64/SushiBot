using Dietze.helper;
using TwitchLib.Api;

namespace Dietze.helper
{
    public class TwitchTokenCheck
    {
        public static async Task CheckTokens(TwitchAPI _api, bool force = false)
        {
            var valid = await _api.Auth.ValidateAccessTokenAsync(ConfigManager.Auth.Twitch.Bot.Access);
            if (valid == null || valid.ExpiresIn > 300 || force)
            {
                var tokens = await _api.Auth.RefreshAuthTokenAsync(ConfigManager.Auth.Twitch.Bot.Refresh,
                    ConfigManager.Auth.Twitch.Client.Secret, ConfigManager.Auth.Twitch.Client.Id);
                ConfigManager.RefreshTwitchBotTokens(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);
                _api.Settings.AccessToken = tokens.AccessToken;
            }

            valid = await _api.Auth.ValidateAccessTokenAsync(ConfigManager.Auth.Twitch.Channel.Access);
            if (valid == null || valid.ExpiresIn > 300 || force)
            {
                var tokens = await _api.Auth.RefreshAuthTokenAsync(ConfigManager.Auth.Twitch.Channel.Refresh,
                    ConfigManager.Auth.Twitch.Client.Secret, ConfigManager.Auth.Twitch.Client.Id);
                ConfigManager.RefreshTwitchChannelTokens(tokens.AccessToken, tokens.RefreshToken, tokens.ExpiresIn);
            }

            await ClassHelper.DiscordBot?.UpdateMemberCount()!;
        }
    }
}
