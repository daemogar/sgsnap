using System.Runtime.ExceptionServices;

using Microsoft.CodeAnalysis;

namespace SGSnap.SourceGenerators;

/// <summary>Turns a generator run into snapshot targets: diagnostics first (unnamed .txt), then each result's sources in ordinal hint order.</summary>
internal static class GeneratorTargets
{
	public static IReadOnlyList<SnapshotTarget> From(GeneratorDriverRunResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		var exceptions = result.Results.Select(run => run.Exception).OfType<Exception>().ToList();
		if (exceptions.Count == 1)
			ExceptionDispatchInfo.Throw(exceptions[0]);
		if (exceptions.Count > 1)
			throw new AggregateException(exceptions);

		var targets = new List<SnapshotTarget>();
		if (!result.Diagnostics.IsEmpty)
			targets.Add(new SnapshotTarget("txt", DiagnosticWriter.Write(result.Diagnostics)));

		foreach (var run in result.Results)
			foreach (var source in run.GeneratedSources.OrderBy(source => source.HintName, StringComparer.Ordinal))
				targets.Add(ToTarget(source));

		return targets;
	}

	static SnapshotTarget ToTarget(GeneratedSourceResult source)
	{
		var content = $"//HintName: {source.HintName}\n{source.SourceText}";
		var path = source.SyntaxTree.FilePath;
		if (path.Length > 0)
		{
			var extension = Path.GetExtension(path);
			return new SnapshotTarget(extension.Length > 1 ? extension[1..] : "cs", content, Path.GetFileNameWithoutExtension(path));
		}

		return new SnapshotTarget(
			source.HintName.EndsWith(".vb", StringComparison.Ordinal) ? "vb" : "cs",
			content,
			Path.GetFileNameWithoutExtension(source.HintName));
	}
}
