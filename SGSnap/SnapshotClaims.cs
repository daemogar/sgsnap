namespace SGSnap;

/// <summary>Lets each snapshot folder and prefix, or each whole per-test folder, be used by one call per test run, so two calls never fight over one file set.</summary>
internal sealed class SnapshotClaims
{
	public static readonly SnapshotClaims Shared = new();

	readonly Lock gate = new();
	readonly Dictionary<string, string> prefixed = new(SnapshotPaths.PathComparer);
	readonly Dictionary<string, string> wholeFolders = new(SnapshotPaths.PathComparer);
	readonly HashSet<string> prefixedFolders = new(SnapshotPaths.PathComparer);

	/// <summary>Claims <paramref name="directory"/>'s files for <paramref name="filePrefix"/>, or the whole folder when it is null.</summary>
	public void Claim(string directory, string? filePrefix, string testName, string testId)
	{
		lock (gate)
		{
			var (owners, key) = filePrefix is null ? (wholeFolders, directory) : (prefixed, Path.Combine(directory, filePrefix));
			if (owners.TryGetValue(key, out var owner))
				throw new InvalidOperationException(owner == testId
					? $"Snapshot files for {testName} were matched more than once in one test; make one Match call per test, passing several targets at once if needed."
					: $"Snapshot files for {testName} are used by more than one test, such as the rows of a [Theory] or two classes with the same name in one folder; give each test its own snapshot folder or name.");

			if (filePrefix is null ? prefixedFolders.Contains(directory) : wholeFolders.ContainsKey(directory))
				throw new InvalidOperationException($"Snapshot folder {directory} is shared by a test whose file names carry no test-name prefix; that test needs a folder of its own.");

			owners.Add(key, testId);
			if (filePrefix is not null)
				prefixedFolders.Add(directory);
		}
	}
}
