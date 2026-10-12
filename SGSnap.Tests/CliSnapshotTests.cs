namespace SGSnap.Tests;

public class CliSnapshotTests
{
	[Fact]
	public Task Snapshots_a_cli_run()
		=> Snapshot.MatchCli((stdout, _) =>
		{
			stdout.WriteLine("done");
			return 0;
		}, new SnapshotSettings { Directory = "Snapshots" });
}
