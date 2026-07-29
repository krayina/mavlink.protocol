namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Resolves type names for fields that are NOT bitmasks, using standard types.
/// </summary>
public class MavlinkNonBitmaskFieldTypeNameResolutionStrategy : IMavlinkMessageFieldTypeNameResolutionStrategy
{
	public string ResolvePrimitive(MavlinkMessageField field)
	{
		string mavlinkBaseType = field.Type.GetTypeWithoutArray();
		return Utilities.MavlinkTypeMap[mavlinkBaseType].TypeName;
	}

	public string ResolveEnum(MavlinkMessageField field, GeneratedMavlinkEnum generatedEnum, string currentNamespace)
	{
		return generatedEnum.GetQualifiedName(currentNamespace);
	}
}
