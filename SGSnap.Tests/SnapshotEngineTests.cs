using System.Text;
using System.Text.RegularExpressions;

namespace SGSnap.Tests;

public class SnapshotEngineTests
{
	static readonly SnapshotEnvironment Local = new(Accept: false, CheckOnly: false, IsCI: false, DiffTool: null, MaxLaunch: 5);

	/// <summary>A snapshot folder, a fake diff tool in a separate folder, and recorded launches and warnings.</summary>
	sealed class Harness : IDisposable
	{
		readonly TempDirectory tools = new();

		public TempDirectory Folder { get; } = new();
		public RecordingStarter Starter { get; } = new();
		public List<string> Warnings { get; } = [];
		public string Tool => tools.File("tool");

		public Harness() => File.WriteAllText(Tool, "");

		public void Run(SnapshotEnvironment environment, params SnapshotTarget[] targets)
			=> Run(environment, new SnapshotSettings(), targets);

		public void Run(SnapshotEnvironment environment, SnapshotSettings settings, params SnapshotTarget[] targets)
			=> new SnapshotEngine(environment with { DiffTool = $"\"{Tool}\" \"{{received}}\" \"{{verified}}\"" }, new DiffLauncher(Starter, _ => null), Warnings.Add)
				.Run(Folder.Path, "C.M", targets, settings);

		public string PathOf(string name) => Folder.File(name);

		public void Write(string name, string content) => SnapshotText.Write(Folder.File(name), content);

		public string[] Files() => [.. Directory.GetFiles(Folder.Path).Select(path => Path.GetFileName(path)!).Order(StringComparer.Ordinal)];

		public void Dispose()
		{
			Folder.Dispose();
			tools.Dispose();
		}
	}

	[Fact]
	public void A_match_passes_and_leaves_only_the_verified_file()
	{
		using var h = new Harness();
		h.Write("C.M.verified.txt", "hello");

		h.Run(Local, new SnapshotTarget("txt", "hello"));

		Assert.Equal(new[] { "C.M.verified.txt" }, h.Files());
		Assert.Empty(h.Starter.Calls);
	}

	[Fact]
	public void A_verified_file_with_crlf_and_no_bom_still_passes()
	{
		using var h = new Harness();
		File.WriteAllText(h.PathOf("C.M.verified.txt"), "a\r\nb", new UTF8Encoding(false));

		h.Run(Local, new SnapshotTarget("txt", "a\nb"));
	}

	[Fact]
	public void A_difference_writes_received_launches_the_tool_and_fails()
	{
		using var h = new Harness();
		h.Write("C.M.verified.txt", "old");

		var error = Assert.Throws<SnapshotMismatchException>(() => h.Run(Local, new SnapshotTarget("txt", "new")));

		Assert.Contains("Differs: " + h.PathOf("C.M.verified.txt"), error.Message);
		Assert.Contains("-old", error.Message);
		Assert.Contains("+new", error.Message);
		Assert.Contains("Accept (PowerShell)", error.Message);
		Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'n', (byte)'e', (byte)'w' }, File.ReadAllBytes(h.PathOf("C.M.received.txt")));
		var call = Assert.Single(h.Starter.Calls);
		Assert.Equal(h.Tool, call.File);
		Assert.Equal(new[] { h.PathOf("C.M.received.txt"), h.PathOf("C.M.verified.txt") }, call.Args);
	}

	[Fact]
	public void A_missing_verified_file_writes_received_and_fails()
	{
		using var h = new Harness();

		var error = Assert.Throws<SnapshotMismatchException>(() => h.Run(Local, new SnapshotTarget("cs", "x", "Widget")));

		Assert.Contains("Missing: " + h.PathOf("C.M#Widget.verified.cs"), error.Message);
		Assert.True(File.Exists(h.PathOf("C.M#Widget.received.cs")));
		Assert.Empty(h.Starter.Calls);
	}

	[Fact]
	public void Stale_received_files_are_removed_on_a_pass()
	{
		using var h = new Harness();
		h.Write("C.M.verified.txt", "same");
		h.Write("C.M.received.txt", "leftover");
		h.Write("C.M#Gone.received.cs", "leftover");

		h.Run(Local, new SnapshotTarget("txt", "same"));

		Assert.Equal(new[] { "C.M.verified.txt" }, h.Files());
	}

	[Fact]
	public void An_orphan_verified_file_fails_by_default()
	{
		using var h = new Harness();
		h.Write("C.M.verified.txt", "same");
		h.Write("C.M#Old.verified.cs", "gone");

		var error = Assert.Throws<SnapshotMismatchException>(() => h.Run(Local, new SnapshotTarget("txt", "same")));

		Assert.Contains("Orphan: " + h.PathOf("C.M#Old.verified.cs"), error.Message);
		Assert.True(File.Exists(h.PathOf("C.M#Old.verified.cs")));
	}

	[Fact]
	public void Accept_writes_verified_deletes_orphans_and_passes()
	{
		using var h = new Harness();
		h.Write("C.M.verified.txt", "old");
		h.Write("C.M#Old.verified.cs", "gone");

		h.Run(Local with { Accept = true }, new SnapshotTarget("txt", "new"), new SnapshotTarget("cs", "fresh", "New"));

		Assert.Equal(new[] { "C.M#New.verified.cs", "C.M.verified.txt" }, h.Files());
		Assert.Equal("new", SnapshotText.ReadVerified(h.PathOf("C.M.verified.txt")));
		Assert.Contains("Accepted snapshot: " + h.PathOf("C.M.verified.txt"), h.Warnings);
		Assert.Contains("Deleted orphan snapshot: " + h.PathOf("C.M#Old.verified.cs"), h.Warnings);
		Assert.Empty(h.Starter.Calls);
	}

	[Fact]
	public void Accept_is_ignored_in_CI()
	{
		using var h = new Harness();
		h.Write("C.M.verified.txt", "old");

		Assert.Throws<SnapshotMismatchException>(() => h.Run(Local with { Accept = true, IsCI = true }, new SnapshotTarget("txt", "new")));

		Assert.Equal("old", SnapshotText.ReadVerified(h.PathOf("C.M.verified.txt")));
		Assert.True(File.Exists(h.PathOf("C.M.received.txt")));
		Assert.Contains("SGSNAP_ACCEPT is ignored in CI; snapshots were compared normally.", h.Warnings);
		Assert.Empty(h.Starter.Calls);
	}

	[Fact]
	public void Check_only_fails_without_writing_or_launching()
	{
		using var h = new Harness();
		h.Write("C.M.verified.txt", "old");

		var error = Assert.Throws<SnapshotMismatchException>(() => h.Run(Local with { CheckOnly = true }, new SnapshotTarget("txt", "new")));

		Assert.Equal(new[] { "C.M.verified.txt" }, h.Files());
		Assert.DoesNotContain("Accept", error.Message);
		Assert.Empty(h.Starter.Calls);
	}

	[Fact]
	public void Check_only_does_not_create_a_missing_folder()
	{
		using var temp = new TempDirectory();
		var folder = temp.File("Snapshots");
		var engine = new SnapshotEngine(Local with { CheckOnly = true }, new DiffLauncher(new RecordingStarter(), _ => null), _ => { });

		Assert.Throws<SnapshotMismatchException>(() => engine.Run(folder, "C.M", [new SnapshotTarget("txt", "x")], new SnapshotSettings()));

		Assert.False(Directory.Exists(folder));
	}

	[Fact]
	public void Accept_and_check_only_together_is_an_error()
	{
		using var h = new Harness();

		Assert.Throws<InvalidOperationException>(() => h.Run(Local with { Accept = true, CheckOnly = true }, new SnapshotTarget("txt", "x")));
	}

	[Fact]
	public void Two_targets_with_the_same_file_name_are_rejected()
	{
		using var h = new Harness();

		Assert.Throws<ArgumentException>(() => h.Run(Local, new SnapshotTarget("cs", "a", "W"), new SnapshotTarget("cs", "b", "W")));
		Assert.Empty(h.Files());
	}

	[Fact]
	public void Scrubbers_apply_before_the_comparison()
	{
		using var h = new Harness();
		h.Write("C.M.verified.txt", "id N");

		h.Run(Local, new SnapshotSettings().AddScrubber(new Regex(@"\d+"), "N"), new SnapshotTarget("txt", "id 42"));
	}

	[Fact]
	public void Every_target_is_compared_before_failing()
	{
		using var h = new Harness();
		h.Write("C.M.verified.txt", "a");
		h.Write("C.M#W.verified.cs", "b");

		var error = Assert.Throws<SnapshotMismatchException>(() => h.Run(Local, new SnapshotTarget("txt", "A"), new SnapshotTarget("cs", "B", "W")));

		Assert.StartsWith("Snapshot mismatch for C.M (2 files):", error.Message);
		Assert.True(File.Exists(h.PathOf("C.M.received.txt")));
		Assert.True(File.Exists(h.PathOf("C.M#W.received.cs")));
	}
}
