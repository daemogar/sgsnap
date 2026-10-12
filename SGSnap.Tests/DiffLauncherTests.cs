namespace SGSnap.Tests;

public class DiffLauncherTests
{
	/// <summary>A launcher whose PATH holds an empty "code" (and "code.cmd") file.</summary>
	static (DiffLauncher Launcher, RecordingStarter Starter, string ToolPath) WithFakeCode(TempDirectory temp)
	{
		File.WriteAllText(temp.File("code"), "");
		File.WriteAllText(temp.File("code.cmd"), "");
		var starter = new RecordingStarter();
		var launcher = new DiffLauncher(starter, name => name switch
		{
			"PATH" => temp.Path,
			"PATHEXT" => ".CMD",
			_ => null,
		});
		return (launcher, starter, OperatingSystem.IsWindows() ? temp.File("code.CMD") : temp.File("code"));
	}

	[Fact]
	public void Tokenize_splits_on_spaces_outside_quotes()
		=> Assert.Equal(new[] { "code", "--diff", "{received}", "{verified}" }, DiffLauncher.Tokenize(DiffLauncher.DefaultTool));

	[Fact]
	public void Tokenize_keeps_a_quoted_path_with_spaces_as_one_token()
		=> Assert.Equal(new[] { @"C:\Program Files\Tool\tool.exe", "-x" }, DiffLauncher.Tokenize("\"C:\\Program Files\\Tool\\tool.exe\" -x"));

	[Fact]
	public void Default_tool_diffs_received_against_verified()
	{
		using var temp = new TempDirectory();
		var (launcher, starter, toolPath) = WithFakeCode(temp);

		launcher.Launch(null, "r.txt", "v.txt", verifiedExists: true, maxLaunch: 5);

		var call = Assert.Single(starter.Calls);
		Assert.Equal(toolPath, call.File);
		Assert.Equal(new[] { "--diff", "r.txt", "v.txt" }, call.Args);
	}

	[Fact]
	public void Default_tool_opens_only_the_received_file_when_nothing_is_verified_yet()
	{
		using var temp = new TempDirectory();
		var (launcher, starter, _) = WithFakeCode(temp);

		launcher.Launch(null, "r.txt", "v.txt", verifiedExists: false, maxLaunch: 5);

		Assert.Equal(new[] { "r.txt" }, Assert.Single(starter.Calls).Args);
	}

	[Fact]
	public void Custom_tool_is_not_launched_when_nothing_is_verified_yet()
	{
		using var temp = new TempDirectory();
		var (launcher, starter, toolPath) = WithFakeCode(temp);

		launcher.Launch($"\"{toolPath}\" \"{{received}}\" \"{{verified}}\"", "r.txt", "v.txt", verifiedExists: false, maxLaunch: 5);

		Assert.Empty(starter.Calls);
	}

	[Fact]
	public void None_disables_launching()
	{
		using var temp = new TempDirectory();
		var (launcher, starter, _) = WithFakeCode(temp);

		launcher.Launch("none", "r.txt", "v.txt", verifiedExists: true, maxLaunch: 5);

		Assert.Empty(starter.Calls);
	}

	[Fact]
	public void Launches_stop_at_the_cap()
	{
		using var temp = new TempDirectory();
		var (launcher, starter, _) = WithFakeCode(temp);

		for (var i = 0; i < 3; i++)
			launcher.Launch(null, "r.txt", "v.txt", verifiedExists: true, maxLaunch: 2);

		Assert.Equal(2, starter.Calls.Count);
	}

	[Fact]
	public void A_tool_that_is_not_installed_is_skipped_quietly()
	{
		using var temp = new TempDirectory();
		var starter = new RecordingStarter();
		var launcher = new DiffLauncher(starter, name => name == "PATH" ? temp.Path : null);

		launcher.Launch(null, "r.txt", "v.txt", verifiedExists: true, maxLaunch: 5);

		Assert.Empty(starter.Calls);
	}
}
