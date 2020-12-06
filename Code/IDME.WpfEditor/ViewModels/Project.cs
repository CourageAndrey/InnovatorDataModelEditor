using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;

using IDME.WpfEditor.Commands;

namespace IDME.WpfEditor.ViewModels
{
	public class Project : INotifyPropertyChanged
	{
		#region Properties

		public string FileName
		{ get; private set; }

		public string WindowTitle
		{
			get
			{
				string changesSign = HasChanges ? "*" : string.Empty;
				return $"Innovator Data Model Editor : {FileName}{changesSign}";
			}
		}

		public ICollection<ItemType> ItemTypes
		{ get; }

		public ICollection<Item> Items
		{ get; }

		public event EventHandler<Item> ItemAdded;
		public event EventHandler<Item> ItemRemoved;

		private void raiseAdded(Item item)
		{
			var handler = Volatile.Read(ref ItemAdded);
			if (handler != null)
			{
				handler(this, item);
			}
		}

		private void raiseRemoved(Item item)
		{
			var handler = Volatile.Read(ref ItemRemoved);
			if (handler != null)
			{
				handler(this, item);
			}
		}

		#endregion

		#region Constructors

		private void onItemChanged(object sender, PropertyChangedEventArgs e)
		{
			if (_suppressHistoryChanges) return;

			if (e.PropertyName == nameof(Item.Left) || e.PropertyName == nameof(Item.Top))
			{
				var moveItemCommand = _currentEditPointer >= 0 ? _editHistory[_currentEditPointer] as MoveItemCommand : null;
				if (moveItemCommand != null && moveItemCommand.Item == sender)
				{
					moveItemCommand.UpdatePosition(moveItemCommand.Item.Left, moveItemCommand.Item.Top);
					// no need to call raiseChanged(); because non-empty history means that project has already been marked as changed
				}
				else
				{
					Item item = (Item) sender;
					moveItemCommand = new MoveItemCommand(this, item, item.Left, item.Top);
					PerformCommand(moveItemCommand, false);
					// no need to call raiseChanged(); because PerformCommand does
				}
			}
			else
			{
				raiseChanged();
			}
		}

		public Project(string fileName, IEnumerable<ItemType> itemTypes, IEnumerable<Item> items)
		{
			FileName = fileName;

			ItemTypes = new List<ItemType>(itemTypes);

			var eventCollection = new Helpers.EventCollection<Item>();
			eventCollection.ItemAdded += (collection, item) =>
			{
				raiseAdded(item);
				raiseChanged();
				item.PropertyChanged += onItemChanged;
			};
			eventCollection.ItemRemoved += (collection, item) =>
			{
				raiseRemoved(item);
				raiseChanged();
				item.PropertyChanged -= onItemChanged;
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

		#region History

		private readonly List<IEditCommand> _editHistory = new List<IEditCommand>();
		private int _currentEditPointer = -1;
		private bool _suppressHistoryChanges;

		public bool CanUndo
		{ get { return _editHistory.Count > 0 && _currentEditPointer >= 0; } }

		public bool CanRedo
		{ get { return _editHistory.Count > 0 && _currentEditPointer < _editHistory.Count - 1; } }

		public void PerformCommand(IEditCommand command, bool performBody = true)
		{
			if (performBody)
			{
				command.Apply();
			}

			_editHistory.RemoveRange(_currentEditPointer + 1, _editHistory.Count - _currentEditPointer - 1);

			_currentEditPointer = _editHistory.Count;
			_editHistory.Add(command);
			raiseChanged();
		}

		public void Undo()
		{
			_suppressHistoryChanges = true;
			try
			{
				_editHistory[_currentEditPointer].Rollback();
				_currentEditPointer--;
				raiseChanged();
			}
			finally
			{
				_suppressHistoryChanges = false;
			}
		}

		public void Redo()
		{
			_suppressHistoryChanges = true;
			try
			{
				_currentEditPointer++;
				_editHistory[_currentEditPointer].Apply();
				raiseChanged();
			}
			finally
			{
				_suppressHistoryChanges = false;
			}
		}

		#endregion

		#region Implementation of INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		public bool HasChanges
		{ get; private set; }

		public bool CanSave
		{ get { return !HasChanges; } }

		public bool NeedToMoveItem
		{ get { return _suppressHistoryChanges; } }

		private void raiseChanged(string propertyName = null)
		{
			HasChanges = true;

			var handler = Volatile.Read(ref PropertyChanged);
			handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
