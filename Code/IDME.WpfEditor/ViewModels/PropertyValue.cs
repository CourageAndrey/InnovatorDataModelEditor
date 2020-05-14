using System;
using System.Threading;

namespace IDME.WpfEditor.ViewModels
{
	public class PropertyValue
	{
		private string _value;

		public string Name
		{ get; }

		public string Value
		{
			get => _value;
			set
			{
				_value = value;
				raiseChanged();
			}
		}

		public event EventHandler ValueChanged;

		private void raiseChanged()
		{
			var handler = Volatile.Read(ref ValueChanged);
			if (handler != null)
			{
				handler(this, EventArgs.Empty);
			}
		}

		public PropertyValue(string name, string value)
		{
			Name = name;
			Value = value;
		}
	}
}
