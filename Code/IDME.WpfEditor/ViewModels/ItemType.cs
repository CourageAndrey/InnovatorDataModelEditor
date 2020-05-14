using System.Collections.Generic;

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

		public static readonly ItemType PropertyItemType = new ItemType("Property", true, new Property[0], new ItemType[0]);

		public static readonly ItemType RelationshipTypeItemType = new ItemType("RelationshipType", true, new Property[0], new ItemType[0]);

		public static readonly ItemType ItemTypeItemType = new ItemType(
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

		public static readonly IDictionary<string, ItemType> DefaultItemTypes = new Dictionary<string, ItemType>
		{
			{ ItemTypeItemType.Name, ItemTypeItemType },
			{ PropertyItemType.Name, PropertyItemType },
			{ RelationshipTypeItemType.Name, RelationshipTypeItemType },
		};

		#endregion
	}
}
