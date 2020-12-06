using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace IDME.WpfEditor.ViewModels
{
	public class PropertyValue : INotifyPropertyChanged
	{
		private string _value;

		public string Name
		{ get; }

		public string Value
		{
			get => _value;
			set
			{
				PreviousValue = _value;
				_value = value;
				raiseChanged();
			}
		}

		internal string PreviousValue
		{ get; private set; }

		public PropertyValue(string name, string value)
		{
			Name = name;
			Value = value;
		}

		#region Implementation of INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		private void raiseChanged([CallerMemberName] string propertyName = null)
		{
			var handler = Volatile.Read(ref PropertyChanged);
			handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}
