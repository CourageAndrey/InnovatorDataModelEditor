using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using IDME.WpfEditor.Dialogs;
using IDME.WpfEditor.ViewModels;

namespace IDME.WpfEditor.Controls
{
	public delegate void ItemControlAddRelationshipEventHandler(ItemControl sender, ItemType itemType);

	public partial class ItemControl
	{
		public ItemControl()
		{
			InitializeComponent();
		}

		public Item Item
		{
			get { return _item; }
			set
			{
				_item = value;

				_header.Header = value.ItemType.Name;

				_propertiesGrid.RowDefinitions.Clear();
				int rowNumber = 0;
				foreach (var property in value.Properties)
				{
					var row = new RowDefinition { Height = GridLength.Auto };
					_propertiesGrid.RowDefinitions.Add(row);

					var propertyTitleControl = new TextBlock
					{
						Text = property.Name,
						Margin = new Thickness(5, 2, 5, 2),
					};
					propertyTitleControl.SetValue(Grid.RowProperty, rowNumber);
					propertyTitleControl.SetValue(Grid.ColumnProperty, 0);
					_propertiesGrid.Children.Add(propertyTitleControl);

					var propertyValueControl = new TextBox
					{
						Margin = new Thickness(1),
					};
					propertyValueControl.SetValue(Grid.RowProperty, rowNumber);
					propertyValueControl.SetValue(Grid.ColumnProperty, 1);

					propertyValueControl.DataContext = property;
					propertyValueControl.SetBinding(TextBox.TextProperty, new Binding("Value")
					{
						Mode = BindingMode.TwoWay,
						UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
					});

					_propertiesGrid.Children.Add(propertyValueControl);

					rowNumber++;
				}
			}
		}

		private Item _item;

		public event EventHandler OnDeleteRequest;
		public event ItemControlAddRelationshipEventHandler OnAddRelationshipRequest;

		private void deleteItemMenuClick(object sender, RoutedEventArgs e)
		{
			var handler = Volatile.Read(ref OnDeleteRequest);
			if (handler != null)
			{
				handler(this, EventArgs.Empty);
			}
		}

		private void addRelationshipMenuClick(object sender, RoutedEventArgs e)
		{
			var itemTypesDialog = new SelectItemTypeDialog
			{
				ItemTypes = _item.ItemType.Relationships,
			};
			if (itemTypesDialog.ShowDialog() == true)
			{
				var handler = Volatile.Read(ref OnAddRelationshipRequest);
				if (handler != null)
				{
					handler(this, itemTypesDialog.SelectedItemType);
				}
			}
		}

		#region Drag&Drop

		private Point _relativeMousePosition;
		private FrameworkElement _parentFrameworkElement;

		private void onMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				_parentFrameworkElement = (FrameworkElement) Parent;
				_relativeMousePosition = e.GetPosition(this);

				MouseMove += onDragMove;

				Mouse.Capture(this);
			}
		}

		private void onMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				MouseMove -= onDragMove;

				updatePosition(e);

				Mouse.Capture(null);
				_parentFrameworkElement = null;
			}
		}

		private void onDragMove(object sender, MouseEventArgs e)
		{
			updatePosition(e);
		}

		private void updatePosition(MouseEventArgs e)
		{
			var point = e.GetPosition(_parentFrameworkElement);

			double x = point.X - _relativeMousePosition.X;
			double y = point.Y - _relativeMousePosition.Y;

			Canvas.SetLeft(this, x);
			Canvas.SetTop(this, y);

			if (_item != null)
			{
				_item.Left = x;
				_item.Top = y;
			}
		}

		#endregion
	}
}
