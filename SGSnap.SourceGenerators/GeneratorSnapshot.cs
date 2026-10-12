using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;

namespace SGSnap.SourceGenerators;

/// <summary>Snapshots a source generator run: one file per generated source, plus a diagnostics file when there are diagnostics.</summary>
public static class GeneratorSnapshot
{
	/// <summary>Snapshots <paramref name="driver"/>'s latest run.</summary>
	public static Task Match(GeneratorDriver driver, SnapshotSettings? settings = null, [CallerFilePath] string sourceFile = "")
	{
		ArgumentNullException.ThrowIfNull(driver);
		return Match(driver.GetRunResult(), settings, sourceFile);
	}

	/// <summary>Snapshots a generator run result.</summary>
	public static Task Match(GeneratorDriverRunResult result, SnapshotSettings? settings = null, [CallerFilePath] string sourceFile = "")
		=> Snapshot.Match(GeneratorTargets.From(result), settings, sourceFile);
}
