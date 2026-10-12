using System.Text;

namespace SGSnap;

internal enum FailureKind { Differs, Missing, Orphan }

/// <summary>One file that failed a snapshot comparison.</summary>
internal sealed record SnapshotFailure(FailureKind Kind, string VerifiedPath, string? ReceivedPath, string? Diff);

/// <summary>Formats the failure message: one block per file, with accept commands when a received file exists.</summary>
internal static class SnapshotReport
{
	public static string Format(string prefix, IReadOnlyList<SnapshotFailure> failures)
	{
		var text = new StringBuilder();
		text.Append($"Snapshot mismatch for {prefix} ({failures.Count} file{(failures.Count == 1 ? "" : "s")}):\n");

		foreach (var failure in failures)
		{
			text.Append('\n');
			switch (failure.Kind)
			{
				case FailureKind.Differs:
					text.Append($"Differs: {failure.VerifiedPath}\n").Append(failure.Diff).Append('\n');
					break;
				case FailureKind.Missing:
					text.Append($"Missing: {failure.VerifiedPath} (no verified file yet)\n");
					break;
				case FailureKind.Orphan:
					text.Append($"Orphan: {failure.VerifiedPath} (this run did not produce it; delete it if that output is gone on purpose)\n");
					break;
			}

			if (failure.ReceivedPath is { } received)
				text.Append($"Accept (PowerShell): Move-Item -Force -LiteralPath '{received}' -Destination '{failure.VerifiedPath}'\n")
					.Append($"Accept (POSIX):      mv -f '{received}' '{failure.VerifiedPath}'\n");
		}

		return text.ToString().TrimEnd('\n');
	}
}
