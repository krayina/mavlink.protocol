using FluentAssertions;

namespace Shmyndra.Mavlink.Generator.Tests.Unit.Deserializtion;

public class MavlinkMessageFieldValidationCompilationFactoryTests
{
	[Fact]
	public void CreateOperation_NoValidationDefinition_ReturnsNoValidationCompiler()
	{
		// Arrange
		var def = new GeneratedMavlinkMessageNoValidationRuleDefinition();

		// Act
		var op = MavlinkMessageFieldValidationCompilationFactory.CreateOperation(def);

		// Assert
		op.Should().BeOfType<MavlinkMessageFieldNoValidationRuleCompiler>();
	}

	[Fact]
	public void CreateOperation_WholeFieldDefinition_ReturnsWholeFieldCompiler()
	{
		// Arrange
		var def = new GeneratedMavlinkMessageWholeFieldValidationRuleDefinition("123");

		// Act
		var op = MavlinkMessageFieldValidationCompilationFactory.CreateOperation(def);

		// Assert
		op.Should().BeOfType<MavlinkMessageFieldWholeFieldRuleCompiler>();
	}

	[Fact]
	public void CreateOperation_PerElementDefinition_ReturnsPerElementCompiler()
	{
		// Arrange
		var def = new GeneratedMavlinkMessagePerElementValidationRuleDefinition("NaN");

		// Act
		var op = MavlinkMessageFieldValidationCompilationFactory.CreateOperation(def);

		// Assert
		op.Should().BeOfType<MavlinkMessageFieldPerElementRuleCompiler>();
	}

	[Fact]
	public void CreateOperation_PerIndexDefinition_ReturnsPerIndexCompiler()
	{
		// Arrange
		var def = new GeneratedMavlinkMessagePerIndexValidationRuleDefinition([null, "65535", null, "0"]);

		// Act
		var op = MavlinkMessageFieldValidationCompilationFactory.CreateOperation(def);

		// Assert
		op.Should().BeOfType<MavlinkMessageFieldPerIndexRuleCompiler>();
	}

	private sealed record UnknownRuleDefinition() : GeneratedMavlinkMessageFieldValidationRuleDefinition;

	[Fact]
	public void CreateOperation_UnknownDefinition_ThrowsNotSupportedException()
	{
		// Arrange
		var def = new UnknownRuleDefinition();

		// Act
		Action act = () => MavlinkMessageFieldValidationCompilationFactory.CreateOperation(def);

		// Assert
		act.Should().Throw<NotSupportedException>();
	}
}
