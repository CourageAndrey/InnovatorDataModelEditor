using System.Collections.Generic;
using System.Windows.Controls;

namespace IDME.WpfEditor.Dialogs
{
	public partial class ItemTypesDialog
	{
		public ItemTypesDialog()
		{
			InitializeComponent();
		}

		private ICollection<ViewModels.ItemType> _itemTypes;

		public ICollection<ViewModels.ItemType> ItemTypes
		{
			get => _itemTypes;
			set
			{
				comboBoxPropertyDataSource.ItemsSource = itemTypesGrid.ItemsSource = _itemTypes = value;
			}
		}

		private void newRelationshipsInitializing(object sender, InitializingNewItemEventArgs e)
		{
			var relationshipType = (ViewModels.ItemType) e.NewItem;
			relationshipType.IsRelationship = true;
			_itemTypes.Add(relationshipType);
			itemTypesGrid.Items.Refresh();
		}
	}
}
