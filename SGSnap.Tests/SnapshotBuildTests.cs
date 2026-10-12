namespace SGSnap.Tests;

public class SnapshotBuildTests
{
	// Committed .verified.cs snapshots are C# text; the package's build targets keep them out of compilation.
	[Fact]
	public void Verified_cs_snapshots_are_not_compiled_into_the_test_assembly()
		=> Assert.Null(typeof(SnapshotBuildTests).Assembly.GetType("Fixture.WidgetAttribute"));
}
