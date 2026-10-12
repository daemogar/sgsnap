namespace SGSnap;

/// <summary>Process-wide defaults, typically set once from a <c>[ModuleInitializer]</c>.</summary>
public static class SnapshotDefaults
{
	/// <summary>Diff-tool command with <c>{received}</c> and <c>{verified}</c> placeholders; <c>SGSNAP_DIFFTOOL</c> overrides it. Null means VS Code.</summary>
	public static string? DiffTool { get; set; }
}
