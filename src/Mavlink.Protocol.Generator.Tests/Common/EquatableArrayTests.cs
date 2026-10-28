using System.Collections.Immutable;
using AwesomeAssertions;
using Mavlink.Protocol.Generator.Common;

namespace Mavlink.Protocol.Generator.Tests.Common;

public sealed class EquatableArrayTests
{
	[Fact]
	public void Equals_SameElements_IsTrue()
	{
		var left = new List<string> { "a", "b" }.ToEquatableArray();
		var right = ImmutableArray.Create("a", "b").ToEquatableArray();

		(left == right).Should().BeTrue();
		left.GetHashCode().Should().Be(right.GetHashCode());
	}

	[Fact]
	public void Equals_DifferentOrder_IsFalse()
	{
		var left = ImmutableArray.Create("a", "b").ToEquatableArray();
		var right = ImmutableArray.Create("b", "a").ToEquatableArray();

		(left == right).Should().BeFalse();
	}

	[Fact]
	public void Default_EqualsEmpty()
	{
		var empty = default(EquatableArray<string>);

		empty.Should().Equal(EquatableArray<string>.Empty);
		(empty == EquatableArray<string>.Empty).Should().BeTrue();
		empty.Count.Should().Be(0);
		empty.AsImmutableArray().IsDefault.Should().BeFalse();
	}

	[Fact]
	public void Constructor_CopiesTheSource()
	{
		var source = new[] { "a" };
		var array = source.ToEquatableArray();

		source[0] = "changed";

		array[0].Should().Be("a");
	}
}
