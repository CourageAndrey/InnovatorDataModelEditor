using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace IDME.WpfEditor.ViewModels
{
	public class Item
	{
		#region Properties

		private double _left;
		private double _top;

		public ItemType ItemType
		{ get; }

		public double Left
		{
			get => _left;
			set
			{
				_left = value;
				raiseChanged();
			}
		}

		public double Top
		{
			get => _top;
			set
			{
				_top = value;
				raiseChanged();
			}
		}

		public ICollection<PropertyValue> Properties
		{ get; }

		public ICollection<Item> Relationships
		{ get; }

		public event EventHandler Changed;

		private void raiseChanged()
		{
			var handler = Volatile.Read(ref Changed);
			if (handler != null)
			{
				handler(this, EventArgs.Empty);
			}
		}

		#endregion

		#region Constructors

		public Item(ItemType itemType, double left, double top)
			: this(itemType, left, top, itemType.Properties.Select(property => new PropertyValue(property.Name, property.Name)))
		{ }

		public Item(ItemType itemType, double left, double top, IEnumerable<PropertyValue> properties)
		{
			ItemType = itemType;
			Left = left;
			Top = top;

			Properties = properties.ToList().AsReadOnly();
			foreach (var property in Properties)
			{
				property.ValueChanged += (sender, args) => { raiseChanged(); };
			}

			Relationships = new List<Item>();
		}

		#endregion
	}
}
