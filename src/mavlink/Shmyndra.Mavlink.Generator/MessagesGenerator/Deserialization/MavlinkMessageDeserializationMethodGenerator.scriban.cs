namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Contains the Scriban templates used for code generation.
/// </summary>
public abstract partial class MavlinkMessageDeserializationMethodGenerator
{
	internal static class Templates
	{
		internal const string DeserializationMethodTemplate = @"
{{- model.method_signature }}
{
    {{ model.initialization_block | string.strip }}

    {{~ for field in model.fields ~}}
    {{- include field.template_name field ~}}
    {{~ end ~}}

    return new {{ model.message_name }}
    {
        {{~ for field in model.fields ~}}
        {{ field.property_name }} = {{ field.variable_name }}{{ if !for.last }},{{ end }}
        {{~ end ~}}
    };
}

{{- ############################################################# -}}
{{- # PARTIAL for CustomDeserializationFieldScribanTemplateModel# -}}
{{- ############################################################# -}}
{{- func CustomDeserializationCode(field) ~}}
{{ field.deserialization_code }}
{{- end ~}}

{{- ############################################################# -}}
{{- # PARTIAL for PrimitiveField                                # -}}
{{- ############################################################# -}}
{{- func PrimitiveField(field) ~}}
{{ if field.validation_condition }}
    var temp{{ field.variable_name | string.capitalize }} = {{ field.read_expression }};
    {{ field.result_type_name }} {{ field.variable_name }} = {{ field.validation_condition | string.replace 'value' ('temp' + (field.variable_name | string.capitalize)) }} ? temp{{ field.variable_name | string.capitalize }} : null;
{{ else }}
    var {{ field.variable_name }} = {{ field.read_expression }};
{{ end }}
{{- end ~}}

{{- ############################################################# -}}
{{- # PARTIAL for EnumField                                     # -}}
{{- ############################################################# -}}
{{- func EnumField(field) ~}}
{{ if field.validation_condition }}
    var temp{{ field.variable_name | string.capitalize }}Value = {{ field.read_expression }};
    {{ field.result_type_name }} {{ field.variable_name }} = null;
    if ({{ field.validation_condition | string.replace 'value' ('temp' + (field.variable_name | string.capitalize) + 'Value') }})
    {
        {{ field.variable_name }} = ({{ field.result_type_name }})temp{{ field.variable_name | string.capitalize }}Value;
    }
{{ else }}
    var {{ field.variable_name }} = ({{ field.result_type_name }}){{ field.read_expression }};
{{ end }}
{{- end ~}}

{{- ############################################################# -}}
{{- # PARTIAL for ClassicBitmaskField                           # -}}
{{- ############################################################# -}}
{{- func ClassicBitmaskField(field) ~}}
    var {{ field.variable_name }}Value = {{ field.read_expression }};
    var combined = ({{ field.bitwise_operation_type }}){{ field.variable_name }}Value;
    var {{ field.variable_name }}Builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<{{ field.enum_type_name }}>();
    for (int i = 0; i < {{ field.total_bits }}; i++)
    {
        var flag = ({{ field.bitwise_operation_type }})1 << i;
        if ((combined & flag) != 0)
        {
            {{ field.variable_name }}Builder.Add(({{ field.enum_type_name }})flag);
        }
    }
    var {{ field.variable_name }} = {{ field.variable_name }}Builder.ToImmutable();
{{- end ~}}

{{- ############################################################# -}}
{{- # PARTIAL for ObjectiveBitmaskField                         # -}}
{{- ############################################################# -}}
{{- func ObjectiveBitmaskField(field) ~}}
    var {{ field.variable_name }} = new {{ field.bitmask_type_name }}({{ field.read_expression }});
{{- end ~}}


{{- ############################################################# -}}
{{- # PARTIAL for ArrayField                                    # -}}
{{- ############################################################# -}}
{{- func ArrayField(field) ~}}
{{ field.temp_array_initialization }}
for (int i = 0; i < {{ field.array_length }}; i++)
{
    var value = {{ field.element_read_expression }};
{{~ if field.has_validation ~}}
    {{~ if field.all_elements_validation ~}}
    if ({{ field.all_elements_validation }})
    {
        {{ field.variable_name }}Temp[i] = ({{ field.element_final_type }})value;
    }
    {{~ else if field.per_index_validation.size > 0 ~}}
    switch (i)
    {
        {{~ for validation in field.per_index_validation ~}}
        case {{ validation.key }}:
            if ({{ validation.value }})
                {{ field.variable_name }}Temp[i] = ({{ field.element_final_type }})value;
            break;
        {{~ end ~}}
        default:
            {{ field.variable_name }}Temp[i] = ({{ field.element_final_type }})value; // No validation for this index
            break;
    }
    {{~ end ~}}
{{~ else ~}}
    {{ field.variable_name }}Temp[i] = ({{ field.element_final_type }})value;
{{~ end ~}}
}
var {{ field.variable_name }} = System.Collections.Immutable.ImmutableArray.CreateRange({{ field.variable_name }}Temp);
{{- end ~}}
";
	}
}
