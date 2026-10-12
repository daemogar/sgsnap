using System.Runtime.CompilerServices;

namespace SGSnap;

/// <summary>Snapshot assertions for xUnit v3 tests; file names come from the running test.</summary>
/// <remarks>Failures throw rather than return a faulted task; see README.md "When a snapshot does not match".</remarks>
public static class Snapshot
{
	/// <summary>Compares <paramref name="text"/> with <c>Class.Method.verified.&lt;extension&gt;</c>.</summary>
	public static Task Match(string text, SnapshotSettings? settings = null, string extension = "txt", [CallerFilePath] string sourceFile = "")
		=> Match([new SnapshotTarget(extension, text)], settings, sourceFile);

	/// <summary>Compares several targets at once; named targets become <c>Class.Method#Name.verified.&lt;ext&gt;</c>.</summary>
	public static Task Match(IEnumerable<SnapshotTarget> targets, SnapshotSettings? settings = null, [CallerFilePath] string sourceFile = "")
	{
		SnapshotHost.Run([.. targets], settings ?? new SnapshotSettings(), sourceFile);
		return Task.CompletedTask;
	}

	/// <summary>Runs <paramref name="run"/> in-process and snapshots its exit code, stdout, stderr and the files it wrote under <paramref name="filesRoot"/>.</summary>
	public static Task MatchCli(Func<TextWriter, TextWriter, int> run, SnapshotSettings? settings = null, string? filesRoot = null, [CallerFilePath] string sourceFile = "")
		=> Match(CliCapture.Render(run, filesRoot), settings, "txt", sourceFile);
}
