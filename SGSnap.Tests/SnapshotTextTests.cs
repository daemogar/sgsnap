namespace SGSnap.Tests;

public class SnapshotTextTests
{
	[Fact]
	public void Normalize_turns_crlf_and_lone_cr_into_lf()
		=> Assert.Equal("a\nb\nc\n", SnapshotText.Normalize("a\r\nb\rc\n"));

	[Fact]
	public void Write_puts_a_bom_before_the_content_and_adds_no_newline()
	{
		using var temp = new TempDirectory();
		var path = temp.File("x.verified.txt");

		SnapshotText.Write(path, "a\nb");

		Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'a', (byte)'\n', (byte)'b' }, File.ReadAllBytes(path));
	}

	[Fact]
	public void Write_of_empty_content_is_just_the_bom()
	{
		using var temp = new TempDirectory();
		var path = temp.File("x.verified.txt");

		SnapshotText.Write(path, "");

		Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, File.ReadAllBytes(path));
	}

	[Fact]
	public void ReadVerified_strips_the_bom_and_normalizes_line_endings()
	{
		using var temp = new TempDirectory();
		var path = temp.File("x.verified.txt");
		File.WriteAllBytes(path, [0xEF, 0xBB, 0xBF, (byte)'a', (byte)'\r', (byte)'\n', (byte)'b']);

		Assert.Equal("a\nb", SnapshotText.ReadVerified(path));
	}

	[Fact]
	public void ReadVerified_reads_a_file_without_a_bom()
	{
		using var temp = new TempDirectory();
		var path = temp.File("x.verified.txt");
		File.WriteAllBytes(path, [(byte)'a', (byte)'\r', (byte)'\n', (byte)'b']);

		Assert.Equal("a\nb", SnapshotText.ReadVerified(path));
	}

	[Fact]
	public void ReadVerified_returns_null_for_a_missing_file()
	{
		using var temp = new TempDirectory();

		Assert.Null(SnapshotText.ReadVerified(temp.File("missing.verified.txt")));
	}
}
