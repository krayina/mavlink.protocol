namespace Mavlink.Protocol.Generator;

/// <summary>
/// Resolves field type names by dispatching to specialized strategies based on whether the field is a bitmask.
/// This class acts as a facade over the specific resolution strategies.
/// </summary>
public class MavlinkFieldTypeNameResolverFacade : IMavlinkMessageFieldTypeNameResolutionStrategy
{
	private readonly IMavlinkMessageFieldTypeNameResolutionStrategy _bitmaskStrategy;
	private readonly IMavlinkMessageFieldTypeNameResolutionStrategy _nonBitmaskStrategy;

	/// <summary>
	/// Creates a new facade instance with specific strategies for bitmask and non-bitmask fields.
	/// </summary>
	/// <param name="bitmaskStrategy">The strategy to use for fields marked as bitmasks.</param>
	/// <param name="nonBitmaskStrategy">The strategy to use for all other fields.</param>
	public MavlinkFieldTypeNameResolverFacade(
		IMavlinkMessageFieldTypeNameResolutionStrategy bitmaskStrategy,
		IMavlinkMessageFieldTypeNameResolutionStrategy nonBitmaskStrategy)
	{
		_bitmaskStrategy = bitmaskStrategy;
		_nonBitmaskStrategy = nonBitmaskStrategy;
	}

	public string ResolvePrimitive(MavlinkMessageField field)
	{
		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			return _bitmaskStrategy.ResolvePrimitive(field);
		}
		else
		{
			return _nonBitmaskStrategy.ResolvePrimitive(field);
		}
	}

	public string ResolveEnum(MavlinkMessageField field, GeneratedMavlinkEnum generatedEnum, string currentNamespace)
	{
		if (field.Display == MavlinkMessageFieldDisplay.Bitmask)
		{
			return _bitmaskStrategy.ResolveEnum(field, generatedEnum, currentNamespace);
		}
		else
		{
			return _nonBitmaskStrategy.ResolveEnum(field, generatedEnum, currentNamespace);
		}
	}
}
