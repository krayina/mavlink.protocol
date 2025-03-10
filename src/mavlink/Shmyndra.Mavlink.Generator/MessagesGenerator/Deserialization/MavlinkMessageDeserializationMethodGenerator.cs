using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Generates deserialization methods for Mavlink messages using a provided strategy.
/// </summary>
public class MavlinkMessageDeserializationMethodGenerator
{
	private readonly IMavlinkDeserializationGeneratorStrategy _strategy;
	private const string PayloadParameterName = "payload";
	private const string WithoutExtensionsMethodName = "DeserializeWithoutExtensions";
	private const string WithExtensionsMethodName = "DeserializeWithExtensions";

	public MavlinkMessageDeserializationMethodGenerator(IMavlinkDeserializationGeneratorStrategy strategy)
	{
		_strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
	}

	public GeneratedMavlinkMessageDeserializeMethod CreateDeserializeMethod(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var withoutExtensions = CreateDeserializeWithoutExtensionsMethodInternal(@namespace, messageName, fields);
		var withExtensions = fields.Any(f => !f.Original.IsRequired) ? CreateDeserializeWithExtensionsMethodInternal(@namespace, messageName, fields) : null;
		return new GeneratedMavlinkMessageDeserializeMethod(@namespace, messageName, fields, withoutExtensions, withExtensions);
	}

	internal MethodDeclarationSyntax CreateDeserializeWithoutExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var sb = new StringBuilder();
		int offset = 0;
		var (requiredFields, arrayFields) = fields.GetSortedFields();
		var fieldVariables = new Dictionary<GeneratedMavlinkMessageField, string>();

		_strategy.AppendBufferInitialization(sb, messageName, fields.CalculateMinSize(), PayloadParameterName);
		AppendFields(sb, requiredFields.Concat(arrayFields), ref offset, @namespace, fieldVariables);
		AppendReturn(sb, messageName, fields, fieldVariables);

		return WrapMethod(WithoutExtensionsMethodName, messageName, sb.ToString());
	}

	internal MethodDeclarationSyntax CreateDeserializeWithExtensionsMethodInternal(string @namespace, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var sb = new StringBuilder();
		int offset = 0;
		var (requiredFields, arrayFields) = fields.GetSortedFields();
		var fieldVariables = new Dictionary<GeneratedMavlinkMessageField, string>();

		_strategy.AppendBufferInitialization(sb, messageName, fields.CalculateFinalSize(), PayloadParameterName);
		AppendFields(sb, requiredFields.Concat(arrayFields), ref offset, @namespace, fieldVariables);
		AppendFields(sb, fields.Where(f => !f.Original.IsRequired && ShouldDeserializeField(f)), ref offset, @namespace, fieldVariables);
		AppendReturn(sb, messageName, fields, fieldVariables);

		return WrapMethod(WithExtensionsMethodName, messageName, sb.ToString());
	}

	private void AppendFields(StringBuilder sb, IEnumerable<GeneratedMavlinkMessageField> fields, ref int offset, string currentNamespace, IDictionary<GeneratedMavlinkMessageField, string> fieldVariables)
	{
		foreach (var field in fields)
		{
			var varName = _strategy.AppendFieldDeserialization(sb, field, ref offset, currentNamespace, PayloadParameterName);
			fieldVariables[field] = varName;
		}
	}

	private void AppendReturn(StringBuilder sb, string messageName, ImmutableArray<GeneratedMavlinkMessageField> fields, IDictionary<GeneratedMavlinkMessageField, string> fieldVariables)
	{
		_strategy.AppendReturnStatement(sb, messageName, fieldVariables);
	}

	private static bool ShouldDeserializeField(GeneratedMavlinkMessageField field)
	{
		return field?.GetFieldSize() > 0;
	}

	private static MethodDeclarationSyntax WrapMethod(string methodName, string returnType, string methodBody)
	{
		var methodString = $@"
public static {returnType} {methodName}(byte[] {PayloadParameterName})
{{
    {methodBody}
}}";
		var classWrapper = $"public class Temp {{ {methodString} }}";
		var syntaxTree = CSharpSyntaxTree.ParseText(classWrapper);
		return syntaxTree.GetRoot()
			.DescendantNodes()
			.OfType<MethodDeclarationSyntax>()
			.First(m => m.Identifier.Text == methodName);
	}
}
