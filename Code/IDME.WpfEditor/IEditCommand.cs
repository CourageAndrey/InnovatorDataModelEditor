using IDME.WpfEditor.ViewModels;

namespace IDME.WpfEditor
{
	public interface IEditCommand
	{
		Project Project
		{ get; }

		void Apply();

		void Rollback();
	}

	public abstract class BaseEditCommand : IEditCommand
	{
		public Project Project
		{ get; }

		protected BaseEditCommand(Project project)
		{
			Project = project;
		}

		public abstract void Apply();

		public abstract void Rollback();
	}
}
