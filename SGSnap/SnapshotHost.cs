using Xunit;

namespace SGSnap;

/// <summary>Wires the engine to the running test, the process environment and the shared diff launcher.</summary>
internal static class SnapshotHost
{
	public static void Run(IReadOnlyList<SnapshotTarget> targets, SnapshotSettings settings, string sourceFile)
	{
		var prefix = TestNaming.CurrentPrefix();
		var directory = SnapshotPaths.ResolveDirectory(settings.Directory, sourceFile);
		SnapshotClaims.Shared.Claim(directory, prefix, TestContext.Current.Test?.UniqueID ?? prefix);
		new SnapshotEngine(SnapshotEnvironment.FromProcess(), DiffLauncher.Shared, TestContext.Current.AddWarning)
			.Run(directory, prefix, targets, settings);
	}
}
