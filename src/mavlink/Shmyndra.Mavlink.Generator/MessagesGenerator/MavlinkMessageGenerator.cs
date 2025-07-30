using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageGenerator : IMavlinkMessageGenerator
{
	private static readonly Template _messageTemplate;

	private readonly IMavlinkMessageFieldTypeNameResolutionStrategy _typeNameResolver;
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
		IMavlinkMessageFieldTypeNameResolutionStrategy typeNameResolver,
		MavlinkMessageDeserializationMethodGenerator deserializerGenerator,
		MavlinkMessageSerializationMethodGenerator serializerGenerator)
	{
		_typeNameResolver = typeNameResolver;
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
		if (message == null)
		{
			throw new ArgumentNullException(nameof(message));
		}

		if (@namespace == null)
		{
			throw new ArgumentNullException(nameof(@namespace));
		}

		var enumsMap = generatedEnums.HasValue
			? generatedEnums.Value.ToImmutableDictionary(e => e.Original.Name, e => e)
			: ImmutableDictionary<string, GeneratedMavlinkEnum>.Empty;

		if (_generatedMessages.ContainsKey((@namespace, message.Name)))
		{
			throw new InvalidOperationException($"The message '{@namespace}.{message.Name}' has already been generated.");
		}

		string normalizedName = Utilities.ToUpperCamelCase(message.Name) + MavlinkGeneratorConstants.MessagesPostfix;

		var generatedFields = message.Fields
			.Select(field => CreateGeneratedField(field, @namespace, enumsMap))
			.ToImmutableArray();

		var deserializeMethods = _deserializerGenerator.CreateDeserializeMethod(@namespace, normalizedName, generatedFields);
		var serializeMethods = _serializerGenerator.CreateSerializeMethod(generatedFields);

		var model = CreateScribanModel(message, normalizedName, generatedFields, deserializeMethods, serializeMethods);

		string code = RenderTemplate(model);

		var recordDeclaration = (RecordDeclarationSyntax)CSharpSyntaxTree
			.ParseText(code)
			.GetRoot()
			.DescendantNodes()
			.First(s => s.IsKind(SyntaxKind.RecordStructDeclaration));

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

	private GeneratedMavlinkMessageField CreateGeneratedField(
		MavlinkMessageField field,
		string currentNamespace,
		IReadOnlyDictionary<string, GeneratedMavlinkEnum> enumsMap)
	{
		string normalizedFieldName = Utilities.ToUpperCamelCase(field.Name);

		string innerTypeName;
		GeneratedMavlinkMessageFieldTypeBase generatedTypeInfo;

		if (field.Type is MavlinkMessageFieldEnumType enumType)
		{
			if (!enumsMap.TryGetValue(enumType.EnumName, out var generatedEnum))
			{
				throw new ArgumentException($"Required enum '{enumType.EnumName}' was not found for field '{field.Name}'.");
			}
			innerTypeName = _typeNameResolver.ResolveEnum(field, generatedEnum, currentNamespace);
			generatedTypeInfo = CreateGeneratedEnumTypeInfo(field, generatedEnum);
		}
		else
		{
			innerTypeName = _typeNameResolver.ResolvePrimitive(field);
			generatedTypeInfo = CreateGeneratedPrimitiveTypeInfo(field);
		}

		string propertyTypeName = DeterminePropertyTypeName(field, innerTypeName);

		var propertySyntax = CreatePropertyDeclaration(propertyTypeName, normalizedFieldName);

		if (field.Type.TypeName.Contains("["))
		{
			var arrayLength = int.Parse(field.Type.TypeName.Split('[', ']')[1]);
			propertySyntax = propertySyntax.AddArrayLengthAttribute(arrayLength);
		}

		propertySyntax = propertySyntax
			.AddSummaryTriviaIfNotNull(field.Description)
			.AddRemarksTriviaIfNotNullOrEmpty($"Original name: {field.Name}");

		return new GeneratedMavlinkMessageField(
			generatedName: normalizedFieldName,
			generatedFieldType: generatedTypeInfo,
			declarationSyntax: propertySyntax,
			original: field
		);
	}

	private string DeterminePropertyTypeName(MavlinkMessageField field, string innerTypeName)
	{
		bool isArray = field.Type.TypeName.Contains("[");

		if (isArray)
		{
			bool areElementsNullable = field.Invalid != null;
			string elementTypeName = areElementsNullable ? $"{innerTypeName}?" : innerTypeName;
			string arrayTypeName = $"System.Collections.Immutable.ImmutableArray<{elementTypeName}>";

			bool isArrayNullable = !field.IsRequired;

			return isArrayNullable ? $"{arrayTypeName}?" : arrayTypeName;
		}
		else
		{
			bool isScalarNullable = !field.IsRequired || field.Invalid != null;

			return isScalarNullable ? $"{innerTypeName}?" : innerTypeName;
		}
	}

	private static GeneratedMavlinkMessageFieldTypeBase CreateGeneratedPrimitiveTypeInfo(MavlinkMessageField field)
	{
		bool isArray = field.Type.TypeName.Contains("[");
		var mavlinkBaseTypeName = field.Type.GetTypeWithoutArray();
		var csharpTypeName = Utilities.MavlinkTypeMap[mavlinkBaseTypeName].TypeName;

		if (isArray)
		{
			var arrayLength = int.Parse(field.Type.TypeName.Split('[', ']')[1]);
			return new GeneratedMavlinkMessageFieldArrayType(csharpTypeName, arrayLength, field.Type);
		}
		return new GeneratedMavlinkMessageFieldPrimitiveType(csharpTypeName, field.Type);
	}

	private static GeneratedMavlinkMessageFieldTypeBase CreateGeneratedEnumTypeInfo(MavlinkMessageField field, GeneratedMavlinkEnum generatedEnum)
	{
		var enumType = (MavlinkMessageFieldEnumType)field.Type;
		bool isArray = enumType.TypeName.Contains("[");
		var mavlinkBaseTypeName = enumType.GetTypeWithoutArray();
		var csharpTypeName = Utilities.MavlinkTypeMap[mavlinkBaseTypeName].TypeName;

		if (isArray)
		{
			var arrayLength = int.Parse(enumType.TypeName.Split('[', ']')[1]);
			return new GeneratedMavlinkMessageFieldArrayEnumType(csharpTypeName, generatedEnum, arrayLength, enumType);
		}
		return new GeneratedMavlinkMessageFieldEnumType(csharpTypeName, generatedEnum, enumType);
	}

	private static PropertyDeclarationSyntax CreatePropertyDeclaration(string propertyType, string fieldName)
	{
		var typeSyntax = SyntaxFactory.ParseTypeName(propertyType);

		return SyntaxFactory.PropertyDeclaration(typeSyntax, SyntaxFactory.Identifier(fieldName))
			.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
			.AddAccessorListAccessors(
				SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
				SyntaxFactory.AccessorDeclaration(SyntaxKind.InitAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
			);
	}

	private MavlinkMessageScribanMetadata CreateScribanModel(
		MavlinkMessage message,
		string normalizedName,
		ImmutableArray<GeneratedMavlinkMessageField> generatedFields,
		GeneratedMavlinkMessageDeserializeMethod deserializeMethods,
		GeneratedMavlinkMessageSerializeMethod serializeMethods)
	{
		var properties = generatedFields.Select(f =>
		{
			var declarationString = f.DeclarationSyntax
									 .WithoutTrivia()
									 .NormalizeWhitespace()
									 .ToFullString();

			var summaryText = f.Original.Description;
			var remarksText = $"Original name: {f.Original.Name}";

			return new MavlinkMessagePropertyScribanMetadata(
				declaration: declarationString,
				summary: summaryText,
				remarks: remarksText
			);
		}).ToList();

		var methods = new List<string>
		{
			deserializeMethods.DeserializeWithoutExtensionsMethod.ToFullString().Trim(),
			serializeMethods.SerializeWithoutExtensionsMethod.ToFullString().Trim()
		};

		if (deserializeMethods.DeserializeWithExtensionsMethod != null)
		{
			methods.Add(deserializeMethods.DeserializeWithExtensionsMethod.ToFullString().Trim());
		}
		if (serializeMethods.SerializeWithExtensionsMethod != null)
		{
			methods.Add(serializeMethods.SerializeWithExtensionsMethod.ToFullString().Trim());
		}

		var model = new MavlinkMessageScribanMetadata(
			name: normalizedName,
			originalName: message.Name,
			id: message.Id,
			hasExtensions: serializeMethods.SerializeWithExtensionsMethod != null,
			properties: properties,
			methods: methods
		)
		{
			Summary = message.Description,
			IsObsolete = message.Deprecated != null,
			ObsoleteMessage = message.Deprecated?.ToString()
		};

		return model;
	}

	private string RenderTemplate(MavlinkMessageScribanMetadata model)
	{
		var context = CSharpScribanTemplateContext.Create();
		var generatedCode = _messageTemplate.Render(model);
		return CSharpSyntaxTree
			.ParseText(generatedCode, options: new CSharpParseOptions(LanguageVersion.Latest))
			.GetRoot()
			.NormalizeWhitespace()
			.ToFullString();
	}
}
