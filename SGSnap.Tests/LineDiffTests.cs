namespace SGSnap.Tests;

public class LineDiffTests
{
	[Fact]
	public void Identical_inputs_are_all_unchanged()
		=> Assert.All(LineDiff.Compute(["x", "y"], ["x", "y"]), edit => Assert.Equal(LineDiff.Kind.Same, edit.Kind));

	[Fact]
	public void One_changed_line_shows_with_three_lines_of_context()
	{
		var verified = string.Join("\n", Enumerable.Range(1, 10));
		var received = verified.Replace("\n5\n", "\nfive\n");

		Assert.Equal("""
			--- verified
			+++ received
			@@ -2,7 +2,7 @@
			 2
			 3
			 4
			-5
			+five
			 6
			 7
			 8
			""".ReplaceLineEndings("\n"), LineDiff.Unified(verified, received));
	}

	[Fact]
	public void A_line_added_at_the_end_shows_as_an_insert()
		=> Assert.EndsWith(" b\n+c", LineDiff.Unified("a\nb", "a\nb\nc"));

	[Fact]
	public void Empty_verified_text_against_new_text()
		=> Assert.Contains("+x", LineDiff.Unified("", "x"));

	[Fact]
	public void Long_diffs_are_cut_off_with_a_count()
	{
		var verified = string.Join("\n", Enumerable.Range(0, 300).Select(i => $"a{i}"));
		var received = string.Join("\n", Enumerable.Range(0, 300).Select(i => $"b{i}"));

		var lines = LineDiff.Unified(verified, received).Split('\n');

		Assert.Equal(201, lines.Length);
		Assert.Equal("... (+403 more diff lines)", lines[^1]);
	}

	[Fact]
	public void Very_large_rewrites_skip_the_search_and_still_render()
	{
		var verified = string.Join("\n", Enumerable.Range(0, 1500).Select(i => $"a{i}"));
		var received = string.Join("\n", Enumerable.Range(0, 1500).Select(i => $"b{i}"));

		var lines = LineDiff.Unified(verified, received).Split('\n');

		Assert.Equal("--- verified", lines[0]);
		Assert.Equal("-a0", lines[3]);
		Assert.Equal("... (+2803 more diff lines)", lines[^1]);
	}
}
