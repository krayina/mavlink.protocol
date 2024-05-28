namespace Shmyndra.Mavlink.SourceGenerators.Protocol;

[Serializable()]
[System.Xml.Serialization.XmlType("SI_Unit", Namespace = "")]
public enum SiUnit
{
	[System.Xml.Serialization.XmlEnum("s")]
	S,
	[System.Xml.Serialization.XmlEnum("ds")]
	Ds,
	[System.Xml.Serialization.XmlEnum("cs")]
	Cs,
	[System.Xml.Serialization.XmlEnum("ms")]
	Ms,
	[System.Xml.Serialization.XmlEnum("us")]
	Us,
	[System.Xml.Serialization.XmlEnum("ns")]
	Ns,
	Hz,
	MHz,
	[System.Xml.Serialization.XmlEnum("km")]
	Km,
	[System.Xml.Serialization.XmlEnum("dam")]
	Dam,
	[System.Xml.Serialization.XmlEnum("m")]
	M,
	[System.Xml.Serialization.XmlEnum("m/s")]
	MS,
	[System.Xml.Serialization.XmlEnum("m/s/s")]
	MSS,
	[System.Xml.Serialization.XmlEnum("m/s*5")]
	MS5,
	[System.Xml.Serialization.XmlEnum("dm")]
	Dm,
	[System.Xml.Serialization.XmlEnum("dm/s")]
	DmS,
	[System.Xml.Serialization.XmlEnum("cm")]
	Cm,
	[System.Xml.Serialization.XmlEnum("cm^2")]
	Cm2,
	[System.Xml.Serialization.XmlEnum("cm/s")]
	CmS,
	[System.Xml.Serialization.XmlEnum("mm")]
	Mm,
	[System.Xml.Serialization.XmlEnum("mm/s")]
	MmS,
	[System.Xml.Serialization.XmlEnum("mm/h")]
	MmH,
	K,
	[System.Xml.Serialization.XmlEnum("degC")]
	DegC,
	[System.Xml.Serialization.XmlEnum("cdegC")]
	CdegC,
	[System.Xml.Serialization.XmlEnum("rad")]
	Rad,
	[System.Xml.Serialization.XmlEnum("rad/s")]
	RadS,
	[System.Xml.Serialization.XmlEnum("mrad/s")]
	MradS,
	[System.Xml.Serialization.XmlEnum("deg")]
	Deg,
	[System.Xml.Serialization.XmlEnum("deg/2")]
	Deg2,
	[System.Xml.Serialization.XmlEnum("deg/s")]
	DegS,
	[System.Xml.Serialization.XmlEnum("cdeg")]
	Cdeg,
	[System.Xml.Serialization.XmlEnum("cdeg/s")]
	CdegS,
	[System.Xml.Serialization.XmlEnum("degE5")]
	DegE5,
	[System.Xml.Serialization.XmlEnum("degE7")]
	DegE7,
	[System.Xml.Serialization.XmlEnum("rpm")]
	Rpm,
	V,
	[System.Xml.Serialization.XmlEnum("cV")]
	Cv,
	[System.Xml.Serialization.XmlEnum("mV")]
	Mv,
	A,
	[System.Xml.Serialization.XmlEnum("cA")]
	Ca,
	[System.Xml.Serialization.XmlEnum("mA")]
	Ma,
	[System.Xml.Serialization.XmlEnum("mAh")]
	MAh,
	Ah,
	[System.Xml.Serialization.XmlEnum("mT")]
	Mt,
	[System.Xml.Serialization.XmlEnum("gauss")]
	Gauss,
	[System.Xml.Serialization.XmlEnum("mgauss")]
	Mgauss,
	[System.Xml.Serialization.XmlEnum("hJ")]
	Hj,
	W,
	[System.Xml.Serialization.XmlEnum("mG")]
	Mg,
	[System.Xml.Serialization.XmlEnum("g")]
	G,
	[System.Xml.Serialization.XmlEnum("kg")]
	Kg,
	Pa,
	[System.Xml.Serialization.XmlEnum("hPa")]
	HPa,
	[System.Xml.Serialization.XmlEnum("kPa")]
	KPa,
	[System.Xml.Serialization.XmlEnum("mbar")]
	Mbar,
	[System.Xml.Serialization.XmlEnum("%")]
	Empty,
	[System.Xml.Serialization.XmlEnum("d%")]
	D,
	[System.Xml.Serialization.XmlEnum("c%")]
	C,
	[System.Xml.Serialization.XmlEnum("dB")]
	Db,
	[System.Xml.Serialization.XmlEnum("dBm")]
	DBm,
	KiB,
	[System.Xml.Serialization.XmlEnum("KiB/s")]
	KiBS,
	MiB,
	[System.Xml.Serialization.XmlEnum("MiB/s")]
	MiBS,
	[System.Xml.Serialization.XmlEnum("bytes")]
	Bytes,
	[System.Xml.Serialization.XmlEnum("bytes/s")]
	BytesS,
	[System.Xml.Serialization.XmlEnum("bits/s")]
	BitsS,
	[System.Xml.Serialization.XmlEnum("pix")]
	Pix,
	[System.Xml.Serialization.XmlEnum("dpix")]
	Dpix,
	[System.Xml.Serialization.XmlEnum("g/min")]
	GMin,
	[System.Xml.Serialization.XmlEnum("cm^3/min")]
	Cm3Min,
	[System.Xml.Serialization.XmlEnum("cm^3")]
	Cm3,
	[System.Xml.Serialization.XmlEnum("l")]
	L,
}

[Serializable()]
[System.Xml.Serialization.XmlType("factor", Namespace = "")]
public enum Factor
{
	[System.Xml.Serialization.XmlEnum("1E-2")]
	Item1E2,
}

[Serializable()]
[System.Xml.Serialization.XmlType("param", Namespace = "", AnonymousType = true)]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlRoot("param", Namespace = "")]
public partial class Param
{
	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlAttribute("index", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public byte Index { get; set; }

	[System.Xml.Serialization.XmlAttribute("label", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string? Label { get; set; }

	[System.Xml.Serialization.XmlAttribute("units", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public SiUnit Units { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Units-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Units property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool UnitsSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("multiplier", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public Factor Multiplier { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Multiplier-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Multiplier property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool MultiplierSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("instance", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public bool Instance { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Instance-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Instance property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool InstanceSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("enum", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string? Enum { get; set; }

	[System.Xml.Serialization.XmlAttribute("decimalPlaces", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public byte DecimalPlaces { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die DecimalPlaces-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the DecimalPlaces property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool DecimalPlacesSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("increment", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public float Increment { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Increment-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Increment property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool IncrementSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("minValue", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public float MinValue { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die MinValue-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the MinValue property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool MinValueSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("maxValue", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public float MaxValue { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die MaxValue-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the MaxValue property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool MaxValueSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("reserved", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public bool Reserved { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Reserved-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Reserved property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool ReservedSpecified { get; set; }

	/// <summary>
	/// <para xml:lang="en">Pattern: NaN.</para>
	/// </summary>
	[System.ComponentModel.DataAnnotations.RegularExpression("NaN")]
	[System.Xml.Serialization.XmlAttribute("default", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string? Default { get; set; }

	[System.Xml.Serialization.XmlText()]
	public string[]? Text { get; set; }
}

[Serializable()]
[System.Xml.Serialization.XmlType("deprecated", Namespace = "", AnonymousType = true)]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlRoot("deprecated", Namespace = "")]
public partial class Deprecated
{
	[System.Xml.Serialization.XmlElement("description")]
	public string? Description { get; set; }

	/// <summary>
	/// <para xml:lang="en">Pattern: (20)\d{2}-(0[1-9]|1[012]).</para>
	/// </summary>
	[System.ComponentModel.DataAnnotations.RegularExpression("(20)\\d{2}-(0[1-9]|1[012])")]
	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlAttribute("since", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string Since { get; set; } = string.Empty;

	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlAttribute("replaced_by", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string ReplacedBy { get; set; } = string.Empty;

	[System.Xml.Serialization.XmlText()]
	public string[]? Text { get; set; }
}

[Serializable()]
[System.Xml.Serialization.XmlType("wip", Namespace = "", AnonymousType = true)]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlRoot("wip", Namespace = "")]
public partial class Wip
{
	[System.Xml.Serialization.XmlElement("description")]
	public string? Description { get; set; }

	/// <summary>
	/// <para xml:lang="en">Pattern: (20)\d{2}-(0[1-9]|1[012]).</para>
	/// </summary>
	[System.ComponentModel.DataAnnotations.RegularExpression("(20)\\d{2}-(0[1-9]|1[012])")]
	[System.Xml.Serialization.XmlAttribute("since", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string? Since { get; set; }

	[System.Xml.Serialization.XmlText()]
	public string[]? Text { get; set; }
}

[Serializable()]
[System.Xml.Serialization.XmlType("field", Namespace = "", AnonymousType = true)]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlRoot("field", Namespace = "")]
public partial class Field
{
	[System.Xml.Serialization.XmlElement("description")]
	public string? Description { get; set; }

	/// <summary>
	/// <para xml:lang="en">Pattern: array\[[0-9]+\].</para>
	/// </summary>
	[System.ComponentModel.DataAnnotations.RegularExpression("array\\[[0-9]+\\]")]
	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlAttribute("type", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string Type { get; set; } = string.Empty;

	/// <summary>
	/// <para xml:lang="en">Pattern: [\w_]+.</para>
	/// </summary>
	[System.ComponentModel.DataAnnotations.RegularExpression("[\\w_]+")]
	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlAttribute("name", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string Name { get; set; } = string.Empty;

	[System.Xml.Serialization.XmlAttribute("print_format", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string? PrintFormat { get; set; }

	[System.Xml.Serialization.XmlAttribute("enum", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string? Enum { get; set; }

	[System.Xml.Serialization.XmlAttribute("display", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string? Display { get; set; }

	[System.Xml.Serialization.XmlAttribute("units", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public SiUnit Units { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Units-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Units property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool UnitsSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("increment", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public float Increment { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Increment-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Increment property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool IncrementSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("minValue", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public float MinValue { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die MinValue-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the MinValue property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool MinValueSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("maxValue", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public float MaxValue { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die MaxValue-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the MaxValue property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool MaxValueSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("multiplier", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public Factor Multiplier { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Multiplier-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Multiplier property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool MultiplierSpecified { get; set; }

	/// <summary>
	/// <para xml:lang="en">Pattern: NaN.</para>
	/// </summary>
	[System.ComponentModel.DataAnnotations.RegularExpression("NaN")]
	[System.Xml.Serialization.XmlAttribute("default", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string? Default { get; set; }

	[System.Xml.Serialization.XmlAttribute("instance", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public bool Instance { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Instance-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Instance property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool InstanceSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("invalid", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string? Invalid { get; set; }

	[System.Xml.Serialization.XmlText()]
	public string[] Text { get; set; } = Array.Empty<string>();
}

[Serializable()]
[System.Xml.Serialization.XmlType("entry", Namespace = "", AnonymousType = true)]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlRoot("entry", Namespace = "")]
public partial class Entry
{
	[System.Xml.Serialization.XmlElement("deprecated")]
	public Deprecated? Deprecated { get; set; }

	[System.Xml.Serialization.XmlElement("wip")]
	public Wip? Wip { get; set; }

	[System.Xml.Serialization.XmlElement("description")]
	public string? Description { get; set; }

	[System.Xml.Serialization.XmlIgnore()]
	private System.Collections.ObjectModel.Collection<Param> _param = new();
	[System.Xml.Serialization.XmlElement("param")]
	public System.Collections.ObjectModel.Collection<Param> Param
	{
		get => _param;
		private set => _param = value;
	}

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Param-Collection leer ist.</para>
	/// <para xml:lang="en">Gets a value indicating whether the Param collection is empty.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool ParamSpecified => Param.Count != 0;

	/// <summary>
	/// <para xml:lang="de">Initialisiert eine neue Instanz der <see cref = "Entry"/> Klasse.</para>
	/// <para xml:lang="en">Initializes a new instance of the <see cref = "Entry"/> class.</para>
	/// </summary>
	public Entry()
	{
		_param = new System.Collections.ObjectModel.Collection<Param>();
	}

	/// <summary>
	/// <para xml:lang="en">Pattern: 2\*\*\d{1,2}.</para>
	/// </summary>
	[System.ComponentModel.DataAnnotations.RegularExpression("2\\*\\*\\d{1,2}")]
	[System.Xml.Serialization.XmlAttribute("value", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string Value { get; set; } = string.Empty;

	/// <summary>
	/// <para xml:lang="en">Pattern: [\w_]+.</para>
	/// </summary>
	[System.ComponentModel.DataAnnotations.RegularExpression("[\\w_]+")]
	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlAttribute("name", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string Name { get; set; } = string.Empty;

	[System.Xml.Serialization.XmlAttribute("hasLocation", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public bool HasLocation { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die HasLocation-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the HasLocation property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool HasLocationSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("isDestination", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public bool IsDestination { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die IsDestination-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the IsDestination property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool IsDestinationSpecified { get; set; }

	[System.Xml.Serialization.XmlAttribute("missionOnly", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public bool MissionOnly { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die MissionOnly-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the MissionOnly property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool MissionOnlySpecified { get; set; }
}

[Serializable()]
[System.Xml.Serialization.XmlType("enum", Namespace = "", AnonymousType = true)]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlRoot("enum", Namespace = "")]
public partial class Enum
{
	[System.Xml.Serialization.XmlElement("deprecated")]
	public Deprecated? Deprecated { get; set; }

	[System.Xml.Serialization.XmlElement("wip")]
	public Wip? Wip { get; set; }

	[System.Xml.Serialization.XmlElement("description")]
	public string? Description { get; set; }

	[System.Xml.Serialization.XmlIgnore()]
	private System.Collections.ObjectModel.Collection<Entry> _entry = new();
	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlElement("entry")]
	public System.Collections.ObjectModel.Collection<Entry> Entry
	{
		get => _entry;
		private set => _entry = value;
	}

	/// <summary>
	/// <para xml:lang="de">Initialisiert eine neue Instanz der <see cref = "Enum"/> Klasse.</para>
	/// <para xml:lang="en">Initializes a new instance of the <see cref = "Enum"/> class.</para>
	/// </summary>
	public Enum()
	{
		_entry = new System.Collections.ObjectModel.Collection<Entry>();
	}

	/// <summary>
	/// <para xml:lang="en">Pattern: [\w_]+.</para>
	/// </summary>
	[System.ComponentModel.DataAnnotations.RegularExpression("[\\w_]+")]
	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlAttribute("name", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string Name { get; set; } = string.Empty;

	[System.Xml.Serialization.XmlAttribute("bitmask", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public bool Bitmask { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Bitmask-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Bitmask property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool BitmaskSpecified { get; set; }
}

[Serializable()]
[System.Xml.Serialization.XmlType("message", Namespace = "", AnonymousType = true)]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlRoot("message", Namespace = "")]
public partial class Message
{
	[System.Xml.Serialization.XmlElement("deprecated")]
	public Deprecated? Deprecated { get; set; }

	[System.Xml.Serialization.XmlElement("wip")]
	public Wip? Wip { get; set; }

	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlElement("description")]
	public string Description { get; set; } = string.Empty;

	[System.Xml.Serialization.XmlIgnore()]
	private System.Collections.ObjectModel.Collection<Field> _field = new();
	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlElement("field")]
	public System.Collections.ObjectModel.Collection<Field> Field
	{
		get => _field;
		private set => _field = value;
	}

	/// <summary>
	/// <para xml:lang="de">Initialisiert eine neue Instanz der <see cref = "Message"/> Klasse.</para>
	/// <para xml:lang="en">Initializes a new instance of the <see cref = "Message"/> class.</para>
	/// </summary>
	public Message()
	{
		_field = new System.Collections.ObjectModel.Collection<Field>();
	}

	[System.Xml.Serialization.XmlElement("extensions")]
	public object? Extensions { get; set; }

	/// <summary>
	/// <para xml:lang="en">Maximum inclusive value: 16777215.</para>
	/// </summary>
	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlAttribute("id", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public uint Id { get; set; }

	/// <summary>
	/// <para xml:lang="en">Pattern: [\w_]+.</para>
	/// </summary>
	[System.ComponentModel.DataAnnotations.RegularExpression("[\\w_]+")]
	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlAttribute("name", Namespace = "", Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
	public string Name { get; set; } = string.Empty;
}

[Serializable()]
[System.Xml.Serialization.XmlType("enums", Namespace = "", AnonymousType = true)]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlRoot("enums", Namespace = "")]
public partial class Enums
{
	[System.Xml.Serialization.XmlIgnore()]
	private System.Collections.ObjectModel.Collection<Enum> _enum = new();
	[System.Xml.Serialization.XmlElement("enum")]
	public System.Collections.ObjectModel.Collection<Enum> Enum
	{
		get => _enum;
		private set => _enum = value;
	}

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Enum-Collection leer ist.</para>
	/// <para xml:lang="en">Gets a value indicating whether the Enum collection is empty.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool EnumSpecified => Enum.Count != 0;

	/// <summary>
	/// <para xml:lang="de">Initialisiert eine neue Instanz der <see cref = "Enums"/> Klasse.</para>
	/// <para xml:lang="en">Initializes a new instance of the <see cref = "Enums"/> class.</para>
	/// </summary>
	public Enums()
	{
		_enum = new System.Collections.ObjectModel.Collection<Enum>();
	}
}

[Serializable()]
[System.Xml.Serialization.XmlType("messages", Namespace = "", AnonymousType = true)]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlRoot("messages", Namespace = "")]
public partial class Messages
{
	[System.Xml.Serialization.XmlIgnore()]
	private System.Collections.ObjectModel.Collection<Message> _message = new();
	[System.Xml.Serialization.XmlElement("message")]
	public System.Collections.ObjectModel.Collection<Message> Message
	{
		get => _message;
		private set => _message = value;
	}

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Message-Collection leer ist.</para>
	/// <para xml:lang="en">Gets a value indicating whether the Message collection is empty.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool MessageSpecified => Message.Count != 0;

	/// <summary>
	/// <para xml:lang="de">Initialisiert eine neue Instanz der <see cref = "Messages"/> Klasse.</para>
	/// <para xml:lang="en">Initializes a new instance of the <see cref = "Messages"/> class.</para>
	/// </summary>
	public Messages()
	{
		_message = new System.Collections.ObjectModel.Collection<Message>();
	}
}

[Serializable()]
[System.Xml.Serialization.XmlType("mavlink", Namespace = "", AnonymousType = true)]
[System.ComponentModel.DesignerCategory("code")]
[System.Xml.Serialization.XmlRoot("mavlink", Namespace = "")]
public partial class Mavlink
{
	[System.Xml.Serialization.XmlIgnore()]
	private System.Collections.ObjectModel.Collection<string> _include = new();
	[System.Xml.Serialization.XmlElement("include")]
	public System.Collections.ObjectModel.Collection<string> Include
	{
		get => _include;
		private set => _include = value;
	}

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Include-Collection leer ist.</para>
	/// <para xml:lang="en">Gets a value indicating whether the Include collection is empty.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool IncludeSpecified => Include.Count != 0;

	/// <summary>
	/// <para xml:lang="de">Initialisiert eine neue Instanz der <see cref = "Mavlink"/> Klasse.</para>
	/// <para xml:lang="en">Initializes a new instance of the <see cref = "Mavlink"/> class.</para>
	/// </summary>
	public Mavlink()
	{
		_include = new System.Collections.ObjectModel.Collection<string>();
		_enums = new System.Collections.ObjectModel.Collection<Enum>();
		_messages = new System.Collections.ObjectModel.Collection<Message>();
	}

	[System.Xml.Serialization.XmlElement("version")]
	public byte Version { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Version-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Version property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool VersionSpecified { get; set; }

	[System.Xml.Serialization.XmlElement("dialect")]
	public byte Dialect { get; set; }

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Dialect-Eigenschaft spezifiziert ist, oder legt diesen fest.</para>
	/// <para xml:lang="en">Gets or sets a value indicating whether the Dialect property is specified.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool DialectSpecified { get; set; }

	[System.Xml.Serialization.XmlIgnore()]
	private System.Collections.ObjectModel.Collection<Enum> _enums = new();
	[System.Xml.Serialization.XmlArray("enums")]
	[System.Xml.Serialization.XmlArrayItem("enum")]
	public System.Collections.ObjectModel.Collection<Enum> Enums
	{
		get => _enums;
		private set => _enums = value;
	}

	/// <summary>
	/// <para xml:lang="de">Ruft einen Wert ab, der angibt, ob die Enums-Collection leer ist.</para>
	/// <para xml:lang="en">Gets a value indicating whether the Enums collection is empty.</para>
	/// </summary>
	[System.Xml.Serialization.XmlIgnore()]
	public bool EnumsSpecified => Enums.Count != 0;

	[System.Xml.Serialization.XmlIgnore()]
	private System.Collections.ObjectModel.Collection<Message> _messages = new();
	[System.ComponentModel.DataAnnotations.Required(AllowEmptyStrings = true)]
	[System.Xml.Serialization.XmlArray("messages")]
	[System.Xml.Serialization.XmlArrayItem("message")]
	public System.Collections.ObjectModel.Collection<Message> Messages
	{
		get => _messages;
		private set => _messages = value;
	}

	[System.Xml.Serialization.XmlAttribute("file")]
	public string? File { get; set; }
}
