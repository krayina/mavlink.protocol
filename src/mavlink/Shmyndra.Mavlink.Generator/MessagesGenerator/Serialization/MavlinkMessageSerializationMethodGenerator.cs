using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;

namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// The main orchestrator for generating a MAVLink message serialization method.
/// This is the serialization counterpart to <c>MavlinkMessageDeserializationMethodGenerator</c>.
/// </summary>
public abstract partial class MavlinkMessageSerializationMethodGenerator
{
	private readonly ImmutableArray<ISerializationFieldScribanTemplateModelFactory> _factories;
	private readonly ISerializationPayloadWriteScribanStrategy _payloadWriteScribanStrategy;
	private readonly IInvalidValueExpressionBuilder _invalidValueBuilder;
	private readonly bool _useObjectiveBitmask;
	private readonly Template _scribanTemplate;

	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkMessageSerializationMethodGenerator"/> class.
	/// </summary>
	protected MavlinkMessageSerializationMethodGenerator(
		ISerializationPayloadWriteScribanStrategy payloadWriteScribanStrategy,
		IInvalidValueExpressionBuilder invalidValueBuilder,
		bool useObjectiveBitmask)
	{
		_payloadWriteScribanStrategy = payloadWriteScribanStrategy;
		_invalidValueBuilder = invalidValueBuilder;
		_useObjectiveBitmask = useObjectiveBitmask;
		_factories = CreateFactories();
		_scribanTemplate = Template.Parse(Templates.SerializationMethodTemplate);
	}

	/// <summary>
	/// Generates the complete serialization method for a given set of fields.
	/// </summary>
	/// <param name="methodName">The name for the generated C# method (e.g., "SerializeWithoutExtensions").</param>
	/// <param name="currentNamespace">The namespace for the message class.</param>
	/// <param name="messageName">The name of the message class.</param>
	/// <param name="fieldsToProcess">The specific fields to include in this serialization method.</param>
	/// <returns>A <see cref="GeneratedMavlinkMessageSerializationMethod"/> containing the generated code.</returns>
	public GeneratedMavlinkMessageSerializationMethod Generate(
		string methodName,
		string currentNamespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fieldsToProcess)
	{
		int totalSize = fieldsToProcess.Sum(f => f.GeneratedType.GetFieldTypeSize());
		var fieldModels = ImmutableArray.CreateBuilder<ISerializationFieldScribanTemplateModel>();
		int offset = 0;

		foreach (var field in fieldsToProcess)
		{
			var factory = _factories.FirstOrDefault(f => f.CanHandle(field, _useObjectiveBitmask));
			if (factory == null)
			{
				throw new NotSupportedException($"No serialization model factory found for field '{field.GeneratedName}' of type '{field.GeneratedType.GetType().Name}'.");
			}

			var context = new FieldSerializationScribanContext(
				field,
				offset,
				_payloadWriteScribanStrategy,
				_useObjectiveBitmask,
				_invalidValueBuilder
			);

			var model = factory.CreateModel(context);
			fieldModels.Add(model);

			offset += field.GeneratedType.GetFieldTypeSize();
		}

		var rootModel = new MavlinkSerializationMethodModel(
			messageName,
			GetMethodSignature(methodName, messageName),
			GetInitializationBlock(messageName, totalSize),
			fieldModels.ToImmutable(),
			totalSize
		);

		var scribanContext = CSharpScribanTemplateContext.Create(); // Assumes this helper exists
		scribanContext.PushGlobal(new ScriptObject { ["model"] = rootModel });

		string methodCode = _scribanTemplate.Render(scribanContext);

		var syntaxTree = CSharpSyntaxTree.ParseText($"class D {{ {methodCode} }}", new CSharpParseOptions(LanguageVersion.Latest));
		var methodSyntax = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();

		return new GeneratedMavlinkMessageSerializationMethod(
			currentNamespace,
			messageName,
			fieldsToProcess,
			methodSyntax.NormalizeWhitespace()
		);
	}

	/// <summary>
	/// Creates and configures the list of field serialization model factories.
	/// The order is important: more specific factories should come before more general ones.
	/// </summary>
	private static ImmutableArray<ISerializationFieldScribanTemplateModelFactory> CreateFactories()
	{
		return
		[
			new TerminatedStringSerializationFieldScribanTemplateModelFactory(),
			new ClassicBitmaskSerializationFieldScribanTemplateModelFactory(),
			new ObjectiveBitmaskSerializationFieldScribanTemplateModelFactory(),
			new ArrayFieldSerializationFieldScribanTemplateModelFactory(),
			new EnumFieldSerializationFieldScribanTemplateModelFactory(),
			new PrimitiveFieldSerializationFieldScribanTemplateModelFactory()
		];
	}

	/// <summary>
	/// When implemented in a derived class, gets the full C# method signature.
	/// </summary>
	protected abstract string GetMethodSignature(string methodName, string messageName);

	/// <summary>
	/// When implemented in a derived class, gets the C# code block for payload initialization and validation.
	/// </summary>
	protected abstract string GetInitializationBlock(string messageName, int requiredSize);
}
