using Xunit;

namespace SGSnap;

/// <summary>The running xUnit v3 test's class and method, and the snapshot file prefix <c>Class.Method</c> built from them.</summary>
internal static class TestNaming
{
	public static (string Class, string Method) Current()
	{
		var context = TestContext.Current;
		if (context.TestClass is not { } testClass || context.TestMethod is not { } testMethod)
			throw new InvalidOperationException("Snapshot.Match must be called from inside a running xUnit v3 test.");

		return (ClassName(testClass.TestClassSimpleName), testMethod.MethodName);
	}

	public static string CurrentPrefix()
	{
		var (className, methodName) = Current();
		return $"{className}.{methodName}";
	}

	/// <summary>Joins class and method; a nested class's <c>+</c> separator becomes <c>.</c>.</summary>
	public static string Prefix(string classSimpleName, string methodName) => $"{ClassName(classSimpleName)}.{methodName}";

	static string ClassName(string classSimpleName)
	{
		if (classSimpleName.Contains('`'))
			throw new NotSupportedException($"Generic test classes are not supported: {classSimpleName}.");

		return classSimpleName.Replace('+', '.');
	}
}
