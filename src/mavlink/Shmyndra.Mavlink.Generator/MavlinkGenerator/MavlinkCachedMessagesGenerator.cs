using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Shmyndra.Mavlink.Generator;

internal record MavlinkCachedMessage(string FullName, ulong Id, string XmlName);

internal static class MavlinkCachedMessagesGenerator
{
	public static MemberDeclarationSyntax GenerateMessagesCache(IEnumerable<MavlinkCachedMessage> messages)
	{
		var dictionaryEntries = string.Join(",\n", messages.Select(message =>
			$"{{ typeof({message.FullName}), ({message.Id}U, \"{message.XmlName}\") }}"));

		var classCode = $@"
public static class MavlinkMessages
{{
    private static Dictionary<Type, (ulong Id, string XmlName)> _mavlinkMessages = new()
    {{
        {dictionaryEntries}
    }};

    public static ulong GetId<T>() where T : MavlinkMessage
    {{
        return _mavlinkMessages[typeof(T)].Id;
    }}

    public static string GetXmlName<T>() where T : MavlinkMessage
    {{
        return _mavlinkMessages[typeof(T)].XmlName;
    }}
}}";

		var memberDeclaration = SyntaxFactory.ParseMemberDeclaration(classCode);
		if (memberDeclaration == null)
		{
			throw new InvalidOperationException("Generated class code is invalid.");
		}

		return memberDeclaration;
	}
}
