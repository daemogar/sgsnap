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

	[Fact]
	public void Failures_come_back_as_a_faulted_task()
	{
		var task = Snapshot.Match([], new SnapshotSettings { Directory = "Snapshots" });

		Assert.True(task.IsFaulted);
		Assert.IsType<ArgumentException>(task.Exception!.InnerException);
	}
}
