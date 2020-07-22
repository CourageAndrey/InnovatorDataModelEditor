using IDME.WpfEditor.ViewModels;

namespace IDME.WpfEditor.Commands
{
	public class AddRelationshipCommand : BaseEditCommand
	{
		public Item Item
		{ get; }

		public ItemType RelationshipType
		{ get; }

		public double X
		{ get; }

		public double Y
		{ get; }

		public AddRelationshipCommand(Project project, Item item, ItemType relationshipType, double x, double y)
			: base(project)
		{
			Item = item;
			RelationshipType = relationshipType;
			X = x;
			Y = y;
		}

		public Item NewRelationship
		{ get; private set; }

		public override void Apply()
		{
			NewRelationship = new Item(RelationshipType, X, Y);
			Project.Items.Add(NewRelationship);
			Item.Relationships.Add(NewRelationship);
		}

		public override void Rollback()
		{
			Item.Relationships.Remove(NewRelationship);
			Project.Items.Remove(NewRelationship);
			NewRelationship = null;
		}
	}
}
