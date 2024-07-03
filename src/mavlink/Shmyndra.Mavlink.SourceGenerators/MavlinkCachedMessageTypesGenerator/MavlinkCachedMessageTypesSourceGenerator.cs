using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Shmyndra.Mavlink.SourceGenerators.MavlinkCachedMessageTypesGenerator;

[Generator]
public class MavlinkCachedMessageTypesSourceGenerator : ISourceGenerator
{
	public void Initialize(GeneratorInitializationContext context)
	{
		context.RegisterForSyntaxNotifications(() => new MavlinkAttributeReceiver());
	}

	public void Execute(GeneratorExecutionContext context)
	{
		if (!System.Diagnostics.Debugger.IsAttached)
		{
			//Uncomment this line to enter Debug mode for the generator and perform a Rebuild
			//System.Diagnostics.Debugger.Launch();
		}

		if (context.SyntaxReceiver is not MavlinkAttributeReceiver receiver)
		{
			return;
		}

		var mavlinkMessages = receiver.MavlinkMessages;

		var builder = new StringBuilder();
		builder.AppendLine("using System;");
		builder.AppendLine("using System.Collections.Generic;");
		builder.AppendLine("namespace MavlinkTypes");
		builder.AppendLine("{");
		builder.AppendLine("    public static class MavlinkMessages");
		builder.AppendLine("    {");
		builder.AppendLine("        private static Dictionary<Type, (ulong Id, string XmlName)> _mavlinkMessages = new()");
		builder.AppendLine("        {");

		for (int i = 0; i < mavlinkMessages.Count; i++)
		{
			var messageInfo = mavlinkMessages[i];
			var comma = i < mavlinkMessages.Count - 1 ? "," : "";
			builder.AppendLine($"            {{ typeof({messageInfo.TypeName}), ({messageInfo.Id}, \"{messageInfo.XmlName}\") }}{comma}");
		}

		builder.AppendLine("        };");
		builder.AppendLine();
		builder.AppendLine("        public static ulong GetId<T>() where T : MavlinkMessage");
		builder.AppendLine("        {");
		builder.AppendLine("            return _mavlinkMessages[typeof(T)].Id;");
		builder.AppendLine("        }");
		builder.AppendLine();
		builder.AppendLine("        public static string GetXmlName<T>() where T : MavlinkMessage");
		builder.AppendLine("        {");
		builder.AppendLine("            return _mavlinkMessages[typeof(T)].XmlName;");
		builder.AppendLine("        }");
		builder.AppendLine("    }");
		builder.AppendLine("}");

		context.AddSource("MavlinkMessages.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
	}

	class MavlinkAttributeReceiver : ISyntaxReceiver
	{
		public List<(string TypeName, ulong Id, string XmlName)> MavlinkMessages { get; } = new List<(string TypeName, ulong Id, string XmlName)>();

		public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
		{
			if (syntaxNode is RecordDeclarationSyntax recordDeclaration &&
				recordDeclaration.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword))
			{
				var attributes = recordDeclaration.AttributeLists.SelectMany(al => al.Attributes);
				foreach (var attribute in attributes)
				{
					if (attribute.Name.ToString().Contains("MavlinkIdentifiedType"))
					{
						var typeName = recordDeclaration.Identifier.Text;

						var idArgument = attribute.ArgumentList!.Arguments.FirstOrDefault(arg =>
							arg.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NumericLiteralExpression));
						var xmlNameArgument = attribute.ArgumentList!.Arguments.FirstOrDefault(arg =>
							arg.Expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression));

						if (idArgument != null && xmlNameArgument != null)
						{
							var idText = idArgument.Expression.ToString().TrimEnd('u', 'U');
							var xmlNameText = xmlNameArgument.Expression.ToString().Trim('"');
							if (ulong.TryParse(idText, out var id))
							{
								MavlinkMessages.Add((typeName, id, xmlNameText));
							}
						}
					}
				}
			}
		}
	}
}
