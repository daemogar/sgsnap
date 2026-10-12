using System.Text.RegularExpressions;

namespace SGSnap;

/// <summary>Per-call options: where snapshot files live and which scrubbers run on their text.</summary>
public sealed class SnapshotSettings
{
	readonly List<Func<string, string>> scrubbers = [];

	/// <summary>Snapshot folder: absolute, or relative to the calling test file's folder. Null means that folder itself.</summary>
	/// <remarks><c>{class}</c> and <c>{method}</c> placeholders; see README.md "One folder per test".</remarks>
	public string? Directory { get; set; }

	/// <summary>Adds a scrubber. Scrubbers run in the order added, on every target's text.</summary>
	public SnapshotSettings AddScrubber(Func<string, string> scrubber)
	{
		ArgumentNullException.ThrowIfNull(scrubber);
		scrubbers.Add(scrubber);
		return this;
	}

	/// <summary>Adds a scrubber that replaces every match of <paramref name="pattern"/> with <paramref name="replacement"/>.</summary>
	public SnapshotSettings AddScrubber(Regex pattern, string replacement)
	{
		ArgumentNullException.ThrowIfNull(pattern);
		return AddScrubber(text => pattern.Replace(text, replacement));
	}

	internal string Apply(string text)
		=> SnapshotText.Normalize(scrubbers.Aggregate(SnapshotText.Normalize(text), (current, scrub) => scrub(current)));
}
