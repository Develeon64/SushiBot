namespace Develeon64.RoboSushi.Util;

public static class VersionManager {
	public static string Prefix { get; } = "v";
	public static short MajorVersion { get; } = 1;
	public static short MinorVersion { get; } = 1;
	public static short PatchVersion { get; } = 3;
	private static readonly string? preVersion = "version-command";
	private static readonly string? buildVersion = "5";

	public static string PreVersion { get => preVersion != null ? $"-{preVersion}" : String.Empty; }
	public static string BuildVersion { get => buildVersion != null ? $"+{buildVersion}" : String.Empty; }

	public static string FullVersion { get => $"{Prefix}{MajorVersion}.{MinorVersion}.{PatchVersion}{PreVersion}{BuildVersion}"; }
	public static string GitVersion { get => $"{Prefix}1.1.2"; }
}
