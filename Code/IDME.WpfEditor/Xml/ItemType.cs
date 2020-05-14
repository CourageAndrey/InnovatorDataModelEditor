using System.Collections.Generic;
using System.Xml.Serialization;

namespace IDME.WpfEditor.Xml
{
	[XmlType]
	public class ItemType
	{
		[XmlAttribute]
		public string Name
		{ get; set; }

		[XmlAttribute]
		public bool IsRelationship
		{ get; set; }

		[XmlArray]
		public List<ItemTypeProperty> Properties
		{ get; set; } = new List<ItemTypeProperty>();

		[XmlArray]
		public List<string> Relationships
		{ get; set; } = new List<string>();
	}
}
