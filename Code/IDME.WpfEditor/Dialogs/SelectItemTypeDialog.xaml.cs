using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using IDME.WpfEditor.ViewModels;

namespace IDME.WpfEditor.Dialogs
{
	public partial class SelectItemTypeDialog
	{
		public SelectItemTypeDialog()
		{
			InitializeComponent();
		}

		public ICollection<ItemType> ItemTypes
		{
			get { return itemTypesGrid.ItemsSource as ICollection<ItemType>; }
			set { itemTypesGrid.ItemsSource = value; }
		}

		public ItemType SelectedItemType
		{
			get { return itemTypesGrid.SelectedItem as ItemType; }
			set { itemTypesGrid.SelectedItem = SelectedItemType; }
		}

		private void selectedItemDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (SelectedItemType != null)
			{
				DialogResult = true;
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

		private void selectedItemTypesChanged(object sender, SelectedCellsChangedEventArgs e)
		{
			updateButtons();
		}

		private void windowLoaded(object sender, RoutedEventArgs e)
		{
			updateButtons();
		}

		private void updateButtons()
		{
			buttonOk.IsEnabled = SelectedItemType != null;
		}
	}
}
