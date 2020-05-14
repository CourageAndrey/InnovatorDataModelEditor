using System.Xml.Serialization;

namespace IDME.WpfEditor.Xml
{
	[XmlType]
	public class ItemProperty
	{
		[XmlAttribute]
		public string Property
		{ get; set; }

		[XmlAttribute]
		public string Value
		{ get; set; }

		public ItemProperty()
		{ }

		public ItemProperty(string property, string value)
		{
			Property = property;
			Value = value;
		}
	}
}
