namespace System.ComponentModel.DataAnnotations;

/// <summary>
/// This file must be linked from foreign folder
/// </summary>
internal class RequiredArrayLengthAttribute : ValidationAttribute
{
	private readonly int _length;

	public RequiredArrayLengthAttribute(int length)
	{
		_length = length;
	}

	protected override ValidationResult IsValid(object value, ValidationContext validationContext)
	{
		if (value is Array array && array.Length == _length)
		{
			return ValidationResult.Success;
		}
		return new ValidationResult($"Array length must be {_length}.");
	}
}
