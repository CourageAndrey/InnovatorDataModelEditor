using System.Windows;
using System.Windows.Shapes;

using IDME.WpfEditor.Controls;

namespace IDME.WpfEditor.ViewModels
{
	public class Relationship
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
}
