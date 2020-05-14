using System.Xml.Serialization;

namespace IDME.WpfEditor.Xml
{
	[XmlType]
	public class ItemTypeProperty
	{
		[XmlAttribute]
		public string Name
		{ get; set; }

		[XmlAttribute]
		public string DataSource
		{ get; set; }
	}
}
