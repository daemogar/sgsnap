using System.Runtime.CompilerServices;

using Microsoft.CodeAnalysis;

namespace SGSnap.SourceGenerators;

/// <summary>Snapshots a source generator run: one file per generated source, plus a diagnostics file when there are diagnostics.</summary>
public static class GeneratorSnapshot
{
	/// <summary>Snapshots <paramref name="driver"/>'s latest run.</summary>
	public static Task Match(GeneratorDriver driver, SnapshotSettings? settings = null, [CallerFilePath] string sourceFile = "")
	{
		GeneratorDriverRunResult result;
		try
		{
			ArgumentNullException.ThrowIfNull(driver);
			result = driver.GetRunResult();
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}

		return Match(result, settings, sourceFile);
	}

	/// <summary>Snapshots a generator run result.</summary>
	public static Task Match(GeneratorDriverRunResult result, SnapshotSettings? settings = null, [CallerFilePath] string sourceFile = "")
	{
		IReadOnlyList<SnapshotTarget> targets;
		try
		{
			targets = GeneratorTargets.From(result);
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}

		return Snapshot.Match(targets, settings, sourceFile);
	}
}
