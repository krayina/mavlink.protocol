namespace Shmyndra.Mavlink.Generator;

public partial class MavlinkMessageFieldInitPropertyGenerator
{
	internal static class Templates
	{
		internal const string PropertyTemplate = @"
{{- if summary_comment_block }}
{{ summary_comment_block }}
{{- end }}
/// <remarks>
/// Original name: {{ remarks_name }}
/// </remarks>
{{- for attribute in attributes }}
{{ attribute }}
{{- end }}
public {{ property_type }} {{ property_name }} { get; init; }
";
	}
}
