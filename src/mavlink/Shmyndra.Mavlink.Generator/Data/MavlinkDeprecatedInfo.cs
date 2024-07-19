namespace Shmyndra.Mavlink.Generator;

/// <summary>
/// Represents deprecated information with various properties.
/// This record is used to indicate that an element is <see cref="System.ObsoleteAttribute"/> and provides details about the deprecation.
/// </summary>
public record MavlinkDeprecatedInfo
{
	/// <summary>
	/// Gets or sets the description of the deprecated information.
	/// </summary>
	public string? Description { get; init; }

	/// <summary>
	/// Gets or sets the date since the information has been deprecated.
	/// </summary>
	/// <remarks>
	/// The pattern for the date is (20)\\d{2}-(0[1-9]|1[012]), which ensures the date is in the format YYYY-MM.
	/// </remarks>
	public string Since { get; init; }

	/// <summary>
	/// Gets or sets the replacement information for the deprecated element.
	/// </summary>
	public string ReplacedBy { get; init; }

	/// <summary>
	/// Gets or sets additional text related to the deprecated information.
	/// </summary>
	public string[]? Text { get; init; }

	/// <summary>
	/// Initializes a new instance of the <see cref="MavlinkDeprecatedInfo"/> record.
	/// </summary>
	/// <param name="description">The description of the deprecated information.</param>
	/// <param name="since">The date since the information has been deprecated.</param>
	/// <param name="replacedBy">The replacement information for the deprecated element.</param>
	/// <param name="text">Additional text related to the deprecated information.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="since"/> or <paramref name="replacedBy"/> is <c>null</c>.</exception>
	public MavlinkDeprecatedInfo(
		string? description,
		string since,
		string replacedBy,
		string[]? text)
	{
		Since = since ?? throw new ArgumentNullException(nameof(since));
		ReplacedBy = replacedBy ?? throw new ArgumentNullException(nameof(replacedBy));
		Description = description;
		Text = text;
	}
}
