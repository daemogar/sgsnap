using System.Diagnostics;

namespace SGSnap;

/// <summary>Starts a process without waiting for it.</summary>
internal interface IProcessStarter
{
	Process? Start(string fileName, IReadOnlyList<string> arguments);
}

internal sealed class ProcessStarter : IProcessStarter
{
	public Process? Start(string fileName, IReadOnlyList<string> arguments)
	{
		var info = new ProcessStartInfo(fileName) { UseShellExecute = false, CreateNoWindow = true };
		foreach (var argument in arguments)
			info.ArgumentList.Add(argument);
		return Process.Start(info);
	}
}
