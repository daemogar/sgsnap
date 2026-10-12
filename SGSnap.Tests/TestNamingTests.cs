namespace SGSnap.Tests;

public class TestNamingTests
{
	[Fact]
	public void Prefix_is_the_class_and_method_of_the_running_test()
		=> Assert.Equal("TestNamingTests.Prefix_is_the_class_and_method_of_the_running_test", TestNaming.CurrentPrefix());

	[Fact]
	public void Nested_class_separator_becomes_a_dot()
		=> Assert.Equal("Outer.Inner.Run", TestNaming.Prefix("Outer+Inner", "Run"));

	[Fact]
	public void Generic_test_classes_are_rejected()
		=> Assert.Throws<NotSupportedException>(() => TestNaming.Prefix("Widget`1", "Run"));

	public class Nested
	{
		[Fact]
		public void Reports_the_outer_and_inner_class()
			=> Assert.Equal("TestNamingTests.Nested.Reports_the_outer_and_inner_class", TestNaming.CurrentPrefix());
	}
}

public abstract class InheritedNamingBase
{
	[Fact]
	public void Reports_the_running_class_not_the_declaring_one()
		=> Assert.Equal("InheritedNamingTests.Reports_the_running_class_not_the_declaring_one", TestNaming.CurrentPrefix());
}

public class InheritedNamingTests : InheritedNamingBase
{
}
