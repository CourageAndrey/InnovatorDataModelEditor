using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace IDME.WpfEditor.Xml
{
	[XmlType, XmlRoot]
	public class Project
	{
		#region Property

		[XmlArray]
		public List<ItemType> ItemTypes
		{ get; set; } = new List<ItemType>();

		[XmlArray]
		public List<Item> Items
		{ get; set; } = new List<Item>();

		[XmlArray]
		public List<Relationship> Relationships
		{ get; set; } = new List<Relationship>();

		#endregion

		#region Constructor

		public Project()
		{ }

		public Project(ViewModels.Project viewModel)
		{
			foreach (var itemType in viewModel.ItemTypes)
			{
				ItemTypes.Add(new ItemType
				{
					Name = itemType.Name,
					IsRelationship = itemType.IsRelationship,
					Properties = itemType.Properties.Select(property => new ItemTypeProperty
					{
						Name = property.Name,
						DataSource = property.DataSourceType?.Name,
					}).ToList(),
					Relationships = itemType.Relationships.Select(relationship => relationship.Name).ToList(),
				});
			}

			var itemIndexes = new Dictionary<ViewModels.Item, int>();
			foreach (var item in viewModel.Items)
			{
				var itemXml = new Item
				{
					ItemType = item.ItemType.Name,
					Left = item.Left,
					Top = item.Top,
					Properties = item.Properties.Select(propertyValue => new ItemProperty(propertyValue.Name, propertyValue.Value)).ToList(),
				};
				itemIndexes[item] = Items.Count;
				Items.Add(itemXml);
			}

			foreach (var item in viewModel.Items)
			{
				foreach (var relationship in item.Relationships)
				{
					Relationships.Add(new Relationship
					{
						SourceIndex = itemIndexes[item],
						RelatedIndex = itemIndexes[relationship],
					});
				}
			}
		}

		#endregion

		#region Save/Load

		private static readonly XmlSerializer _xmlSerializer = new XmlSerializer(typeof(Project));

		public static Project Load(string fileName)
		{
			using (var xmlReader = XmlReader.Create(fileName))
			{
				return (Project) _xmlSerializer.Deserialize(xmlReader);
			}
		}

		public void Save(string fileName)
		{
			var xmlDocument = new XmlDocument();
			using (var writer = new StringWriter())
			{
				_xmlSerializer.Serialize(writer, this);
				xmlDocument.LoadXml(writer.ToString());
				xmlDocument.Save(fileName);
			}
		}

		#endregion
	}
}
