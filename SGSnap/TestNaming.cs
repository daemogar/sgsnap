using Xunit;

namespace SGSnap;

/// <summary>The snapshot file prefix, <c>Class.Method</c>, taken from the running xUnit v3 test.</summary>
internal static class TestNaming
{
	public static string CurrentPrefix()
	{
		var context = TestContext.Current;
		if (context.TestClass is not { } testClass || context.TestMethod is not { } testMethod)
			throw new InvalidOperationException("Snapshot.Match must be called from inside a running xUnit v3 test.");

		return Prefix(testClass.TestClassSimpleName, testMethod.MethodName);
	}

	/// <summary>Joins class and method; a nested class's <c>+</c> separator becomes <c>.</c>.</summary>
	public static string Prefix(string classSimpleName, string methodName)
	{
		if (classSimpleName.Contains('`'))
			throw new NotSupportedException($"Generic test classes are not supported: {classSimpleName}.");

		return $"{classSimpleName.Replace('+', '.')}.{methodName}";
	}
}
