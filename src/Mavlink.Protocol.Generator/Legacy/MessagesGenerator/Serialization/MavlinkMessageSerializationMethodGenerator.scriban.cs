namespace Mavlink.Protocol.Generator;

public partial class MavlinkMessageSerializationMethodGenerator
{
	internal static class Templates
	{
		internal const string SerializationMethodTemplate = @"
{{- model.method_signature }}
{
    {{- model.initialization_block | string.strip }}

    {{~ for field in model.fields ~}}
    {{- include field.template_name field ~}}
    {{~ end ~}}

    return {{ model.payload_size }};
}

{{- ############################################################# -}}
{{- # PARTIAL for CustomSerializationCode                        # -}}
{{- ############################################################# -}}
{{- func CustomSerializationCode(field) ~}}
{{ field.serialization_code_block }}
{{- end ~}}

{{- ############################################################# -}}
{{- # PARTIAL for PrimitiveField (and ObjectiveBitmaskField)   # -}}
{{- ############################################################# -}}
{{- func PrimitiveField(field) ~}}
{{~ if field.is_nullable ~}}
    if (message.{{ field.property_name }} != null)
    {
        {{ field.write_statement }};
    }
{{~ else ~}}
    {{ field.write_statement }};
{{~ end ~}}
{{- end ~}}
{{- func ObjectiveBitmaskField(field)
    PrimitiveField(field)
end ~}}

{{- ############################################################# -}}
{{- # PARTIAL for ClassicBitmaskField                           # -}}
{{- ############################################################# -}}
{{- func ClassicBitmaskField(field) ~}}
{
    {{ field.underlying_type }} combined = 0;
    {{- if field.is_nullable }}
    if (message.{{ field.property_name }} != null)
    {
        foreach (var flag in message.{{ field.property_name }}) { combined |= ({{ field.underlying_type }})flag; }
    }
    {{- else }}
    foreach (var flag in message.{{ field.property_name }}) { combined |= ({{ field.underlying_type }})flag; }
    {{- end }}
    {{ field.write_statement | string.replace '{value}' 'combined' }};
}
{{- end ~}}

{{- ############################################################# -}}
{{- # PARTIAL for ArrayField                                    # -}}
{{- ############################################################# -}}
{{- func ArrayField(field) ~}}
for (int i = 0; i < {{ field.array_length }}; i++)
{
    {{~ if field.elements_are_nullable ~}}
    var valueToWrite = message.{{ field.property_name }}[i].HasValue 
        ? message.{{ field.property_name }}[i].Value
        : {{ field.default_value_expression }};
    {{ field.element_write_statement | string.replace ('message.' + field.property_name + '[i]') 'valueToWrite' }};
    {{~ else ~}}
    {{ field.element_write_statement }};
    {{~ end ~}}
}
{{- end ~}}
";
	}
}
