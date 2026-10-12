using System.Runtime.CompilerServices;

namespace SGSnap;

/// <summary>Snapshot assertions for xUnit v3 tests; file names come from the running test.</summary>
public static class Snapshot
{
	/// <summary>Compares <paramref name="text"/> with <c>Class.Method.verified.&lt;extension&gt;</c>.</summary>
	public static Task Match(string text, SnapshotSettings? settings = null, string extension = "txt", [CallerFilePath] string sourceFile = "")
		=> Match([new SnapshotTarget(extension, text)], settings, sourceFile);

	/// <summary>Compares several targets at once; named targets become <c>Class.Method#Name.verified.&lt;ext&gt;</c>.</summary>
	public static Task Match(IEnumerable<SnapshotTarget> targets, SnapshotSettings? settings = null, [CallerFilePath] string sourceFile = "")
	{
		try
		{
			SnapshotHost.Run([.. targets], settings ?? new SnapshotSettings(), sourceFile);
			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}

	/// <summary>Runs <paramref name="run"/> in-process and snapshots its exit code, stdout, stderr and the files it wrote under <paramref name="filesRoot"/>.</summary>
	public static Task MatchCli(Func<TextWriter, TextWriter, int> run, SnapshotSettings? settings = null, string? filesRoot = null, [CallerFilePath] string sourceFile = "")
	{
		string text;
		try
		{
			text = CliCapture.Render(run, filesRoot);
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}

		return Match(text, settings, "txt", sourceFile);
	}
}
