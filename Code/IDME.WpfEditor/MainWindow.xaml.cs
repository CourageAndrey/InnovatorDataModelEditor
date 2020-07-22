﻿using System;
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

using Newtonsoft.Json;

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
			selectItemControl(null);
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
			updateCutCopyButtons();
			updatePasteButton();
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

				if (_selectedItemControl == itemControl)
				{
					selectItemControl(null);
				}
				_allItemControls.Remove(itemControl.Item);
				updateCutCopyButtons();
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
		private readonly PrintDialog _printDialog = new PrintDialog();

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

		private void copyClick(object sender, RoutedEventArgs e)
		{
			copy();
		}

		private void cutClick(object sender, RoutedEventArgs e)
		{
			cut();
		}

		private void pasteClick(object sender, RoutedEventArgs e)
		{
			paste();
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
				selectItemControl(null);

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

		private void printClick(object sender, RoutedEventArgs e)
		{
			if (_printDialog.ShowDialog() == true)
			{
				selectItemControl(null);
				_printDialog.PrintVisual(_viewScreen, $"Innovator data model \"{_project.FileName}\"");
			}
		}

		#endregion

		#region Canvas context menu

		private ItemControl _selectedItemControl;
		private Point _lastMousePosition;

		private void canvasMouseDown(object sender, MouseButtonEventArgs e)
		{
			selectItemControl(e.Source as ItemControl);
			if (_selectedItemControl == null)
			{
				if (e.ChangedButton == MouseButton.Right)
				{
					_viewScreenPopup.IsOpen = true;
				}
				_lastMousePosition = e.GetPosition(dockPanelContainer);
			}
		}

		private void selectItemControl(ItemControl control)
		{
			if (_selectedItemControl != null)
			{
				_selectedItemControl.Background = SystemColors.ControlBrush;
			}
			_selectedItemControl = control;
			if (_selectedItemControl != null)
			{
				_selectedItemControl.Background = SystemColors.HighlightBrush;
			}

			updateCutCopyButtons();
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
				var newControl = displayItem(newItem);
				selectItemControl(newControl);
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

		#region Copy&Paste

		private void updateCutCopyButtons()
		{
			_buttonCopy.IsEnabled = _buttonCut.IsEnabled = _selectedItemControl != null;
		}

		private void updatePasteButton()
		{
			_buttonPaste.IsEnabled = Clipboard.ContainsData(DataFormats.StringFormat);
		}

		private void canvasKeyDown(object sender, KeyEventArgs e)
		{
			if (Keyboard.Modifiers == ModifierKeys.Control)
			{
				switch (e.Key)
				{
					case Key.C:
						if (_buttonCopy.IsEnabled)
						{
							copy();
						}
						break;
					case Key.X:
						if (_buttonCut.IsEnabled)
						{
							cut();
						}
						break;
					case Key.V:
						if (_buttonPaste.IsEnabled)
						{
							paste();
						}
						break;
				}
			}
			else if (e.Key == Key.Delete && _selectedItemControl != null)
			{
				itemControlDeleteRequest(_selectedItemControl, EventArgs.Empty);
			}
		}

		private void copy()
		{
			Clipboard.SetData(DataFormats.StringFormat, serializeToClipboard(_selectedItemControl.Item));
			updatePasteButton();
		}

		private void cut()
		{
			Clipboard.SetData(DataFormats.StringFormat, serializeToClipboard(_selectedItemControl.Item));
			itemControlDeleteRequest(_selectedItemControl, EventArgs.Empty);
			updatePasteButton();
		}

		private void paste()
		{
			string serializedItem = (string) Clipboard.GetData(DataFormats.StringFormat);
			var dictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(serializedItem);
			string itemType = null;
			var properties = new List<PropertyValue>();
			foreach (var value in dictionary)
			{
				if (value.Key == "__type__")
				{
					itemType = value.Value;
				}
				else
				{
					properties.Add(new PropertyValue(value.Key, value.Value));
				}
			}
			var newItem = new Item(_project.ItemTypes.First(type => type.Name == itemType), _lastMousePosition.X, _lastMousePosition.Y, properties);
			_project.Items.Add(newItem);
			displayItem(newItem);
			updateCutCopyButtons();
		}

		private static string serializeToClipboard(Item item)
		{
			var dictionary = new Dictionary<string, string> { { "__type__", item.ItemType.Name } };
			foreach (var property in item.Properties)
			{
				dictionary[property.Name] = property.Value;
			}
			return JsonConvert.SerializeObject(dictionary);
		}

		#endregion
	}
}
