using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageFieldInitPropertyGenerator : IMavlinkMessageFieldPropertyGenerator
{
	private static readonly Template _propertyTemplate;
	private readonly IMavlinkMessageFieldTypeNameResolutionStrategy _typeNameResolver;
	private readonly IMavlinkMessageFieldValidationRuleProvider _validationRuleProvider;

	static MavlinkMessageFieldInitPropertyGenerator()
	{
		_propertyTemplate = Template.Parse(Templates.PropertyTemplate);
		if (_propertyTemplate.HasErrors)
		{
			var errors = string.Join("\n", _propertyTemplate.Messages.Select(m => m.Message));
			throw new InvalidOperationException($"Failed to parse Property Scriban template: \n{errors}");
		}
	}

	public MavlinkMessageFieldInitPropertyGenerator(
		IMavlinkMessageFieldTypeNameResolutionStrategy typeNameResolver,
		IMavlinkMessageFieldValidationRuleProvider validationRuleProvider)
	{
		_typeNameResolver = typeNameResolver ?? throw new ArgumentNullException(nameof(typeNameResolver));
		_validationRuleProvider = validationRuleProvider ?? throw new ArgumentNullException(nameof(validationRuleProvider));
	}

	public GeneratedMavlinkMessageField GeneratePrimitiveProperty(MavlinkMessageField field)
	{
		if (field == null)
		{
			throw new ArgumentNullException(nameof(field));
		}

		var resolvedTypeName = _typeNameResolver.ResolvePrimitive(field);
		var typeInfo = CreateGeneratedPrimitiveTypeInfo(field);
		return Generate(field, resolvedTypeName, typeInfo);
	}

	public GeneratedMavlinkMessageField GenerateEnumProperty(
		MavlinkMessageField field,
		GeneratedMavlinkEnum generatedEnum,
		string fieldOwnerTypeNamespace)
	{
		if (field == null)
		{
			throw new ArgumentNullException(nameof(field));
		}
		if (fieldOwnerTypeNamespace == null)
		{
			throw new ArgumentNullException(nameof(fieldOwnerTypeNamespace));
		}
		if (generatedEnum == null)
		{
			throw new ArgumentNullException(nameof(generatedEnum));
		}

		var resolvedTypeName = _typeNameResolver.ResolveEnum(field, generatedEnum, fieldOwnerTypeNamespace);
		var typeInfo = CreateGeneratedEnumTypeInfo(field, generatedEnum);
		return Generate(field, resolvedTypeName, typeInfo);
	}

	private GeneratedMavlinkMessageField Generate(MavlinkMessageField field, string basePropertyTypeName, GeneratedMavlinkMessageFieldTypeBase typeInfo)
	{
		string normalizedFieldName = Utilities.ToUpperCamelCase(field.Name);

		string? summaryCommentBlock = string.IsNullOrEmpty(field.Description)
			? null
			: Utilities.CreateSummaryTrivia(field.Description!).ToFullString().TrimEnd();

		var validationRule = _validationRuleProvider.GetRule(field);
		var typeStructure = BuildTypeStructure(field, validationRule, basePropertyTypeName);
		var finalPropertyTypeName = RenderTypeStructure(typeStructure);

		var attributesBuilder = ImmutableList.CreateBuilder<string>();
		if (TryGetArrayLength(typeStructure, out int arrayLength))
		{
			attributesBuilder.Add($"[System.ComponentModel.DataAnnotations.RequiredArrayLength({arrayLength})]");
		}

		var model = new PropertyTemplateModel(
			SummaryCommentBlock: summaryCommentBlock,
			RemarksName: field.Name,
			Attributes: attributesBuilder.ToImmutable(),
			PropertyType: finalPropertyTypeName,
			PropertyName: normalizedFieldName);

		string propertyCode = _propertyTemplate.Render(model);
		var trimmedCode = propertyCode.Trim();

		if (string.IsNullOrEmpty(trimmedCode))
		{
			throw new InvalidOperationException("Generated property code is empty.");
		}

		var propertySyntax = SyntaxFactory.ParseMemberDeclaration(trimmedCode) as PropertyDeclarationSyntax;

		if (propertySyntax == null)
		{
			throw new InvalidOperationException($"Failed to parse the generated property code for '{field.Name}':\n---\n{trimmedCode}\n---");
		}

		return new GeneratedMavlinkMessageField(
			generatedName: normalizedFieldName,
			generatedFieldType: typeInfo,
			validationRule: validationRule,
			declarationSyntax: propertySyntax,
			original: field);
	}

	private bool TryGetArrayLength(GeneratedMavlinkMessageFieldTypeStructure structure, out int length)
	{
		length = 0;
		switch (structure)
		{
			case GeneratedMavlinkMessageFieldArrayTypeStructure array:
				length = array.Length;
				return true;
			case GeneratedMavlinkMessageFieldInvalidatableTypeStructure invalidatable when invalidatable.InnerType is GeneratedMavlinkMessageFieldArrayTypeStructure innerArray:
				length = innerArray.Length;
				return true;
			default:
				return false;
		}
	}

	private GeneratedMavlinkMessageFieldTypeStructure BuildTypeStructure(
		MavlinkMessageField field,
		GeneratedMavlinkMessageFieldValidationRule rule,
		string baseTypeName)
	{
		bool isArray = field.Type.TypeName.Contains("[");
		var scalarType = new GeneratedMavlinkMessageFieldScalarTypeStructure(baseTypeName);

		if (isArray)
		{
			int length = ParseArrayLength(field.Type.TypeName);

			return rule switch
			{
				GeneratedMavlinkMessagePerElementValidationRule =>
					new GeneratedMavlinkMessageFieldArrayTypeStructure(
						new GeneratedMavlinkMessageFieldInvalidatableTypeStructure(scalarType),
						length),

				GeneratedMavlinkMessageWholeFieldValidationRule =>
					new GeneratedMavlinkMessageFieldInvalidatableTypeStructure(
						new GeneratedMavlinkMessageFieldArrayTypeStructure(scalarType, length)),

				_ => new GeneratedMavlinkMessageFieldArrayTypeStructure(scalarType, length),
			};
		}
		else
		{
			return rule switch
			{
				GeneratedMavlinkMessageWholeFieldValidationRule =>
					new GeneratedMavlinkMessageFieldInvalidatableTypeStructure(scalarType),

				_ => scalarType,
			};
		}
	}

	private string RenderTypeStructure(GeneratedMavlinkMessageFieldTypeStructure structure)
	{
		return structure switch
		{
			GeneratedMavlinkMessageFieldScalarTypeStructure scalar => scalar.TypeName,

			GeneratedMavlinkMessageFieldInvalidatableTypeStructure invalidatable =>
				$"Invalidatable<{RenderTypeStructure(invalidatable.InnerType)}>",

			GeneratedMavlinkMessageFieldArrayTypeStructure array =>
				$"System.Collections.Immutable.ImmutableArray<{RenderTypeStructure(array.ElementType)}>",

			_ => throw new NotSupportedException($"Cannot render type structure: {structure.GetType().Name}")
		};
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

	private static int ParseArrayLength(string typeName)
	{
		var startIndex = typeName.IndexOf('[') + 1;
		var endIndex = typeName.IndexOf(']');
		var lengthStr = typeName.Substring(startIndex, endIndex - startIndex);
		return int.Parse(lengthStr);
	}
}
