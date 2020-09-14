using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace IDME.WpfEditor.Dialogs
{
	public partial class SelectItemsDialog
	{
		private ICollection<string> _properties;

		public SelectItemsDialog()
		{
			InitializeComponent();
		}

		public ICollection<string> Properties
		{
			get { return _properties; }
			set
			{
				_properties = value;

				dataGrid.ItemsSource = new List<Aras.IOM.Item>();

				dataGrid.Columns.Clear();
				dataGrid.Columns.Add(new DataGridCheckBoxColumn
				{
					Header = string.Empty,
					Binding = new Binding("IsChecked") { Mode = BindingMode.TwoWay },
				});
				foreach (var property in _properties)
				{
					dataGrid.Columns.Add(new DataGridTextColumn
					{
						Header = property,
						Binding = new Binding($"Values[{property}]") { Mode = BindingMode.OneTime },
					});
				}
			}
		}

		public ICollection<Aras.IOM.Item> Items
		{
			get { return dataGrid.ItemsSource.OfType<CheckedItemWrapper>().Select(item => item.Item).ToList(); }
			set { dataGrid.ItemsSource = value.Select(item => new CheckedItemWrapper(_properties, item)).ToList(); }
		}

		public ICollection<Aras.IOM.Item> SelectedItems
		{
			get { return dataGrid.ItemsSource.OfType<CheckedItemWrapper>().Where(item => item.IsChecked).Select(item => item.Item).ToList(); }
			set
			{
				var hashSet = new HashSet<Aras.IOM.Item>(value);
				foreach (var checkedWrapper in dataGrid.ItemsSource.OfType<CheckedItemWrapper>())
				{
					checkedWrapper.IsChecked = hashSet.Contains(checkedWrapper.Item);
				}
			}
		}

		private void okClick(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
		}

		private void cancelClick(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
		}

		private class CheckedItemWrapper
		{
			public Aras.IOM.Item Item
			{ get; }

			public bool IsChecked
			{ get; set; }

			public Dictionary<string, string> Values
			{ get; }

			public CheckedItemWrapper(ICollection<string> properties, Aras.IOM.Item item)
			{
				Item = item;
				Values = properties.ToDictionary(
					property => property,
					property => item.getProperty(property));
			}
		}
	}
}
