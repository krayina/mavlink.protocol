namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageGenerator
{
	internal static class Templates
	{
		internal const string MessageTemplate = @"
{{- # Explicitly output newline to separate summary from remarks.
    # See: https://github.com/scriban/scriban/issues/145 -}}
{{- if message.summary_comment_block -}}
{{ message.summary_comment_block }}
{{- ""\n"" -}}
{{- end -}}
/// <remarks>
/// Original name: {{ message.original_name }}
/// </remarks>
{{- if message.is_obsolete }}
[System.Obsolete(""{{ message.obsolete_message }}"")]
{{- end }}
[MavlinkIdentifiedType({{ message.id }}U, ""{{ message.original_name }}"")]
public readonly record struct {{ message.name }} : MavlinkMessage, IMavlinkMessageSerializerWithoutExtensions{{ if message.has_extensions }}, IMavlinkMessageSerializerWithExtensions{{ end }}
{
    {{- # Add a blank line after { only if there are members. -}}
    {{- if message.all_members | array.size > 0 -}}
    {{- ""\n"" -}}
    {{- end -}}

    {{- for member in message.all_members -}}
    {{- indent member -}}
    {{- # Always add a single newline after each member.
        # This prevents the final `end` tag from sticking to the last member's code. -}}
    {{- ""\n"" -}}
    {{- # If it's not the last member, add a second newline to create a blank line. -}}
    {{- if !for.last -}}
    {{- ""\n"" -}}
    {{- end -}}
    {{- end -}}
}";
	}
}
