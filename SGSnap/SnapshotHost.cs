using Xunit;

namespace SGSnap;

/// <summary>Wires the engine to the running test, the process environment and the shared diff launcher.</summary>
internal static class SnapshotHost
{
	public static void Run(IReadOnlyList<SnapshotTarget> targets, SnapshotSettings settings, string sourceFile)
	{
		var (className, methodName) = TestNaming.Current();
		var prefix = $"{className}.{methodName}";
		var directory = SnapshotPaths.ResolveDirectory(SnapshotPaths.Expand(settings.Directory, className, methodName), sourceFile);
		SnapshotClaims.Shared.Claim(directory, SnapshotPaths.IsPerTest(settings.Directory) ? null : prefix, prefix, TestContext.Current.Test?.UniqueID ?? prefix);
		new SnapshotEngine(SnapshotEnvironment.FromProcess(), DiffLauncher.Shared, TestContext.Current.AddWarning)
			.Run(directory, prefix, targets, settings);
	}
}
