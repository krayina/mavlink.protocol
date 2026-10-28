using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using Scriban.Runtime;

namespace Mavlink.Protocol.Generator;

/// <summary>
/// The main orchestrator for generating a MAVLink message deserialization method.
/// </summary>
public abstract partial class MavlinkMessageDeserializationMethodGenerator
{
	private readonly IMavlinkMessageFieldValidationExpressionCompiler _validationCompiler;
	private readonly ImmutableArray<IDeserializationFieldScribanTemplateModelFactory> _factories;
	private readonly IDeserializationPayloadReadScribanStrategy _payloadReadScribanStrategy;
	private readonly bool _useObjectiveBitmask;
	private readonly Template _scribanTemplate;

	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkMessageDeserializationMethodGenerator"/> class.
	/// </summary>
	protected MavlinkMessageDeserializationMethodGenerator(
		IMavlinkMessageFieldValidationExpressionCompiler validationCompiler,
		IDeserializationPayloadReadScribanStrategy payloadReadScribanStrategy,
		bool useObjectiveBitmask)
	{
		_validationCompiler = validationCompiler;
		_payloadReadScribanStrategy = payloadReadScribanStrategy;
		_useObjectiveBitmask = useObjectiveBitmask;
		_factories = CreateFactories();
		_scribanTemplate = Template.Parse(Templates.DeserializationMethodTemplate);
	}

	/// <summary>
	/// Generates the complete deserialization method for a given set of fields.
	/// </summary>
	/// <param name="methodName">The name for the generated C# method (e.g., "DeserializeWithoutExtensions").</param>
	/// <param name="currentNamespace">The namespace for the message class.</param>
	/// <param name="messageName">The name of the message class.</param>
	/// <param name="fieldsToProcess">The specific fields to include in this deserialization method.</param>
	/// <returns>A <see cref="GeneratedMavlinkMessageDeserializationMethod"/> containing the generated code.</returns>
	public GeneratedMavlinkMessageDeserializationMethod Generate(
		string methodName,
		string currentNamespace,
		string messageName,
		ImmutableArray<GeneratedMavlinkMessageField> fieldsToProcess)
	{
		int totalSize = fieldsToProcess.Sum(f => f.GeneratedType.GetFieldTypeSize());
		var fieldModels = ImmutableArray.CreateBuilder<IDeserializationFieldScribanTemplateModel>();
		int offset = 0;

		foreach (var field in fieldsToProcess)
		{
			var factory = _factories.FirstOrDefault(f => f.CanHandle(field, _useObjectiveBitmask));
			if (factory == null)
			{
				throw new NotSupportedException($"No deserialization model factory found for field '{field.GeneratedName}' of type '{field.GeneratedType.GetType().Name}'.");
			}

			var context = new FieldDeserializationScribanContext(
				field,
				currentNamespace,
				offset,
				_validationCompiler,
				_payloadReadScribanStrategy,
				_useObjectiveBitmask
			);

			var model = factory.CreateModel(context);
			fieldModels.Add(model);

			offset += field.GeneratedType.GetFieldTypeSize();
		}

		var rootModel = new MavlinkDeserializationMethodModel(
			messageName,
			GetMethodSignature(methodName, messageName),
			GetInitializationBlock(messageName, totalSize),
			fieldModels.ToImmutable()
		);

		var scribanContext = CSharpScribanTemplateContext.Create();
		scribanContext.PushGlobal(new ScriptObject { ["model"] = rootModel });

		string methodCode = _scribanTemplate.Render(scribanContext);

		string wrapperCode = $"class DummyClass {{ {methodCode} }}";
		var syntaxTree = CSharpSyntaxTree.ParseText(wrapperCode, new CSharpParseOptions(LanguageVersion.Latest));
		var methodSyntax = syntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().First();

		return new GeneratedMavlinkMessageDeserializationMethod(
			currentNamespace, messageName, fieldsToProcess, methodSyntax.NormalizeWhitespace());
	}

	/// <summary>
	/// Creates and configures the list of field deserialization model factories.
	/// The order is important: more specific factories should come before more general ones.
	/// </summary>
	private static ImmutableArray<IDeserializationFieldScribanTemplateModelFactory> CreateFactories()
	{
		return
		[
			new TerminatedStringDeserializationFieldScribanTemplateModelFactory(),
			new ClassicBitmaskDeserializationFieldScribanTemplateModelFactory(),
			new ObjectiveBitmaskDeserializationFieldScribanTemplateModelFactory(),
			new EnumFieldDeserializationFieldScribanTemplateModelFactory(),
			new ArrayFieldDeserializationFieldScribanTemplateModelFactory(),
			new PrimitiveFieldDeserializationFieldScribanTemplateModelFactory()
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
