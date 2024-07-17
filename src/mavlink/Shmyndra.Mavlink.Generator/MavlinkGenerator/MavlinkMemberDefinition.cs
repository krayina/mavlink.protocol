using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

internal sealed class MavlinkMemberDefinition
{
	private readonly IMavlinkEnumTypesGenerator _enumGenerator;
	private readonly IMavlinkMessageTypesGenerator _messageGenerator;
	private readonly IMavlinkSpecificationTypeGenerator _specificationGenerator;

	private readonly Dictionary<string, (string Namespace, string TypeName, string BaseType)> _generatedTypes = new();

	public MavlinkMemberDefinition(
		IMavlinkEnumTypesGenerator enumGenerator,
		IMavlinkMessageTypesGenerator messageGenerator,
		IMavlinkSpecificationTypeGenerator specificationGenerator)
	{
		_enumGenerator = enumGenerator;
		_messageGenerator = messageGenerator;
		_specificationGenerator = specificationGenerator;
	}

	public List<MemberDeclarationSyntax> GenerateNamespaceMembers(MavlinkData mavlinkData, string namespaceName, out List<MavlinkCachedMessage> messagesCache, string filePath)
	{
		var allGeneratedEnumTypes = GenerateEnums(mavlinkData.Enums, namespaceName, mavlinkData.Includes, filePath);

		foreach (var kvp in allGeneratedEnumTypes)
		{
			_generatedTypes[kvp.Key] = (kvp.Value.Namespace, kvp.Value.TypeName, kvp.Value.BaseType);
		}

		var allGeneratedMessageTypes = GenerateMessages(mavlinkData.Messages, namespaceName, _generatedTypes.ToImmutableSortedDictionary());

		var members = new List<MemberDeclarationSyntax>();
		members.AddRange(allGeneratedEnumTypes.Values.Select(enumType => enumType.Declaration));
		members.AddRange(allGeneratedMessageTypes.Values.Select(messageType => messageType.Declaration));

		var specificationClass = _specificationGenerator.GenerateSpecification(mavlinkData, namespaceName);
		members.Add(specificationClass);

		messagesCache = mavlinkData.Messages
			.Select(message => new MavlinkCachedMessage(
				$"{namespaceName}.{allGeneratedMessageTypes[message.Name].TypeName}",
				message.Id,
				message.Name.ToUpper()))
			.ToList();

		return members;
	}

	private Dictionary<string, (string Namespace, string TypeName, string BaseType, EnumDeclarationSyntax Declaration)> GenerateEnums(
		ImmutableArray<(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries)> enums,
		string namespaceName,
		ImmutableArray<string> includes,
		string filePath)
	{
		var enumDeclarations = _enumGenerator.GenerateEnums(enums, namespaceName, includes, out var generatedMavlinkEnumTypes, filePath);

		return generatedMavlinkEnumTypes.ToDictionary(
			kvp => kvp.Key,
			kvp =>
			{
				var enumDeclaration = enumDeclarations.First(e => e.Identifier.Text == kvp.Value.TypeName);
				var baseType = GetEnumBaseType(enumDeclaration);
				return (namespaceName, kvp.Value.TypeName, baseType, enumDeclaration);
			});
	}

	private Dictionary<string, (string Namespace, string TypeName, RecordDeclarationSyntax Declaration)> GenerateMessages(
		ImmutableArray<(uint Id, string Name, string? Description, ImmutableList<(FieldType Type, string Name, string? Description)> Fields)> messages,
		string namespaceName,
		IImmutableDictionary<string, (string Namespace, string TypeName, string BaseType)> allGeneratedEnumTypes)
	{
		var messageDeclarations = _messageGenerator.GenerateMessages(messages, namespaceName, allGeneratedEnumTypes, out var generatedMavlinkMessageTypes);
		return generatedMavlinkMessageTypes.ToDictionary(kvp => kvp.Key, kvp => (namespaceName, kvp.Value.TypeName, messageDeclarations.First(m => m.Identifier.Text == kvp.Value.TypeName)));
	}

	private string GetEnumBaseType(EnumDeclarationSyntax enumDeclaration)
	{
		if (enumDeclaration.BaseList?.Types.FirstOrDefault() is SimpleBaseTypeSyntax baseType)
		{
			return baseType.Type.ToString();
		}

		return "int"; // default base type
	}
}
