namespace SGSnap.Tests;

// One class, so the test that sets SnapshotDefaults never runs in parallel with the defaults test.
public class SnapshotEnvironmentTests
{
	static SnapshotEnvironment From(params (string Name, string Value)[] variables)
		=> SnapshotEnvironment.From(name => variables.FirstOrDefault(variable => variable.Name == name).Value);

	[Fact]
	public void Nothing_set_means_local_defaults()
		=> Assert.Equal(new SnapshotEnvironment(Accept: false, CheckOnly: false, IsCI: false, DiffTool: null, MaxLaunch: 5), From());

	[Fact]
	public void Accept_and_check_only_take_1_or_true()
	{
		var environment = From(("SGSNAP_ACCEPT", "1"), ("SGSNAP_CHECKONLY", "TRUE"));

		Assert.True(environment.Accept);
		Assert.True(environment.CheckOnly);
	}

	[Fact]
	public void Other_values_leave_modes_off()
		=> Assert.False(From(("SGSNAP_ACCEPT", "yes")).Accept);

	[Fact]
	public void CI_true_is_detected()
		=> Assert.True(From(("CI", "true")).IsCI);

	[Fact]
	public void TF_BUILD_is_detected()
		=> Assert.True(From(("TF_BUILD", "True")).IsCI);

	[Fact]
	public void Environment_diff_tool_overrides_the_code_level_default()
	{
		SnapshotDefaults.DiffTool = "tool-a {received} {verified}";
		try
		{
			Assert.Equal("tool-b {received} {verified}", From(("SGSNAP_DIFFTOOL", "tool-b {received} {verified}")).DiffTool);
			Assert.Equal("tool-a {received} {verified}", From().DiffTool);
		}
		finally
		{
			SnapshotDefaults.DiffTool = null;
		}
	}

	[Fact]
	public void Environment_root_length_overrides_the_code_level_default()
	{
		SnapshotDefaults.CheckoutRootLength = 30;
		try
		{
			Assert.Equal(12, From(("SGSNAP_ROOTLENGTH", "12")).RootLength);
			Assert.Equal(30, From(("SGSNAP_ROOTLENGTH", "-1")).RootLength);
			Assert.Equal(30, From().RootLength);
		}
		finally
		{
			SnapshotDefaults.CheckoutRootLength = null;
		}
	}

	[Fact]
	public void Max_launch_parses_or_falls_back_to_5()
	{
		Assert.Equal(5, From(("SGSNAP_MAXLAUNCH", "many")).MaxLaunch);
		Assert.Equal(0, From(("SGSNAP_MAXLAUNCH", "0")).MaxLaunch);
	}
}
