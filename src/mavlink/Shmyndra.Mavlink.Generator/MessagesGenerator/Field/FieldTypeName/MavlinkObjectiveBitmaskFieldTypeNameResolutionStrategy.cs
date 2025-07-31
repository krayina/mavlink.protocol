namespace Shmyndra.Mavlink.Generator;

public class MavlinkObjectiveBitmaskFieldTypeNameResolutionStrategy : IMavlinkMessageFieldTypeNameResolutionStrategy
{
	public string ResolvePrimitive(MavlinkMessageField field)
	{
		string mavlinkType = field.Type.GetTypeWithoutArray();
		return Utilities.GetPrimitiveBitmaskType(Utilities.MavlinkTypeMap[mavlinkType].TypeName);
	}

	public string ResolveEnum(MavlinkMessageField field, GeneratedMavlinkEnum generatedEnum, string currentNamespace)
	{
		string baseEnumName = generatedEnum.GetQualifiedName(currentNamespace);
		string mavlinkBaseType = field.Type.GetTypeWithoutArray();

		string underlyingMavlinkType = Utilities.MavlinkTypeMap[mavlinkBaseType].TypeName;
		string csharpUnderlyingType = Utilities.ToUpperCamelCase(underlyingMavlinkType);

		return $"{baseEnumName}{csharpUnderlyingType}Bitmask";
	}
}
