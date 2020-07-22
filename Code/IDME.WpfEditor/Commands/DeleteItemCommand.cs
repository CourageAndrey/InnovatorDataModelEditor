using IDME.WpfEditor.ViewModels;

namespace IDME.WpfEditor.Commands
{
	public class DeleteItemCommand : BaseEditCommand
	{
		public Item Item
		{ get; }

		public DeleteItemCommand(Project project, Item item)
			: base(project)
		{
			Item = item;
		}

		public override void Apply()
		{
			Project.Items.Remove(Item);
		}

		public override void Rollback()
		{
			Project.Items.Add(Item);
		}
	}
}
