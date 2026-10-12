using System.Security.Cryptography;

namespace SGSnap.Tests;

public class CliCaptureTests
{
	[Fact]
	public void Captures_the_exit_code_and_both_streams()
		=> Assert.Equal(
			"ExitCode: 3\n--- stdout ---\nhello\n--- stderr ---\nwarn\n",
			CliCapture.Render((stdout, stderr) =>
			{
				stdout.Write("hello");
				stderr.Write("warn\r\n");
				return 3;
			}, null));

	[Fact]
	public void Empty_streams_keep_their_headers()
		=> Assert.Equal("ExitCode: 0\n--- stdout ---\n--- stderr ---\n", CliCapture.Render((_, _) => 0, null));

	[Fact]
	public void Lists_files_added_changed_and_deleted()
	{
		using var temp = new TempDirectory();
		File.WriteAllText(temp.File("keep.txt"), "same");
		File.WriteAllText(temp.File("change.txt"), "before");
		File.WriteAllText(temp.File("gone.txt"), "bye");

		var text = CliCapture.Render((_, _) =>
		{
			Directory.CreateDirectory(temp.File("new"));
			File.WriteAllText(temp.File("new/added.json"), "{}\n");
			File.WriteAllText(temp.File("change.txt"), "after");
			File.Delete(temp.File("gone.txt"));
			return 0;
		}, temp.Path);

		Assert.Equal(
			"ExitCode: 0\n--- stdout ---\n--- stderr ---\n--- file: change.txt ---\nafter\n--- file: new/added.json ---\n{}\n--- deleted: gone.txt ---\n",
			text);
	}

	[Fact]
	public void Binary_files_show_size_and_hash()
	{
		using var temp = new TempDirectory();
		byte[] bytes = [0, 1, 2];

		var text = CliCapture.Render((_, _) =>
		{
			File.WriteAllBytes(temp.File("data.bin"), bytes);
			return 0;
		}, temp.Path);

		Assert.EndsWith($"--- file: data.bin ---\n(binary, 3 bytes, sha256 {Convert.ToHexStringLower(SHA256.HashData(bytes))})\n", text);
	}
}
