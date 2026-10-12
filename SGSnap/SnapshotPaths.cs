namespace SGSnap;

/// <summary>Snapshot file names and folder resolution.</summary>
internal static class SnapshotPaths
{
	public static readonly StringComparer PathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	public static string FileName(string prefix, SnapshotTarget target, string kind)
		=> target.Name is null
			? $"{prefix}.{kind}.{target.Extension}"
			: $"{prefix}#{target.Name}.{kind}.{target.Extension}";

	/// <summary>The absolute snapshot folder for a call made from <paramref name="sourceFile"/>.</summary>
	public static string ResolveDirectory(string? configured, string sourceFile)
	{
		if (string.IsNullOrEmpty(sourceFile))
			throw new ArgumentException("The calling test file's path is required; let [CallerFilePath] supply it.", nameof(sourceFile));

		var testFolder = Path.GetDirectoryName(sourceFile)!;
		return Path.GetFullPath(configured is null ? testFolder : Path.Combine(testFolder, configured));
	}

	/// <summary>Existing files of <paramref name="kind"/> ("verified" or "received") that belong to <paramref name="prefix"/>.</summary>
	public static IEnumerable<string> Find(string directory, string prefix, string kind)
	{
		if (!Directory.Exists(directory))
			return [];

		var plain = $"{prefix}.{kind}.";
		var named = $"{prefix}#";
		var marker = $".{kind}.";
		return Directory.EnumerateFiles(directory).Where(path =>
		{
			var name = Path.GetFileName(path);
			return name.StartsWith(plain, StringComparison.Ordinal)
				|| (name.StartsWith(named, StringComparison.Ordinal) && name.Contains(marker, StringComparison.Ordinal));
		});
	}
}
