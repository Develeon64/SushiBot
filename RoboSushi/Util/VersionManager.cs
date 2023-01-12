namespace Develeon64.RoboSushi.Util;

public static class VersionManager
{
    private static readonly string? preVersion = null;
    private const string? buildVersion = "16";
    public static string Prefix => "v";
    public static short MajorVersion => 1;
    public static short MinorVersion => 4;
    public static short PatchVersion => 5;

    public static string PreVersion => !string.IsNullOrWhiteSpace(preVersion) ? $"-{preVersion}" : string.Empty;
    public static string BuildVersion => $"+{buildVersion}";

    public static string FullVersion =>
        $"{Prefix}{MajorVersion}.{MinorVersion}.{PatchVersion}{PreVersion}{BuildVersion}";

    public static string GitVersion => $"{Prefix}1.4.5";
}