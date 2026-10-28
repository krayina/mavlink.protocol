namespace Mavlink.Protocol.Generator;

public class MavlinkBitmaskFieldTypeNameResolutionStrategy : IMavlinkMessageFieldTypeNameResolutionStrategy
{
	private readonly MavlinkNonBitmaskFieldTypeNameResolutionStrategy _nonBitmaskFieldTypeNameResolutionStrategy;

	public MavlinkBitmaskFieldTypeNameResolutionStrategy()
	{
		_nonBitmaskFieldTypeNameResolutionStrategy = new MavlinkNonBitmaskFieldTypeNameResolutionStrategy();
	}

	public string ResolvePrimitive(MavlinkMessageField field)
	{
		return _nonBitmaskFieldTypeNameResolutionStrategy.ResolvePrimitive(field);
	}

	public string ResolveEnum(MavlinkMessageField field, GeneratedMavlinkEnum generatedEnum, string currentNamespace)
	{
		return _nonBitmaskFieldTypeNameResolutionStrategy.ResolveEnum(field, generatedEnum, currentNamespace);
	}
}
