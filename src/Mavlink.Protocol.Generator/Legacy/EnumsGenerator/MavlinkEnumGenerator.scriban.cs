namespace Mavlink.Protocol.Generator;

public partial class MavlinkEnumGenerator
{
	internal static class Templates
	{
		internal const string EnumTemplate = @"
{{- if summary_comment_block }}
{{ summary_comment_block }}
{{- end }}
{{- if remarks }}
/// <remarks>
/// {{ remarks }}
/// </remarks>
{{- end }}
[MavlinkType(""{{ original_name }}"")]
{{- if is_bitmask }}
[System.Flags]
{{- end }}
{{- if is_deprecated }}
[System.Obsolete(""{{ deprecated_reason }}"")]
{{- end }}
public enum {{ enum_name }}{{ if has_base_type }} : {{ base_type_name }}{{ end }}
{
{{- for entry in entries }}
{{ entry }}{{ if !for.last }},{{ end }}
{{- end }}
}
";
	}
}
