using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml;

using IDME.WpfEditor.Commands;
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

		private void itemAdded(object sender, Item e)
		{
			var newControl = displayItem(e);
			selectItemControl(newControl);
		}

		private void itemRemoved(object sender, Item e)
		{
			var itemControl = _allItemControls[e];

			itemControl.OnDeleteRequest -= itemControlDeleteRequest;
			itemControl.OnAddRelationshipRequest -= itemControlAddRelationshipRequest;

			_viewScreen.Children.Remove(itemControl);

			itemControl.Item.PropertyChanged -= onItemChanged;

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

		private void setModel(Project project)
		{
			if (_project != null)
			{
				_project.ItemAdded -= itemAdded;
				_project.ItemRemoved -= itemRemoved;
			}
			DataContext = _project = project;
			selectItemControl(null);
			if (_project != null)
			{
				_project.ItemAdded += itemAdded;
				_project.ItemRemoved += itemRemoved;
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

			updateCutCopyButtons();
			updatePasteButton();
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
			item.PropertyChanged += onItemChanged;
			_allItemControls[item] = itemControl;
			return itemControl;
		}

		private void onItemChanged(object sender, PropertyChangedEventArgs e)
		{
			var item = (Item) sender;

			if (e.PropertyName == nameof(Item.Left) || e.PropertyName == nameof(Item.Top))
			{
				foreach (var relationship in _incomingRelationships[item])
				{
					relationship.UpdateConnector();
				}
				foreach (var relationship in _outgoingRelationships[item])
				{
					relationship.UpdateConnector();
				}

				if (_project.NeedToMoveItem)
				{
					Canvas.SetLeft(_allItemControls[item], item.Left);
					Canvas.SetTop(_allItemControls[item], item.Top);
				}
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

		private readonly IDictionary<Item, List<Relationship>> _incomingRelationships = new Dictionary<Item, List<Relationship>>();
		private readonly IDictionary<Item, List<Relationship>> _outgoingRelationships = new Dictionary<Item, List<Relationship>>();
		private readonly IDictionary<Item, ItemControl> _allItemControls = new Dictionary<Item, ItemControl>();

		private void itemControlDeleteRequest(object sender, EventArgs e)
		{
			var itemControl = sender as ItemControl;
			if (itemControl != null)
			{
				_project.PerformCommand(new DeleteItemCommand(_project, itemControl.Item));
			}
		}

		private void itemControlAddRelationshipRequest(ItemControl sender, ItemType itemType)
		{
			var bottomControls = sender.Item.Relationships.Select(relationship => _allItemControls[relationship]).ToList();
			double mostBottom = bottomControls.Count > 0
				? bottomControls.Max(itemControl => itemControl.Item.Top + itemControl.ActualHeight)
				: sender.Item.Top + sender.ActualHeight;

			var command = new AddRelationshipCommand(_project, sender.Item, itemType, sender.Item.Left + sender.ActualWidth * 2 / 3, mostBottom + 15);
			_project.PerformCommand(command);

			var relationshipControl = _allItemControls[command.NewRelationship];
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
			}
			else
			{
				saveAs();
			}
		}

		private void saveAsMenuClick(object sender, RoutedEventArgs e)
		{
			saveAs();
		}

		private bool saveAs()
		{
			_saveFileDialog.FileName = _project.FileName;
			if (_saveFileDialog.ShowDialog() == true)
			{
				_project.Save(_saveFileDialog.FileName);
				return true;
			}
			else
			{
				return false;
			}
		}

		private void undoMenuClick(object sender, RoutedEventArgs e)
		{
			_project.Undo();
		}

		private void redoMenuClick(object sender, RoutedEventArgs e)
		{
			_project.Redo();
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
			if (_selectedItemControl != null)
			{
				Keyboard.ClearFocus();
			}
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
				Owner = this,
				ItemTypes = _project.ItemTypes,
			};
			if (itemTypesDialog.ShowDialog() == true)
			{
				_project.PerformCommand(new AddItemCommand(_project, itemTypesDialog.SelectedItemType, _lastMousePosition.X, _lastMousePosition.Y));
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
			if (!(e.OriginalSource is TextBoxBase))
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
						case Key.Z:
							if (_buttonUndo.IsEnabled)
							{
								_project.Undo();
							}
							break;
						case Key.Y:
							if (_buttonRedo.IsEnabled)
							{
								_project.Redo();
							}
							break;
					}
				}
				else if (e.Key == Key.Delete && _selectedItemControl != null)
				{
					itemControlDeleteRequest(_selectedItemControl, EventArgs.Empty);
				}

				e.Handled = true;
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

		#region Interaction with Innovator

		private void connectInnovatorClick(object sender, RoutedEventArgs e)
		{
			tryToConnectInnovator();
		}

		private void downloadItemsClick(object sender, RoutedEventArgs e)
		{
			var innovator = getInnovator();
			if (innovator != null)
			{
				var selectItemTypeDialog = new SelectItemTypeDialog
				{
					Owner = this,
					ItemTypes = _project.ItemTypes,
				};
				if (selectItemTypeDialog.ShowDialog() == true)
				{
					var itemType = selectItemTypeDialog.SelectedItemType;

					var itemsRequest = innovator.newItem(itemType.Name, "get");
					itemsRequest.setAttribute("select", "*");
					var itemsItem = itemsRequest.apply();

					var items = new List<Aras.IOM.Item>();
					for (int i = 0, count = itemsItem.getItemCount(); i < count; i++)
					{
						items.Add(itemsItem.getItemByIndex(i));
					}

					var selectDialog = new SelectItemsDialog
					{
						Owner = this,
						Properties = itemType.Properties.Select(property => property.Name).ToList(),
						Items = items,
					};
					if (selectDialog.ShowDialog() == true)
					{
						double x = _project.Items.Max(item => item.Left + _allItemControls[item].ActualWidth);
						double y = _project.Items.Max(item => item.Top + _allItemControls[item].ActualHeight);

						const int xOffset = 100;
						const int yOffset = 50;

						foreach (var item in selectDialog.SelectedItems)
						{
							x += xOffset;
							y += yOffset;
							var newItem = new Item(itemType, x, y);
							foreach (var property in newItem.Properties)
							{
								property.Value = item.getProperty(property.Name);
							}
							_project.Items.Add(newItem);
						}
					}
				}
			}
		}

		private void uploadItemsClick(object sender, RoutedEventArgs e)
		{
			var innovator = getInnovator();
			if (innovator != null)
			{
				var aml = new StringBuilder("<AML>");
				foreach (var projectItem in _project.Items)
				{
					var innovatorItem = innovator.newItem(projectItem.ItemType.Name, "add");
					foreach (var value in projectItem.Properties)
					{
						innovatorItem.setProperty(value.Name, value.Value);
					}
					aml.Append(innovatorItem.node.OuterXml);
				}
				aml.Append("</AML>");

				var result = innovator.applyAML(aml.ToString());
				if (result.isError())
				{
					MessageBox.Show(result.getErrorDetail(), "An error occured", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				else
				{
					MessageBox.Show("Successful!");
				}
			}
		}

		private void downloadItemTypesClick(object sender, RoutedEventArgs e)
		{
			var innovator = getInnovator();
			if (innovator != null)
			{
				var itemTypesRequest = innovator.newItem("ItemType", "get");
				itemTypesRequest.setAttribute("select", "name,is_relationship");
				var propertyRequest = innovator.newItem("Property", "get");
				propertyRequest.setAttribute("select", "name,label,data_type");
				propertyRequest.setProperty("is_hidden", "0");
				itemTypesRequest.addRelationship(propertyRequest);

				var itemTypesItem = itemTypesRequest.apply();
				var itemTypes = new List<Aras.IOM.Item>();
				for (int i = 0, count = itemTypesItem.getItemCount(); i < count; i++)
				{
					itemTypes.Add(itemTypesItem.getItemByIndex(i));
				}

				var selectDialog = new SelectItemsDialog
				{
					Owner = this,
					Properties = new List<string> { "name", "is_relationship" },
					Items = itemTypes,
				};
				if (selectDialog.ShowDialog() == true)
				{
					var itemTypesCache = _project.ItemTypes.ToDictionary(
						itemType => itemType.Name,
						itemType => itemType);
					var itemTypeProperties = new Dictionary<string, Dictionary<string, string>>();

					foreach (var itemType in selectDialog.SelectedItems)
					{
						var properties = new List<Property>();
						var itemProperties = new Dictionary<string, string>();
						var propertiesItem = itemType.getRelationships("Property");
						for (int i = 0, count = propertiesItem.getItemCount(); i < count; i++)
						{
							var propertyItem = propertiesItem.getItemByIndex(i);
							properties.Add(new Property { Name = propertyItem.getProperty("name") });
						}
						var newItemType = new ItemType(
							itemType.getProperty("name"),
							itemType.getProperty("is_relationship") == "1",
							properties,
							new ItemType[0]);
						_project.ItemTypes.Add(newItemType);
						itemTypesCache[newItemType.Name] = newItemType;
						if (itemProperties.Count > 0)
						{
							itemTypeProperties[newItemType.Name] = itemProperties;
						}
					}

					foreach (var itemType in itemTypeProperties)
					{
						foreach (var property in itemType.Value)
						{
							ItemType pointingType;
							if (itemTypesCache.TryGetValue(property.Value, out pointingType))
							{
								itemTypesCache[itemType.Key].Properties.First(p => p.Name == property.Key).DataSourceType = pointingType;
							}
						}
						
					}
				}
			}
		}

		private void tryToConnectInnovator()
		{
			if (_innovator == null)
			{
				_innovatorServer = DefaultInnovatorServer;
				_innovatorDatabase = DefaultInnovatorDatabase;
				_innovatorUserName = DefaultInnovatorUserName;
				_innovatorPassword = DefaultInnovatorPassword;
			}

			var connectionDialog = new ConnectToInnovatorDialog
			{
				Owner = this,
				Server = _innovatorServer,
				Database = _innovatorDatabase,
				UserName = _innovatorUserName,
				Password = _innovatorPassword,
			};

			if (connectionDialog.ShowDialog() == true)
			{
				_innovatorServer = connectionDialog.Server;
				_innovatorDatabase = connectionDialog.Database;
				_innovatorUserName = connectionDialog.UserName;
				_innovatorPassword = connectionDialog.Password;

				var serverConnection = Aras.IOM.IomFactory.CreateHttpServerConnection(
					_innovatorServer,
					_innovatorDatabase,
					_innovatorUserName,
					_innovatorPassword);

				_innovator = new Aras.IOM.Innovator(serverConnection);
			}
		}

		private Aras.IOM.Innovator getInnovator()
		{
			if (_innovator == null)
			{
				tryToConnectInnovator();
			}
			return _innovator;
		}

		const string DefaultInnovatorServer = "http://localhost/KHI_Aerospace-Development-M2/";
		const string DefaultInnovatorDatabase = "KHI_Aerospace-Development-M2";
		const string DefaultInnovatorUserName = "admin";
		const string DefaultInnovatorPassword = "innovator";

		string _innovatorServer;
		string _innovatorDatabase;
		string _innovatorUserName;
		string _innovatorPassword;
		private Aras.IOM.Innovator _innovator;

		#endregion

		#region [de]serialization from/to AML

		private void almImportClick(object sender, RoutedEventArgs e)
		{
			var innovator = getInnovator();
			if (_openAmlDialog.ShowDialog() == true && innovator != null)
			{
				var dom = new XmlDocument();
				dom.Load(_openAmlDialog.FileName);

				var item = innovator.newItem();
				item.loadAML(dom.OuterXml);

				string importError = EnsureSingleItem(null, "add", ref item);
				if (string.IsNullOrEmpty(importError))
				{
					string itemTypeName = item.getType();
					var itemType = _project.ItemTypes.FirstOrDefault(it => it.Name == itemTypeName);
					if (itemType != null)
					{
						var addItemCommand = new AddItemCommand(_project, itemType, 0, 0);
						_project.PerformCommand(addItemCommand);
						Item importedItem = addItemCommand.NewItem;

						foreach (var property in importedItem.Properties)
						{
							property.Value = item.getProperty(property.Name);
						}

						_allItemControls[importedItem].Item = importedItem; // update after properties are refreshed
					}
					else
					{
						MessageBox.Show($"There are no \"{itemTypeName}\" type found.", "Impossible to import", MessageBoxButton.OK, MessageBoxImage.Error);
					}
				}
				else
				{
					MessageBox.Show(importError, "Impossible to import", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}

		private void amlExportClick(object sender, RoutedEventArgs e)
		{
			var innovator = getInnovator();
			if (_saveAmlDialog.ShowDialog() == true && innovator != null)
			{
				Aras.IOM.Item domItem;

				if (_project.Items.Count > 0)
				{
					domItem = innovator.newItem();

					foreach (var projectItem in _project.Items)
					{
						var iomItem = innovator.newItem(projectItem.ItemType.Name, "add");
						iomItem.removeAttribute("id");
						iomItem.removeAttribute("isTemp");
						iomItem.removeAttribute("isNew");

						foreach (var itemProperty in projectItem.Properties)
						{
							iomItem.setProperty(itemProperty.Name, itemProperty.Value);
						}

						domItem.appendItem(iomItem);
					}

					domItem.removeItem(domItem.getItemByIndex(0));
				}
				else
				{
					domItem = innovator.newResult(string.Empty);
				}

				domItem.dom.Save(_saveAmlDialog.FileName);
			}
		}

		private void amlItemTypesClick(object sender, RoutedEventArgs e)
		{
			var innovator = getInnovator();
			if (_openAmlDialog.ShowDialog() == true && innovator != null)
			{
				var dom = new XmlDocument();
				dom.Load(_openAmlDialog.FileName);

				var item = innovator.newItem();
				item.loadAML(dom.OuterXml);

				string importError = EnsureSingleItem("ItemType", "add", ref item);
				if (string.IsNullOrEmpty(importError))
				{
					string itemTypeName = item.getProperty("name");
					if (_project.ItemTypes.Any(it => it.Name == itemTypeName))
					{
						MessageBox.Show($"Itemtype \"{itemTypeName}\" is already defined.", "Impossible to import", MessageBoxButton.OK, MessageBoxImage.Error);
					}
					else
					{
						var properties = item.getRelationships("Property").Enumerate();
						var relationships = item.getRelationships("RelationshipType").Enumerate().Select(r => r.getPropertyAttribute("relationship_id", "keyed_name")).ToList();

						var itemType = new ItemType(
							itemTypeName,
							item.getProperty("is_relationship") == "1",
							properties.Select(propertyItem =>
							{
								string propertyName = propertyItem.getProperty("name");
								string dataSourceTypeName = propertyItem.getPropertyAttribute("data_source", "keyed_name");
								var dataSourceType = _project.ItemTypes.FirstOrDefault(type => type.Name == dataSourceTypeName);
								return new Property
								{
									Name = propertyName,
									DataSourceType = dataSourceType,
								};
							}),
							_project.ItemTypes.Where(it => it.IsRelationship && relationships.Contains(it.Name)));

						_project.ItemTypes.Add(itemType);
					}
				}
				else
				{
					MessageBox.Show(importError, "Impossible to import", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}

		private static string EnsureSingleItem(string expectedItemType, string expectedAction, ref Aras.IOM.Item item)
		{
			var allItems = item.Enumerate().Where(i =>
				(string.IsNullOrEmpty(expectedItemType) || i.getType() == expectedItemType) &&
				(string.IsNullOrEmpty(expectedAction) || i.getAction() == expectedAction)).ToList();
			if (allItems.Count > 1)
			{
				string typeFilter = !string.IsNullOrEmpty(expectedItemType) ? $" of '{expectedItemType}' type" : string.Empty;
				string actionFilter = !string.IsNullOrEmpty(expectedAction) ? $" with '{expectedAction}' action" : string.Empty;
				return $"AML has to contain single Item {typeFilter}{actionFilter} in order to be processed.";
			}
			else if (allItems.Count == 1)
			{
				item = allItems[0];
				return null;
			}
			else // if (allItems.Count == 0)
			{
				return $"No Items of '{expectedItemType}' type with '{expectedAction}' action found.";
			}
		}

		private readonly OpenFileDialog _openAmlDialog = new OpenFileDialog
		{
			DefaultExt = ".xml",
			Filter = "AML file|*.xml",
			RestoreDirectory = true,
			Title = "Open Import file...",
		};
		private readonly SaveFileDialog _saveAmlDialog = new SaveFileDialog
		{
			DefaultExt = ".xml",
			Filter = "AML file|*.xml",
			RestoreDirectory = true,
			Title = "Save Import file...",
		};

		#endregion
	}
}
