using System.Text;
using Newtonsoft.Json;

using Dietze.Utils.Config;
using Dietze.Utils.Db;
using Newtonsoft.Json.Linq;

namespace Dietze.helper;

public static class ConfigManager
{
    private const string ConfPath = "Var/Config/Configuration.jsonc";
    private const string AuthPath = "Var/Config/Authentification.jsonc";
    private const string dbPath = "Var/DB/Database.json";

    public static JsonSerializerSettings JsonSettings { get; } = new()
    {
        DefaultValueHandling = DefaultValueHandling.Populate,
        FloatFormatHandling = FloatFormatHandling.DefaultValue,
        Formatting = Formatting.None,
        StringEscapeHandling = StringEscapeHandling.EscapeNonAscii
    };

    public static AppConfig Config { get; set; }
    public static AuthConfig Auth { get; set; }
    public static AppDb? Db { get; set; }

    public static void Initialize(string? filePath = null)
    {
        Config = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(ConfPath, Encoding.UTF8), JsonSettings);
        Auth = JsonConvert.DeserializeObject<AuthConfig>(File.ReadAllText(AuthPath, Encoding.UTF8), JsonSettings);
        Db = JsonConvert.DeserializeObject<AppDb>(File.ReadAllText(dbPath, Encoding.UTF8), JsonSettings);
    }

    public static void RefreshTwitchBotTokens(string accessToken, string refreshToken, int? expiresIn = 0)
    {
        var auth = Auth;
        var twitch = auth.Twitch;
        var bot = twitch.Bot;

        bot.Access = accessToken;
        bot.Refresh = refreshToken;

        twitch.Bot = bot;
        auth.Twitch = twitch;
        Auth = auth;

        WriteAuthFile();
    }

    public static void RefreshTwitchChannelTokens(string accessToken, string refreshToken, int? expiresIn = 0)
    {
        var auth = Auth;
        var twitch = auth.Twitch;
        var channel = twitch.Channel;

        channel.Access = accessToken;
        channel.Refresh = refreshToken;

        twitch.Channel = channel;
        auth.Twitch = twitch;
        Auth = auth;

        WriteAuthFile();
    }

    private static void WriteAuthFile()
    {
        File.WriteAllText(AuthPath, JObject.FromObject(Auth).ToString(Formatting.Indented), Encoding.UTF8);
    }
}