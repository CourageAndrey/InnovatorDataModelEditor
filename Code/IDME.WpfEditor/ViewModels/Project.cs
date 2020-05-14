using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IDME.WpfEditor.ViewModels
{
	public class Project
	{
		#region Properties

		public string FileName
		{ get; set; }

		public ICollection<ItemType> ItemTypes
		{ get; }

		public ICollection<Item> Items
		{ get; }

		public bool HasChanges
		{ get; private set; }

		public event EventHandler Changed;

		private void raiseChanged()
		{
			HasChanges = true;

			var handler = Volatile.Read(ref Changed);
			if (handler != null)
			{
				handler(this, EventArgs.Empty);
			}
		}

		#endregion

		#region Constructors

		private void onItemOnChanged(object sender, EventArgs args)
		{
			raiseChanged();
		}

		public Project(string fileName, IEnumerable<ItemType> itemTypes, IEnumerable<Item> items)
		{
			FileName = fileName;

			ItemTypes = new List<ItemType>(itemTypes);

			var eventCollection = new Helpers.EventCollection<Item>();
			eventCollection.ItemAdded += (collection, item) =>
			{
				raiseChanged();
				item.Changed += onItemOnChanged;
			};
			eventCollection.ItemRemoved += (collection, item) =>
			{
				raiseChanged();
				item.Changed -= onItemOnChanged;
			};
			Items = eventCollection;
			foreach (var item in items)
			{
				Items.Add(item);
			}

			HasChanges = false;
		}

		public Project()
			: this(string.Empty, ItemType.DefaultItemTypes.Values, new[] { new Item(ItemType.ItemTypeItemType, 50, 50) })
		{ }

		#endregion

		#region Save/Load

		public static Project Load(string fileName)
		{
			var snapshot = Xml.Project.Load(fileName);

			var itemTypes = new Dictionary<string, ItemType>();
			foreach (var xmlItemType in snapshot.ItemTypes)
			{
				var itemType = new ItemType(
					xmlItemType.Name,
					xmlItemType.IsRelationship,
					xmlItemType.Properties.Select(property => new Property
					{
						Name = property.DataSource,
					}),
					new ItemType[0]);
				itemTypes.Add(itemType.Name, itemType);
			}

			foreach (var xmlItemType in snapshot.ItemTypes)
			{
				var itemType = itemTypes[xmlItemType.Name];

				foreach (var property in xmlItemType.Properties)
				{
					if (!string.IsNullOrEmpty(property.DataSource))
					{
						itemType.Properties.First(p => p.Name == property.Name).DataSourceType = itemTypes[property.DataSource];
					}
				}

				foreach (string relationship in xmlItemType.Relationships)
				{
					itemType.Relationships.Add(itemTypes[relationship]);
				}
			}

			var items = new Dictionary<Xml.Item, Item>();
			foreach (var xmlItem in snapshot.Items)
			{
				var item = new Item(
					itemTypes[xmlItem.ItemType],
					xmlItem.Left,
					xmlItem.Top,
					xmlItem.Properties.Select(property => new PropertyValue(property.Property, property.Value)));
				items[xmlItem] = item;
			}
			foreach (var relationship in snapshot.Relationships)
			{
				var sourceItem = items[snapshot.Items[relationship.SourceIndex]];
				var relatedItem = items[snapshot.Items[relationship.RelatedIndex]];
				sourceItem.Relationships.Add(relatedItem);
			}

			return new Project(fileName, itemTypes.Values, items.Values);
		}

		public void Save(string fileName)
		{
			var snapshot = new Xml.Project(this);
			snapshot.Save(fileName);
			FileName = fileName;
			HasChanges = false;
		}

		#endregion
	}
}
