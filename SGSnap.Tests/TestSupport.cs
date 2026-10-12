using System.Diagnostics;

namespace SGSnap.Tests;

/// <summary>A fresh temp folder per test, deleted afterwards.</summary>
sealed class TempDirectory : IDisposable
{
	public string Path { get; } = Directory.CreateTempSubdirectory("sgsnap").FullName;

	public string File(string name) => System.IO.Path.Combine(Path, name);

	public void Dispose() => Directory.Delete(Path, recursive: true);
}

/// <summary>Records process starts instead of launching anything.</summary>
sealed class RecordingStarter : IProcessStarter
{
	public List<(string File, IReadOnlyList<string> Args)> Calls { get; } = [];

	public Process? Start(string fileName, IReadOnlyList<string> arguments)
	{
		Calls.Add((fileName, arguments));
		return null;
	}
}
