namespace Develeon64.RoboSushi.Util;

public static class VersionManager {
	public static string Prefix { get; } = "v";
	public static short MajorVersion { get; } = 1;
	public static short MinorVersion { get; } = 2;
	public static short PatchVersion { get; } = 1;
	private static readonly string? preVersion = null;
	private static readonly string? buildVersion = "6";

	public static string PreVersion { get => !String.IsNullOrWhiteSpace(preVersion) ? $"-{preVersion}" : String.Empty; }
	public static string BuildVersion { get => buildVersion != null ? $"+{buildVersion}" : String.Empty; }

	public static string FullVersion { get => $"{Prefix}{MajorVersion}.{MinorVersion}.{PatchVersion}{PreVersion}{BuildVersion}"; }
	public static string GitVersion { get => $"{Prefix}1.2.1"; }
}
