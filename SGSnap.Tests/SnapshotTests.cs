namespace SGSnap.Tests;

public class SnapshotTests
{
	[Fact]
	public Task Matches_a_committed_snapshot()
		=> Snapshot.Match("first line\nsecond line\n", new SnapshotSettings { Directory = "Snapshots" });

	[Fact]
	public Task Writes_named_targets_with_a_hash()
		=> Snapshot.Match(
			[new SnapshotTarget("txt", "summary"), new SnapshotTarget("cs", "// generated", "Widget")],
			new SnapshotSettings { Directory = "Snapshots" });

	// Throwing (not a faulted Task) makes a call that is not awaited fail the test instead of passing silently.
	[Fact]
	public void Failures_throw_instead_of_returning_a_faulted_task()
		=> Assert.Throws<ArgumentException>(() => { _ = Snapshot.Match([], new SnapshotSettings { Directory = "Snapshots" }); });

	[Fact]
	public async Task A_second_call_in_one_test_is_rejected()
	{
		await Snapshot.Match("one", new SnapshotSettings { Directory = "Snapshots" });

		var error = Assert.Throws<InvalidOperationException>(() => { _ = Snapshot.Match("two", new SnapshotSettings { Directory = "Snapshots" }); });
		Assert.Contains("more than once", error.Message);
	}
}
