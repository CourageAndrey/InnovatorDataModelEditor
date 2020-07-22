using IDME.WpfEditor.ViewModels;

namespace IDME.WpfEditor.Commands
{
	public class AddItemCommand : BaseEditCommand
	{
		public ItemType ItemType
		{ get; }

		public double X
		{ get; }

		public double Y
		{ get; }

		public Item NewItem
		{ get; private set; }

		public AddItemCommand(Project project, ItemType itemType, double x, double y)
			: base(project)
		{
			ItemType = itemType;
			X = x;
			Y = y;
		}

		public override void Apply()
		{
			NewItem = new Item(ItemType, X, Y);
			Project.Items.Add(NewItem);
		}

		public override void Rollback()
		{
			Project.Items.Remove(NewItem);
			NewItem = null;
		}
	}
}
