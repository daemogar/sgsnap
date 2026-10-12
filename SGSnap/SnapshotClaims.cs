using System.Collections.Concurrent;

namespace SGSnap;

/// <summary>Lets each snapshot folder and prefix be used by one call per test run, so two calls never fight over one file set.</summary>
internal sealed class SnapshotClaims
{
	public static readonly SnapshotClaims Shared = new();

	readonly ConcurrentDictionary<string, string> owners = new(SnapshotPaths.PathComparer);

	public void Claim(string directory, string prefix, string testId)
	{
		var key = Path.Combine(directory, prefix);
		if (owners.TryAdd(key, testId))
			return;

		throw new InvalidOperationException(owners[key] == testId
			? $"Snapshot files for {prefix} were matched more than once in one test; make one Match call per test, passing several targets at once if needed."
			: $"Snapshot files for {prefix} are used by more than one test, such as the rows of a [Theory] or two classes with the same name in one folder; give each test its own snapshot folder or name.");
	}
}
