namespace SGSnap.Tests;

public class SnapshotPathsTests
{
	static readonly string Source = Path.Combine(Path.GetTempPath(), "proj", "Tests.cs");

	[Fact]
	public void FileName_without_a_name()
		=> Assert.Equal("C.M.verified.txt", SnapshotPaths.FileName("C.M", new SnapshotTarget("txt", ""), "verified"));

	[Fact]
	public void FileName_with_a_name_uses_a_hash()
		=> Assert.Equal("C.M#Widget.g.received.cs", SnapshotPaths.FileName("C.M", new SnapshotTarget("cs", "", "Widget.g"), "received"));

	[Fact]
	public void ResolveDirectory_defaults_to_the_test_file_folder()
		=> Assert.Equal(Path.Combine(Path.GetTempPath(), "proj"), SnapshotPaths.ResolveDirectory(null, Source));

	[Fact]
	public void ResolveDirectory_resolves_a_relative_folder_against_the_test_file_folder()
		=> Assert.Equal(Path.Combine(Path.GetTempPath(), "proj", "Snapshots"), SnapshotPaths.ResolveDirectory("Snapshots", Source));

	[Fact]
	public void ResolveDirectory_keeps_an_absolute_folder()
	{
		var absolute = Path.Combine(Path.GetTempPath(), "elsewhere");

		Assert.Equal(absolute, SnapshotPaths.ResolveDirectory(absolute, Source));
	}

	[Fact]
	public void ResolveDirectory_needs_the_calling_file()
		=> Assert.Throws<ArgumentException>(() => SnapshotPaths.ResolveDirectory(null, ""));

	[Fact]
	public void Find_returns_only_files_of_that_prefix_and_kind()
	{
		using var temp = new TempDirectory();
		foreach (var name in new[] { "C.M.verified.txt", "C.M#A.verified.cs", "C.M.received.txt", "C.MX.verified.txt", "C.M2#A.verified.cs", "Other.M.verified.txt" })
			File.WriteAllText(temp.File(name), "");

		var found = SnapshotPaths.Find(temp.Path, "C.M", "verified").Select(path => Path.GetFileName(path)!).Order(StringComparer.Ordinal);

		Assert.Equal(new[] { "C.M#A.verified.cs", "C.M.verified.txt" }, found);
	}

	[Fact]
	public void Find_matches_the_kind_only_as_the_suffix_before_the_extension()
	{
		using var temp = new TempDirectory();
		File.WriteAllText(temp.File("C.M#Snapshot.received.g.verified.cs"), "");

		Assert.Empty(SnapshotPaths.Find(temp.Path, "C.M", "received"));
		Assert.Single(SnapshotPaths.Find(temp.Path, "C.M", "verified"));
	}

	[Fact]
	public void Find_in_a_missing_folder_is_empty()
		=> Assert.Empty(SnapshotPaths.Find(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), "C.M", "verified"));
}
