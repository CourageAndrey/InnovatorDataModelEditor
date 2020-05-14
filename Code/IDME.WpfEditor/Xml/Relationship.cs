using System.Xml.Serialization;

namespace IDME.WpfEditor.Xml
{
	[XmlType]
	public class Relationship
	{
		[XmlAttribute]
		public int SourceIndex
		{ get; set; }

		[XmlAttribute]
		public int ItemTypeIndex
		{ get; set; }

		[XmlAttribute]
		public int RelatedIndex
		{ get; set; }
	}
}
