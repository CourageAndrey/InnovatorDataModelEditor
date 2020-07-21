using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using IDME.WpfEditor.Controls;
using IDME.WpfEditor.Dialogs;
using IDME.WpfEditor.Helpers;
using IDME.WpfEditor.ViewModels;

using Microsoft.Win32;

namespace IDME.WpfEditor
{
	public partial class MainWindow
	{
		#region Initialization

		public MainWindow()
		{
			InitializeComponent();
		}

		private void onWindowLoaded(object sender, RoutedEventArgs e)
		{
			setModel(createNewProject());
		}

		private void onWindowClosing(object sender, CancelEventArgs e)
		{
			if (!canProceedAfterSave())
			{
				e.Cancel = true;
			}
		}

		#endregion

		#region View model data

		private Project _project;

		private void projectChanged(object sender, EventArgs e)
		{
			updateWindowTitle();
		}

		private void setModel(Project project)
		{
			if (_project != null)
			{
				_project.Changed -= projectChanged;
			}
			_project = project;
			if (_project != null)
			{
				_project.Changed += projectChanged;
			}

			_viewScreen.Children.Clear();
			var itemControls = new Dictionary<Item, ItemControl>();
			foreach (var item in project.Items)
			{
				itemControls[item] = displayItem(item);
			}

			foreach (var item in project.Items)
			{
				foreach (var relationship in item.Relationships)
				{
					displayRelationship(itemControls[item], itemControls[relationship]);
				}
			}

			updateWindowTitle();
		}

		private void updateWindowTitle()
		{
			string changesSign = _project.HasChanges ? "*" : string.Empty;
			Title = $"Innovator Data Model Editor : {_project.FileName}{changesSign}";
		}

		private ItemControl displayItem(Item item)
		{
			var itemControl = new ItemControl
			{
				Item = item,
			};
			Canvas.SetLeft(itemControl, item.Left);
			Canvas.SetTop(itemControl, item.Top);
			itemControl.OnDeleteRequest += itemControlDeleteRequest;
			itemControl.OnAddRelationshipRequest += itemControlAddRelationshipRequest;
			_viewScreen.Children.Add(itemControl);
			_incomingRelationships[itemControl.Item] = new List<Relationship>();
			_outgoingRelationships[itemControl.Item] = new List<Relationship>();
			item.Changed += onItemOnChanged;
			_allItemControls[item] = itemControl;
			return itemControl;
		}

		private void onItemOnChanged(object sender, EventArgs args)
		{
			foreach (var relationship in _incomingRelationships[(Item) sender])
			{
				relationship.UpdateConnector();
			}
			foreach (var relationship in _outgoingRelationships[(Item) sender])
			{
				relationship.UpdateConnector();
			}
		}

		private Polyline displayRelationship(ItemControl itemControl, ItemControl relationshipControl)
		{
			var connector = new Polyline
			{
				Stroke = Brushes.Blue,
				StrokeThickness = 2,
			};
			_viewScreen.Children.Add(connector);
			var relationship = new Relationship(itemControl, relationshipControl, connector);
			_incomingRelationships[relationshipControl.Item].Add(relationship);
			_outgoingRelationships[itemControl.Item].Add(relationship);
			relationship.UpdateConnector();
			return connector;
		}

		private class Relationship
		{
			public ItemControl Source { get; }

			public ItemControl Related { get; }

			public Polyline Connector { get; }

			public Relationship(ItemControl source, ItemControl related, Polyline connector)
			{
				Source = source;
				Related = related;
				Connector = connector;
			}

			public void UpdateConnector()
			{
				Source.UpdateLayout();
				Related.UpdateLayout();

				double leftX = Source.Item.Left + Source.ActualWidth / 2;
				double rightX = Related.Item.Left;
				double topY = Source.Item.Top + Source.ActualHeight;
				double bottomY = Related.Item.Top + Related.ActualHeight / 2;

				Connector.Points.Clear();
				Connector.Points.Add(new Point(leftX, topY));
				Connector.Points.Add(new Point(leftX, bottomY));
				Connector.Points.Add(new Point(rightX, bottomY));
			}
		}

		private readonly IDictionary<Item, List<Relationship>> _incomingRelationships = new Dictionary<Item, List<Relationship>>();
		private readonly IDictionary<Item, List<Relationship>> _outgoingRelationships = new Dictionary<Item, List<Relationship>>();
		private readonly IDictionary<Item, ItemControl> _allItemControls = new Dictionary<Item, ItemControl>();

		private void itemControlDeleteRequest(object sender, EventArgs e)
		{
			var itemControl = sender as ItemControl;
			if (itemControl != null)
			{
				itemControl.OnDeleteRequest -= itemControlDeleteRequest;
				itemControl.OnAddRelationshipRequest -= itemControlAddRelationshipRequest;

				_viewScreen.Children.Remove(itemControl);
				_project.Items.Remove(itemControl.Item);

				itemControl.Item.Changed -= onItemOnChanged;

				foreach (var relationship in _incomingRelationships[itemControl.Item])
				{
					_viewScreen.Children.Remove(relationship.Connector);
				}
				_incomingRelationships.Remove(itemControl.Item);

				foreach (var relationship in _outgoingRelationships[itemControl.Item])
				{
					_viewScreen.Children.Remove(relationship.Connector);
				}
				_outgoingRelationships.Remove(itemControl.Item);

				_allItemControls.Remove(itemControl.Item);
			}
		}

		private void itemControlAddRelationshipRequest(ItemControl sender, ItemType itemType)
		{
			var bottomControls = sender.Item.Relationships.Select(relationship => _allItemControls[relationship]).ToList();
			double mostBottom = bottomControls.Count > 0
				? bottomControls.Max(itemControl => itemControl.Item.Top + itemControl.ActualHeight)
				: sender.Item.Top + sender.ActualHeight;

			var newItem = new Item(
				itemType,
				sender.Item.Left + sender.ActualWidth*2/3,
				mostBottom + 15);
			sender.Item.Relationships.Add(newItem);

			_project.Items.Add(newItem);
			var relationshipControl = displayItem(newItem);
			displayRelationship(sender, relationshipControl);
		}

		private bool canProceedAfterSave()
		{
			if (!_project.HasChanges) return true;

			var promptResult = MessageBox.Show("This file has been modified. Save changes?", "Save changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

			return  promptResult == MessageBoxResult.No ||
					(promptResult == MessageBoxResult.Yes && saveAs());
		}

		private static Project createNewProject()
		{
			return new Project();
		}

		#endregion

		#region Main menu

		private readonly OpenFileDialog _openFileDialog = new OpenFileDialog
		{
			DefaultExt = ".xml",
			Filter = "XML-file|*.xml",
			RestoreDirectory = true,
			Title = "Open file...",
		};
		private readonly SaveFileDialog _saveFileDialog = new SaveFileDialog
		{
			DefaultExt = ".xml",
			Filter = "XML-file|*.xml",
			RestoreDirectory = true,
			Title = "Save file...",
		};
		private readonly SaveFileDialog _exportImageDialog = new SaveFileDialog
		{
			DefaultExt = $"{BitmapEncodingHelper.DefaultExt}",
			Filter = BitmapEncodingHelper.GetDialogFilter(),
			RestoreDirectory = true,
			Title = "Export image...",
		};

		private void newMenuClick(object sender, RoutedEventArgs e)
		{
			if (canProceedAfterSave())
			{
				setModel(createNewProject());
			}
		}

		private void openMenuClick(object sender, RoutedEventArgs e)
		{
			if (canProceedAfterSave())
			{
				_openFileDialog.FileName = _project.FileName;
				if (_openFileDialog.ShowDialog() == true)
				{
					setModel(Project.Load(_openFileDialog.FileName));
				}
			}
		}

		private void saveMenuClick(object sender, RoutedEventArgs e)
		{
			if (!string.IsNullOrEmpty(_project.FileName))
			{
				_project.Save(_project.FileName);
				updateWindowTitle();
			}
			else
			{
				if (saveAs())
				{
					updateWindowTitle();
				}
			}
		}

		private void saveAsMenuClick(object sender, RoutedEventArgs e)
		{
			if (saveAs())
			{
				updateWindowTitle();
			}
		}

		private bool saveAs()
		{
			_saveFileDialog.FileName = _project.FileName;
			if (_saveFileDialog.ShowDialog() == true)
			{
				_project.Save(_saveFileDialog.FileName);
				updateWindowTitle();
				return true;
			}
			else
			{
				return false;
			}
		}

		private void itemTypesClick(object sender, RoutedEventArgs e)
		{
			new ItemTypesDialog
			{
				ItemTypes = _project.ItemTypes
			}.ShowDialog();
		}

		private void exportImageClick(object sender, RoutedEventArgs e)
		{
			if (_exportImageDialog.ShowDialog() == true)
			{
				const double dpi = 96;
				var pixelFormat = PixelFormats.Pbgra32;

				double width = 0, height = 0;
				foreach (var itemControl in _allItemControls.Values)
				{
					width = Math.Max(width, itemControl.Item.Left + itemControl.ActualWidth);
					height = Math.Max(height, itemControl.Item.Top + itemControl.ActualHeight);
				}

				RenderTargetBitmap resultBitmap;
				if (width > 0 && height > 0)
				{
					var drawingVisual = new DrawingVisual();
					using (var drawingContext = drawingVisual.RenderOpen())
					{
						var bounds = new Rect(new Point(), new Size(width, height));
						drawingContext.DrawRectangle(Brushes.White, null, bounds);
						drawingContext.DrawRectangle(new VisualBrush(_viewScreen), null, bounds);
					}
					resultBitmap = new RenderTargetBitmap((int) width, (int) height, dpi, dpi, pixelFormat);
					resultBitmap.Render(drawingVisual);
				}
				else
				{
					resultBitmap = new RenderTargetBitmap(0, 0, dpi, dpi, pixelFormat);
				}

				var bitmapEncoder = BitmapEncodingHelper.CreateEncoder(System.IO.Path.GetExtension(_exportImageDialog.FileName));
				bitmapEncoder.Frames.Add(BitmapFrame.Create(resultBitmap));

				using (var outputStream = File.Create(_exportImageDialog.FileName))
				{
					bitmapEncoder.Save(outputStream);
				}
			}
		}

		#endregion

		#region Canvas context menu

		private Point _lastMousePosition;

		private void canvasMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Right && !(e.Source is ItemControl))
			{
				_viewScreenPopup.IsOpen = true;
				_lastMousePosition = e.GetPosition(dockPanelContainer);
			}
		}

		private void addItemMenuClick(object sender, RoutedEventArgs e)
		{
			_viewScreenPopup.IsOpen = false;

			var itemTypesDialog = new SelectItemTypeDialog
			{
				ItemTypes = _project.ItemTypes,
			};
			if (itemTypesDialog.ShowDialog() == true)
			{
				var newItem = new Item(itemTypesDialog.SelectedItemType, _lastMousePosition.X, _lastMousePosition.Y);
				_project.Items.Add(newItem);
				displayItem(newItem);
			}
		}

		private void clearItemsMenuClick(object sender, RoutedEventArgs e)
		{
			_viewScreenPopup.IsOpen = false;

			_project.Items.Clear();

			foreach (var itemControl in _viewScreen.Children.OfType<ItemControl>())
			{
				itemControl.OnDeleteRequest -= itemControlDeleteRequest;
			}
			_viewScreen.Children.Clear();
		}

		#endregion
	}
}
