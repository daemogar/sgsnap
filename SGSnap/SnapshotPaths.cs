namespace SGSnap;

/// <summary>Snapshot file names and folder resolution.</summary>
internal static class SnapshotPaths
{
	/// <summary>The file stem of an unnamed target when file names carry no test-name prefix.</summary>
	public const string UnprefixedStem = "snapshot";

	const string ClassPlaceholder = "{class}";
	const string MethodPlaceholder = "{method}";

	public static readonly StringComparer PathComparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

	/// <summary>A null <paramref name="prefix"/> means no test-name prefix: <c>Name.kind.ext</c>, or <c>snapshot.kind.ext</c> when unnamed.</summary>
	public static string FileName(string? prefix, SnapshotTarget target, string kind)
		=> prefix is null
			? $"{target.Name ?? UnprefixedStem}.{kind}.{target.Extension}"
			: target.Name is null
				? $"{prefix}.{kind}.{target.Extension}"
				: $"{prefix}#{target.Name}.{kind}.{target.Extension}";

	/// <summary>True when <paramref name="configured"/> names a folder per test method, so file names drop the <c>Class.Method</c> prefix.</summary>
	public static bool IsPerTest(string? configured) => configured?.Contains(MethodPlaceholder, StringComparison.Ordinal) == true;

	/// <summary>Fills the <c>{class}</c> and <c>{method}</c> placeholders of a configured folder.</summary>
	public static string? Expand(string? configured, string className, string methodName)
		=> configured?.Replace(ClassPlaceholder, className, StringComparison.Ordinal).Replace(MethodPlaceholder, methodName, StringComparison.Ordinal);

	/// <summary>The absolute snapshot folder for a call made from <paramref name="sourceFile"/>.</summary>
	public static string ResolveDirectory(string? configured, string sourceFile)
	{
		if (string.IsNullOrEmpty(sourceFile))
			throw new ArgumentException("The calling test file's path is required; let [CallerFilePath] supply it.", nameof(sourceFile));

		var testFolder = Path.GetDirectoryName(sourceFile)!;
		return Path.GetFullPath(configured is null ? testFolder : Path.Combine(testFolder, configured));
	}

	/// <summary>Existing files of <paramref name="kind"/> ("verified" or "received") that belong to <paramref name="prefix"/>, or to the whole folder when it is null.</summary>
	public static IEnumerable<string> Find(string directory, string? prefix, string kind)
	{
		if (!Directory.Exists(directory))
			return [];

		// The kind must sit right before the extension, so a name that merely contains ".received." never matches.
		var suffix = $".{kind}";
		return Directory.EnumerateFiles(directory).Where(path =>
		{
			var stem = Path.GetFileNameWithoutExtension(path);
			if (!stem.EndsWith(suffix, StringComparison.Ordinal))
				return false;

			var owner = stem[..^suffix.Length];
			return prefix is null || owner == prefix || owner.StartsWith($"{prefix}#", StringComparison.Ordinal);
		});
	}
}
