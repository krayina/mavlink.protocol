using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

internal sealed class MavlinkMemberDefinition
{
	private readonly IMavlinkEnumTypesGenerator _enumGenerator;
	private readonly IMavlinkMessageTypesGenerator _messageGenerator;
	private readonly IMavlinkSpecificationTypeGenerator _specificationGenerator;

	private readonly Dictionary<string, (string Namespace, string TypeName)> _generatedTypes = new();

	public MavlinkMemberDefinition(
		IMavlinkEnumTypesGenerator enumGenerator,
		IMavlinkMessageTypesGenerator messageGenerator,
		IMavlinkSpecificationTypeGenerator specificationGenerator)
	{
		_enumGenerator = enumGenerator;
		_messageGenerator = messageGenerator;
		_specificationGenerator = specificationGenerator;
	}

	public List<MemberDeclarationSyntax> GenerateNamespaceMembers(MavlinkData mavlinkData, string namespaceName)
	{
		var allGeneratedEnumTypes = GenerateEnums(mavlinkData.Enums, namespaceName);

		foreach (var kvp in allGeneratedEnumTypes)
		{
			_generatedTypes[kvp.Key] = (kvp.Value.Namespace, kvp.Value.TypeName);
		}

		var allGeneratedMessageTypes = GenerateMessages(mavlinkData.Messages, namespaceName, _generatedTypes.ToImmutableSortedDictionary());

		var members = new List<MemberDeclarationSyntax>();
		members.AddRange(allGeneratedEnumTypes.Values.Select(enumType => enumType.Declaration));
		members.AddRange(allGeneratedMessageTypes.Values.Select(messageType => messageType.Declaration));

		var specificationClass = _specificationGenerator.GenerateSpecification(mavlinkData, namespaceName);
		members.Add(specificationClass);

		return members;
	}

	private Dictionary<string, (string Namespace, string TypeName, EnumDeclarationSyntax Declaration)> GenerateEnums(
		ImmutableArray<(string Name, string? Description, ImmutableList<(string Name, string Value, string? Description)> Entries)> enums,
		string namespaceName)
	{
		var enumDeclarations = _enumGenerator.GenerateEnums(enums, namespaceName, out var generatedMavlinkEnumTypes);
		return generatedMavlinkEnumTypes.ToDictionary(kvp => kvp.Key, kvp => (namespaceName, kvp.Value.TypeName, enumDeclarations.First(e => e.Identifier.Text == kvp.Value.TypeName)));
	}

	private Dictionary<string, (string Namespace, string TypeName, RecordDeclarationSyntax Declaration)> GenerateMessages(
		ImmutableArray<(string Name, string? Description, ImmutableList<(FieldType Type, string Name, string? Description)> Fields)> messages,
		string namespaceName,
		IImmutableDictionary<string, (string Namespace, string TypeName)> allGeneratedEnumTypes)
	{
		var messageDeclarations = _messageGenerator.GenerateMessages(messages, namespaceName, allGeneratedEnumTypes, out var generatedMavlinkMessageTypes);
		return generatedMavlinkMessageTypes.ToDictionary(kvp => kvp.Key, kvp => (namespaceName, kvp.Value.TypeName, messageDeclarations.First(m => m.Identifier.Text == kvp.Value.TypeName)));
	}
}

