using System.Collections.Generic;
using System.Linq;

namespace IDME.WpfEditor.ViewModels
{
	public class ItemType
	{
		#region Properties

		public string Name
		{ get; set; }

		public bool IsRelationship
		{ get; set; }

		public IList<Property> Properties
		{ get; }

		public IList<ItemType> Relationships
		{ get; }

		#endregion

		#region Constructors

		public ItemType()
			: this(string.Empty, false, new Property[0], new ItemType[0])
		{ }

		public ItemType(string name, bool isRelationship, IEnumerable<Property> properties, IEnumerable<ItemType> relationships)
		{
			Name = name;
			IsRelationship = isRelationship;
			Properties = new List<Property>(properties);
			Relationships = new List<ItemType>(relationships);
		}

		#endregion

		#region List

		public static readonly ItemType PropertyItemType;
		public static readonly ItemType RelationshipTypeItemType;
		public static readonly ItemType ItemTypeItemType;

		public static readonly IDictionary<string, ItemType> DefaultItemTypes;

		static ItemType()
		{
			PropertyItemType = new ItemType(
				"Property",
				true,
				new[]
				{
					new Property
					{
						Name = "name",
						DataSourceType = null,
					},
					new Property
					{
						Name = "data_type",
						DataSourceType = null,
					},
					new Property
					{
						Name = "stored_length",
						DataSourceType = null,
					},
					new Property
					{
						Name = "data_source",
						DataSourceType = null,
					},
				},
				new ItemType[0]);

			RelationshipTypeItemType = new ItemType(
				"RelationshipType",
				true,
				new[]
				{
					new Property
					{
						Name = "related_id",
						DataSourceType = null,
					}
				},
				new ItemType[0]);

			ItemTypeItemType = new ItemType(
				"ItemType",
				false,
				new[]
				{
					new Property
					{
						Name = "name",
						DataSourceType = null,
					}
				},
				new[]
				{
					PropertyItemType,
					RelationshipTypeItemType
				});

			RelationshipTypeItemType.Properties.Single( /*"related_id"*/).DataSourceType = ItemTypeItemType;

			DefaultItemTypes = new Dictionary<string, ItemType>
			{
				{ ItemTypeItemType.Name, ItemTypeItemType },
				{ PropertyItemType.Name, PropertyItemType },
				{ RelationshipTypeItemType.Name, RelationshipTypeItemType },
			};
		}

		#endregion
	}
}
