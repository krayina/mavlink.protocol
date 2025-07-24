namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageGenerator
{
	internal static class Templates
	{
		internal const string MessageTemplate = @"
{{- if summary }}
/// <summary>
/// {{ summary | string.replace ""\n"" ""\n/// "" }}
/// </summary>
{{- end }}
/// <remarks>
/// Original name: {{ original_name }}
/// </remarks>
{{- if is_obsolete }}
[System.Obsolete(""{{ obsolete_message }}"")]
{{- end }}
[MavlinkIdentifiedType({{ id }}U, ""{{ original_name }}"")]
public readonly record struct {{ name }} : MavlinkMessage, IMavlinkMessageSerializerWithoutExtensions{{ if has_extensions }}, IMavlinkMessageSerializerWithExtensions{{ end }}
{
    {{- for prop in properties ~}}
    {{- if prop.summary }}
    /// <summary>
    /// {{ prop.summary | string.replace ""\n"" ""\n    /// "" }}
    /// </summary>
    {{- end }}
    /// <remarks>
    /// {{ prop.remarks }}
    /// </remarks>
    {{ prop.declaration }}
    
    {{ end -}}

    {{- for method_text in methods ~}}
    {{ method_text }}
    {{ end -}}
}
";
	}
}
