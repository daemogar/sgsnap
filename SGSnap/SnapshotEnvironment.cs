namespace SGSnap;

/// <summary>The <c>SGSNAP_*</c> variables and CI detection, read once per snapshot call.</summary>
internal sealed record SnapshotEnvironment(bool Accept, bool CheckOnly, bool IsCI, string? DiffTool, int MaxLaunch)
{
	public static SnapshotEnvironment FromProcess() => From(Environment.GetEnvironmentVariable);

	public static SnapshotEnvironment From(Func<string, string?> get) => new(
		Accept: IsOn(get("SGSNAP_ACCEPT")),
		CheckOnly: IsOn(get("SGSNAP_CHECKONLY")),
		IsCI: string.Equals(get("CI"), "true", StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(get("TF_BUILD")),
		DiffTool: NullIfBlank(get("SGSNAP_DIFFTOOL")) ?? SnapshotDefaults.DiffTool,
		MaxLaunch: int.TryParse(get("SGSNAP_MAXLAUNCH"), out var max) && max >= 0 ? max : 5);

	static bool IsOn(string? value) => value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);

	static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
