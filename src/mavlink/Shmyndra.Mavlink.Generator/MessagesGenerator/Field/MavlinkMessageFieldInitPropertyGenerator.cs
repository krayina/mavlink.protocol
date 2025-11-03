using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageFieldInitPropertyGenerator : IMavlinkMessageFieldPropertyGenerator
{
	private static readonly Template _propertyTemplate;

	private readonly IMavlinkMessageFieldTypeNameResolutionStrategy _typeNameResolver;
	private readonly IMavlinkMessageFieldValidationRuleDefinitionProvider _ruleDefinitionProvider;
	private readonly IInvalidatabilityPlacementProvider _placementProvider;

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
		IMavlinkMessageFieldValidationRuleDefinitionProvider ruleDefinitionProvider,
		IInvalidatabilityPlacementProvider placementProvider)
	{
		_typeNameResolver = typeNameResolver ?? throw new ArgumentNullException(nameof(typeNameResolver));
		_ruleDefinitionProvider = ruleDefinitionProvider ?? throw new ArgumentNullException(nameof(ruleDefinitionProvider));
		_placementProvider = placementProvider ?? throw new ArgumentNullException(nameof(placementProvider));
	}

	public GeneratedMavlinkMessageField GeneratePrimitiveProperty(MavlinkMessageField field)
	{
		if (field == null)
		{
			throw new ArgumentNullException(nameof(field));
		}

		var resolvedTypeName = _typeNameResolver.ResolvePrimitive(field);
		var typeInfo = CreateGeneratedTypeInfo(field);
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
		var typeInfo = CreateGeneratedTypeInfo(field, generatedEnum);
		return Generate(field, resolvedTypeName, typeInfo);
	}

	private static GeneratedMavlinkMessageFieldType CreateGeneratedTypeInfo(MavlinkMessageField field, GeneratedMavlinkEnum? generatedEnum = null)
	{
		bool isArray = field.Type.TypeName.Contains("[");
		var mavlinkBaseTypeName = field.Type.GetTypeWithoutArray();
		var csharpTypeName = Utilities.MavlinkTypeMap[mavlinkBaseTypeName].TypeName;

		GeneratedMavlinkMessageFieldScalarType elementType;
		if (generatedEnum != null && field.Type is MavlinkMessageFieldEnumType enumType)
		{
			elementType = new GeneratedMavlinkMessageFieldEnumType(csharpTypeName, generatedEnum, enumType);
		}
		else
		{
			elementType = new GeneratedMavlinkMessageFieldPrimitiveType(csharpTypeName, field.Type);
		}

		if (isArray)
		{
			var arrayLength = ParseArrayLength(field.Type.TypeName);
			return new GeneratedMavlinkMessageFieldArrayType(elementType, arrayLength, field.Type);
		}
		return elementType;
	}

	private GeneratedMavlinkMessageField Generate(MavlinkMessageField field, string basePropertyTypeName, GeneratedMavlinkMessageFieldType typeInfo)
	{
		var ruleDefinition = _ruleDefinitionProvider.GetRuleDefinition(field);
		var placement = _placementProvider.GetPlacement(ruleDefinition);
		var typeStructure = BuildTypeStructure(typeInfo, placement, basePropertyTypeName);
		var finalPropertyTypeName = RenderTypeStructure(typeStructure);

		string normalizedFieldName = Utilities.ToUpperCamelCase(field.Name);
		string? summaryCommentBlock = string.IsNullOrEmpty(field.Description)
			? null
			: Utilities.CreateSummaryTrivia(field.Description!).ToFullString().TrimEnd();

		var attributesBuilder = ImmutableList.CreateBuilder<string>();
		if (typeInfo is GeneratedMavlinkMessageFieldArrayType arrayType)
		{
			attributesBuilder.Add($"[System.ComponentModel.DataAnnotations.RequiredArrayLength({arrayType.ArrayLength})]");
		}

		var model = new PropertyTemplateModel(
			SummaryCommentBlock: summaryCommentBlock,
			RemarksName: field.Name,
			Attributes: attributesBuilder.ToImmutable(),
			PropertyType: finalPropertyTypeName,
			PropertyName: normalizedFieldName);

		string propertyCode = _propertyTemplate.Render(model);
		var trimmedCode = propertyCode.Trim();

		var propertySyntax = SyntaxFactory.ParseMemberDeclaration(trimmedCode) as PropertyDeclarationSyntax
			?? throw new InvalidOperationException($"Failed to parse the generated property code for '{field.Name}':\n---\n{trimmedCode}\n---");

		return new GeneratedMavlinkMessageField(
			generatedName: normalizedFieldName,
			generatedFieldType: typeInfo,
			validationRule: ruleDefinition,
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
			case GeneratedMavlinkMessageFieldInvalidatableTypeStructure invalidatable
				when invalidatable.InnerType is GeneratedMavlinkMessageFieldArrayTypeStructure innerArray:

				length = innerArray.Length;
				return true;
			default:
				return false;
		}
	}

	private GeneratedMavlinkMessageFieldTypeStructure BuildTypeStructure(
		GeneratedMavlinkMessageFieldType typeInfo,
		InvalidatabilityPlacement placement,
		string baseTypeName)
	{
		var scalarType = new GeneratedMavlinkMessageFieldScalarTypeStructure(baseTypeName);

		if (typeInfo is GeneratedMavlinkMessageFieldArrayType arrayType)
		{
			return placement switch
			{
				WholeFieldInvalidatability =>
					new GeneratedMavlinkMessageFieldInvalidatableTypeStructure(
						new GeneratedMavlinkMessageFieldArrayTypeStructure(scalarType, arrayType.ArrayLength)),

				PerElementInvalidatability =>
					new GeneratedMavlinkMessageFieldArrayTypeStructure(
						new GeneratedMavlinkMessageFieldInvalidatableTypeStructure(scalarType),
						arrayType.ArrayLength),

				_ => new GeneratedMavlinkMessageFieldArrayTypeStructure(scalarType, arrayType.ArrayLength),
			};
		}
		else
		{
			if (placement is WholeFieldInvalidatability)
			{
				return new GeneratedMavlinkMessageFieldInvalidatableTypeStructure(scalarType);
			}
			return scalarType;
		}
	}

	private string RenderTypeStructure(GeneratedMavlinkMessageFieldTypeStructure structure)
	{
		return structure switch
		{
			GeneratedMavlinkMessageFieldScalarTypeStructure scalar => scalar.TypeName,
			GeneratedMavlinkMessageFieldInvalidatableTypeStructure inv => $"Invalidatable<{RenderTypeStructure(inv.InnerType)}>",
			GeneratedMavlinkMessageFieldArrayTypeStructure array => $"System.Collections.Immutable.ImmutableArray<{RenderTypeStructure(array.ElementType)}>",
			_ => throw new NotSupportedException($"Cannot render type structure: {structure.GetType().Name}")
		};
	}

	private static int ParseArrayLength(string typeName)
	{
		var startIndex = typeName.IndexOf('[') + 1;
		var endIndex = typeName.IndexOf(']');
		var lengthStr = typeName.Substring(startIndex, endIndex - startIndex);
		return int.Parse(lengthStr);
	}
}
