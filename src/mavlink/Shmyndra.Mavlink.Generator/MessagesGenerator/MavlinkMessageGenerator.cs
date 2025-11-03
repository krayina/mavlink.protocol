using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;

namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageGenerator : IMavlinkMessageGenerator
{
	private const string SerializeWithoutExtensionsMethodName = "SerializeWithoutExtensions";
	private const string SerializeWithExtensionsMethodName = "SerializeWithExtensions";

	private const string DeserializeWithoutExtensionsMethodName = "DeserializeWithoutExtensions";
	private const string DeserializeWithExtensionsMethodName = "DeserializeWithExtensions";

	private static readonly Template _messageTemplate;

	private readonly MavlinkMessageFieldInitPropertyGenerator _propertyGenerator;
	private readonly MavlinkMessageDeserializationMethodGenerator _deserializationGenerator;
	private readonly MavlinkMessageSerializationMethodGenerator _serializationGenerator;

	private readonly Dictionary<(string Namespace, string MavlinkMessageName), GeneratedMavlinkMessage> _generatedMessages = new();

	static MavlinkMessageGenerator()
	{
		_messageTemplate = Template.Parse(Templates.MessageTemplate);
		if (_messageTemplate.HasErrors)
		{
			var errors = string.Join("\n", _messageTemplate.Messages.Select(m => m.Message));
			throw new InvalidOperationException($"Failed to parse Scriban template: \n{errors}");
		}
	}

	public MavlinkMessageGenerator(
		MavlinkMessageFieldInitPropertyGenerator propertyGenerator,
		MavlinkMessageDeserializationMethodGenerator deserializationGenerator,
		MavlinkMessageSerializationMethodGenerator serializationGenerator)
	{
		_propertyGenerator = propertyGenerator;
		_deserializationGenerator = deserializationGenerator;
		_serializationGenerator = serializationGenerator;
	}

	#region Explicit IGeneratedStorage Implementation

	ImmutableArray<GeneratedMavlinkMessage> IGeneratedStorage<GeneratedMavlinkMessage>.GetGeneratedTypes()
	{
		return _generatedMessages.Values.ToImmutableArray();
	}

	ImmutableArray<GeneratedMavlinkMessage> IGeneratedStorage<GeneratedMavlinkMessage>.GetGeneratedTypes(Func<GeneratedMavlinkMessage, bool>? predicate)
	{
		if (predicate == null)
		{
			return ((IGeneratedStorage<GeneratedMavlinkMessage>)this).GetGeneratedTypes();
		}

		return _generatedMessages.Values.Where(predicate).ToImmutableArray();
	}

	#endregion

	public GeneratedMavlinkMessage GenerateMavlinkMessage(
		 MavlinkMessage message,
		 string @namespace,
		 ImmutableArray<GeneratedMavlinkEnum>? generatedEnums)
	{
		ValidateAndCheckCache(message, @namespace);

		string normalizedName = Utilities.ToUpperCamelCase(message.Name) + MavlinkGeneratorConstants.MessagesPostfix;

		var enumsMap = generatedEnums.HasValue
			? generatedEnums.Value.ToImmutableDictionary(e => e.Original.Name, e => e)
			: ImmutableDictionary<string, GeneratedMavlinkEnum>.Empty;

		var generatedFields = GenerateMessageFields(message, @namespace, enumsMap);

		var deserializeMethods = CreateDeserializationMethods(@namespace, normalizedName, generatedFields);
		var serializeMethods = CreateSerializationMethods(@namespace, normalizedName, generatedFields);

		var model = CreateScribanModel(message, normalizedName, generatedFields, deserializeMethods, serializeMethods);
		string code = RenderTemplate(model);

		var recordDeclaration = ParseRecordDeclaration(code);

		var generatedMessage = new GeneratedMavlinkMessage(
			@namespace,
			normalizedName,
			generatedFields,
			recordDeclaration,
			message
		);

		_generatedMessages.Add((@namespace, message.Name), generatedMessage);
		return generatedMessage;
	}

	private ImmutableArray<GeneratedMavlinkMessageDeserializationMethod> CreateDeserializationMethods(
		string @namespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var generatedMethods = ImmutableArray.CreateBuilder<GeneratedMavlinkMessageDeserializationMethod>();

		var requiredFields = fields.Where(f => f.Original.IsRequired).ToImmutableArray();
		var withoutExtensionsMethod = _deserializationGenerator.Generate(
			DeserializeWithoutExtensionsMethodName,
			@namespace,
			messageName,
			requiredFields);
		generatedMethods.Add(withoutExtensionsMethod);

		bool hasExtensionFields = fields.Any(f => !f.Original.IsRequired);
		if (hasExtensionFields)
		{
			var withExtensionsMethod = _deserializationGenerator.Generate(
				DeserializeWithExtensionsMethodName,
				@namespace,
				messageName,
				fields);
			generatedMethods.Add(withExtensionsMethod);
		}

		return generatedMethods.ToImmutable();
	}

	private ImmutableArray<GeneratedMavlinkMessageSerializationMethod> CreateSerializationMethods(
		string @namespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fields)
	{
		var generatedMethods = ImmutableArray.CreateBuilder<GeneratedMavlinkMessageSerializationMethod>();

		var requiredFields = fields.Where(f => f.Original.IsRequired).ToImmutableArray();
		var withoutExtensionsMethod = _serializationGenerator.Generate(
			SerializeWithoutExtensionsMethodName,
			@namespace,
			messageName,
			requiredFields);
		generatedMethods.Add(withoutExtensionsMethod);

		bool hasExtensionFields = fields.Any(f => !f.Original.IsRequired);
		if (hasExtensionFields)
		{
			var withExtensionsMethod = _serializationGenerator.Generate(
				SerializeWithExtensionsMethodName,
				@namespace,
				messageName,
				fields);
			generatedMethods.Add(withExtensionsMethod);
		}

		return generatedMethods.ToImmutable();
	}

	private void ValidateAndCheckCache(MavlinkMessage message, string @namespace)
	{
		if (message == null)
		{
			throw new ArgumentNullException(nameof(message));
		}
		if (@namespace == null)
		{
			throw new ArgumentNullException(nameof(@namespace));
		}
		if (_generatedMessages.ContainsKey((@namespace, message.Name)))
		{
			throw new InvalidOperationException($"The message '{@namespace}.{message.Name}' has already been generated.");
		}
	}

	private ImmutableArray<GeneratedMavlinkMessageField> GenerateMessageFields(
		MavlinkMessage message,
		string @namespace,
		IReadOnlyDictionary<string, GeneratedMavlinkEnum> enumsMap)
	{
		return message.Fields
			.Select(field =>
			{
				if (field.Type is MavlinkMessageFieldEnumType enumType)
				{
					if (!enumsMap.TryGetValue(enumType.EnumName, out var generatedEnum))
					{
						throw new ArgumentException($"Required enum '{enumType.EnumName}' was not found for field '{field.Name}'.");
					}
					return _propertyGenerator.GenerateEnumProperty(field, generatedEnum, @namespace);
				}
				else
				{
					return _propertyGenerator.GeneratePrimitiveProperty(field);
				}
			})
			.ToImmutableArray();
	}

	private MavlinkMessageScribanMetadata CreateScribanModel(
		MavlinkMessage message,
		string normalizedName,
		ImmutableArray<GeneratedMavlinkMessageField> generatedFields,
		ImmutableArray<GeneratedMavlinkMessageDeserializationMethod> deserializeMethods,
		ImmutableArray<GeneratedMavlinkMessageSerializationMethod> serializeMethods)
	{
		string? summaryCommentBlock = string.IsNullOrEmpty(message.Description)
			? null
			: Utilities.CreateSummaryTrivia(message.Description!).ToFullString().TrimEnd();

		var propertiesDeclarations = generatedFields
			   .Select(f => f.DeclarationSyntax.ToFullString().Trim())
			   .ToList();

		var methodDeclarations = PrepareMethodDeclarations(deserializeMethods, serializeMethods);

		var allMembers = new List<string>();
		allMembers.AddRange(propertiesDeclarations);
		allMembers.AddRange(methodDeclarations);

		bool hasExtensions = serializeMethods.Length > 1;

		return new MavlinkMessageScribanMetadata(
			name: normalizedName,
			originalName: message.Name,
			id: message.Id,
			hasExtensions: hasExtensions,
			allMembers: allMembers
		)
		{
			SummaryCommentBlock = summaryCommentBlock,
			IsObsolete = message.Deprecated != null,
			ObsoleteMessage = message.Deprecated?.ToString()
		};
	}

	private List<string> PrepareMethodDeclarations(
		ImmutableArray<GeneratedMavlinkMessageDeserializationMethod> deserializeMethods,
		ImmutableArray<GeneratedMavlinkMessageSerializationMethod> serializeMethods)
	{
		var methods = new List<string>();

		var deserializeWithoutExt = deserializeMethods.FirstOrDefault(m => m.MethodSyntax.Identifier.Text == DeserializeWithoutExtensionsMethodName);
		var deserializeWithExt = deserializeMethods.FirstOrDefault(m => m.MethodSyntax.Identifier.Text == DeserializeWithExtensionsMethodName);

		var serializeWithoutExt = serializeMethods.FirstOrDefault(m => m.MethodSyntax.Identifier.Text == SerializeWithoutExtensionsMethodName);
		var serializeWithExt = serializeMethods.FirstOrDefault(m => m.MethodSyntax.Identifier.Text == SerializeWithExtensionsMethodName);

		AddMethod(deserializeWithoutExt?.MethodSyntax);
		AddMethod(serializeWithoutExt?.MethodSyntax);
		AddMethod(deserializeWithExt?.MethodSyntax);
		AddMethod(serializeWithExt?.MethodSyntax);

		void AddMethod(MethodDeclarationSyntax? methodSyntax)
		{
			if (methodSyntax != null)
			{
				methods.Add(methodSyntax.ToFullString().Trim());
			}
		}
		return methods;
	}

	private RecordDeclarationSyntax ParseRecordDeclaration(string code)
	{
		var syntaxTree = CSharpSyntaxTree.ParseText(code);
		var root = syntaxTree.GetRoot();
		var recordDeclaration = root.DescendantNodes().OfType<RecordDeclarationSyntax>().FirstOrDefault();

		if (recordDeclaration == null)
		{
			throw new InvalidOperationException("Failed to find a record declaration in the generated code.");
		}
		return recordDeclaration;
	}

	private string RenderTemplate(MavlinkMessageScribanMetadata model)
	{
		var context = CSharpScribanTemplateContext.Create();
		var modelScriptObject = new ScriptObject
		{
			{ "message", model }
		};
		context.PushGlobal(modelScriptObject);

		return _messageTemplate.Render(context);
	}
}
