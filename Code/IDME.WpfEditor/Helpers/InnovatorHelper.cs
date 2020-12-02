using System.Collections.Generic;

using Aras.IOM;

namespace IDME.WpfEditor.Helpers
{
	public static class InnovatorHelper
	{
		public static IEnumerable<Item> Enumerate(this Item item)
		{
			for (int count = item.getItemCount(), i = 0; i < count; i++)
			{
				yield return item.getItemByIndex(i);
			}
		}
	}
}
