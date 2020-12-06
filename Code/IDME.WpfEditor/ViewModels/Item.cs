using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace IDME.WpfEditor.ViewModels
{
	public class Item : INotifyPropertyChanged
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
				property.PropertyChanged += (sender, args) => { raiseChanged("#" + ((PropertyValue) sender).Name); };
			}

			Relationships = new List<Item>();
		}

		#endregion

		#region Implementation of INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		private void raiseChanged([CallerMemberName] string propertyName = null)
		{
			var handler = Volatile.Read(ref PropertyChanged);
			handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
