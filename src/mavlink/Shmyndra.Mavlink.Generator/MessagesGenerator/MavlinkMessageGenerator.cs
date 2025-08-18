using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;

namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageGenerator : IMavlinkMessageGenerator
{
	private static readonly Template _messageTemplate;

	private readonly IMavlinkMessageFieldPropertyGenerator _propertyGenerator;
	private readonly MavlinkMessageDeserializationMethodGenerator _deserializerGenerator;
	private readonly MavlinkMessageSerializationMethodGenerator _serializerGenerator;

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
		IMavlinkMessageFieldPropertyGenerator propertyGenerator,
		MavlinkMessageDeserializationMethodGenerator deserializerGenerator,
		MavlinkMessageSerializationMethodGenerator serializerGenerator)
	{
		_propertyGenerator = propertyGenerator;
		_deserializerGenerator = deserializerGenerator;
		_serializerGenerator = serializerGenerator;
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

		var deserializeMethods = _deserializerGenerator.CreateDeserializeMethod(@namespace, normalizedName, generatedFields);
		var serializeMethods = _serializerGenerator.CreateSerializeMethod(generatedFields);

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
		GeneratedMavlinkMessageDeserializeMethod deserializeMethods,
		GeneratedMavlinkMessageSerializeMethod serializeMethods)
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

		return new MavlinkMessageScribanMetadata(
			name: normalizedName,
			originalName: message.Name,
			id: message.Id,
			hasExtensions: serializeMethods.SerializeWithExtensionsMethod != null,
			allMembers: allMembers
		)
		{
			SummaryCommentBlock = summaryCommentBlock,
			IsObsolete = message.Deprecated != null,
			ObsoleteMessage = message.Deprecated?.ToString()
		};
	}

	private List<string> PrepareMethodDeclarations(
		GeneratedMavlinkMessageDeserializeMethod deserializeMethods,
		GeneratedMavlinkMessageSerializeMethod serializeMethods)
	{
		var methods = new List<string>();

		AddMethod(deserializeMethods.DeserializeWithoutExtensionsMethod);
		AddMethod(serializeMethods.SerializeWithoutExtensionsMethod);
		AddMethod(deserializeMethods.DeserializeWithExtensionsMethod);
		AddMethod(serializeMethods.SerializeWithExtensionsMethod);

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
