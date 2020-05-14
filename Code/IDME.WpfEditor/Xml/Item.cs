using System.Collections.Generic;
using System.Xml.Serialization;

namespace IDME.WpfEditor.Xml
{
	[XmlType]
	public class Item
	{
		[XmlAttribute]
		public string ItemType
		{ get; set; }

		[XmlAttribute]
		public double Left
		{ get; set; }

		[XmlAttribute]
		public double Top
		{ get; set; }

		[XmlArray]
		public List<ItemProperty> Properties
		{ get; set; } = new List<ItemProperty>();
	}
}
