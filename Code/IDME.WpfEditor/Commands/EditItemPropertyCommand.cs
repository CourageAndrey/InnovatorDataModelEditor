using IDME.WpfEditor.ViewModels;

namespace IDME.WpfEditor.Commands
{
#warning This class is not used, but need!
	public class EditItemPropertyCommand : BaseEditCommand
	{
		public Item Item
		{ get; set; }

		public PropertyValue Property
		{ get; set; }

		public string ValueBefore
		{ get; set; }

		public string ValueAfter
		{ get; set; }

		public EditItemPropertyCommand(Project project, Item item, PropertyValue property, string value)
			: base(project)
		{
			Item = item;
			Property = property;
			ValueBefore = property.Value;
			ValueAfter = value;
		}

		public override void Apply()
		{
			Property.Value = ValueAfter;
		}

		public override void Rollback()
		{
			Property.Value = ValueBefore;
		}
	}
}
