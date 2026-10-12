using System.Text.RegularExpressions;

namespace SGSnap.Tests;

public class SnapshotSettingsTests
{
	[Fact]
	public void Scrubbers_run_in_the_order_added()
		=> Assert.Equal("x12", new SnapshotSettings().AddScrubber(t => t + "1").AddScrubber(t => t + "2").Apply("x"));

	[Fact]
	public void Regex_scrubber_replaces_every_match()
		=> Assert.Equal(
			"Offset = {scrubbed}, Offset = {scrubbed}",
			new SnapshotSettings().AddScrubber(new Regex(@"Offset = \d+"), "Offset = {scrubbed}").Apply("Offset = 12, Offset = 7"));

	[Fact]
	public void Apply_normalizes_line_endings_a_scrubber_introduces()
		=> Assert.Equal("a\nb", new SnapshotSettings().AddScrubber(t => t.Replace("|", "\r\n")).Apply("a|b"));
}
