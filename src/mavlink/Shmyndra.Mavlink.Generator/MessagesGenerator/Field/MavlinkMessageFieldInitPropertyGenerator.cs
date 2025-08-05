using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageFieldInitPropertyGenerator : IMavlinkMessageFieldPropertyGenerator
{
	private static readonly Template _propertyTemplate;
	private readonly IMavlinkMessageFieldTypeNameResolutionStrategy _typeNameResolver;

	static MavlinkMessageFieldInitPropertyGenerator()
	{
		_propertyTemplate = Template.Parse(Templates.PropertyTemplate);
		if (_propertyTemplate.HasErrors)
		{
			var errors = string.Join("\n", _propertyTemplate.Messages.Select(m => m.Message));
			throw new InvalidOperationException($"Failed to parse Property Scriban template: \n{errors}");
		}
	}

	public MavlinkMessageFieldInitPropertyGenerator(IMavlinkMessageFieldTypeNameResolutionStrategy typeNameResolver)
	{
		_typeNameResolver = typeNameResolver ?? throw new ArgumentNullException(nameof(typeNameResolver));
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

	private GeneratedMavlinkMessageField Generate(MavlinkMessageField field, string propertyTypeName, GeneratedMavlinkMessageFieldTypeBase typeInfo)
	{
		string normalizedFieldName = Utilities.ToUpperCamelCase(field.Name);

		string? summaryCommentBlock = string.IsNullOrEmpty(field.Description)
			? null
			: Utilities.CreateSummaryTrivia(field.Description!).ToFullString().TrimEnd();

		var attributesBuilder = ImmutableList.CreateBuilder<string>();

		string finalPropertyType = propertyTypeName;

		int? arrayLength = null;
		switch (typeInfo)
		{
			case GeneratedMavlinkMessageFieldArrayType arrayType:
				arrayLength = arrayType.ArrayLength;
				break;
			case GeneratedMavlinkMessageFieldArrayEnumType arrayEnumType:
				arrayLength = arrayEnumType.ArrayLength;
				break;
		}

		if (arrayLength.HasValue)
		{
			finalPropertyType = $"System.Collections.Immutable.ImmutableArray<{propertyTypeName}>";
			attributesBuilder.Add($"[System.ComponentModel.DataAnnotations.RequiredArrayLength({arrayLength.Value})]");
		}

		var model = new PropertyTemplateModel(
			SummaryCommentBlock: summaryCommentBlock,
			RemarksName: field.Name,
			Attributes: attributesBuilder.ToImmutable(),
			PropertyType: finalPropertyType,
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
			declarationSyntax: propertySyntax,
			original: field,
			generatedFieldType: typeInfo);
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
}
