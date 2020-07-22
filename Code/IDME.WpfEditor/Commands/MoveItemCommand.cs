using IDME.WpfEditor.ViewModels;

namespace IDME.WpfEditor.Commands
{
#warning This class is not used, but need!
	public class MoveItemCommand : BaseEditCommand
	{
		public Item Item
		{ get; set; }

		public double LeftBefore
		{ get; set; }

		public double TopBefore
		{ get; set; }

		public double LeftAfter
		{ get; set; }

		public double TopAfter
		{ get; set; }

		public MoveItemCommand(Project project, Item item, double left, double top)
			: base(project)
		{
			Item = item;
			LeftBefore = item.Left;
			TopBefore = item.Top;
			LeftAfter = left;
			TopAfter = top;
		}

		public override void Apply()
		{
			Item.Left = LeftAfter;
			Item.Top = TopAfter;
		}

		public override void Rollback()
		{
			Item.Left = LeftBefore;
			Item.Top = TopBefore;
		}
	}
}
