namespace SGSnap.Tests;

public class ProcessStarterTests
{
	public static bool IsWindows => OperatingSystem.IsWindows();

	// The default diff tool is code.cmd on Windows; this proves a .cmd file started by full path gets its arguments intact.
	[Fact(Skip = "Windows only", SkipUnless = nameof(IsWindows))]
	public void Starts_a_cmd_file_by_full_path_with_an_argument_containing_spaces()
	{
		using var temp = new TempDirectory();
		var script = temp.File("echoargs.cmd");
		File.WriteAllText(script, "@echo %~1>\"%~dp0out.txt\"\r\n");
		var argument = temp.File("has space.txt");

		using var process = new ProcessStarter().Start(script, [argument]);

		Assert.NotNull(process);
		Assert.True(process.WaitForExit(10_000));
		Assert.Equal(argument, File.ReadAllText(temp.File("out.txt")).Trim());
	}
}
