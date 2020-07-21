using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

namespace IDME.WpfEditor.Helpers
{
	internal static class BitmapEncodingHelper
	{
		#region Known descriptors

		private class BitmapEncodingDescriptor
		{
			public string Extension
			{ get; }

			private readonly Func<BitmapEncoder> _constructor;

			public BitmapEncodingDescriptor(string extension, Func<BitmapEncoder> constructor)
			{
				Extension = extension;
				_constructor = constructor;
			}

			public BitmapEncoder CreateEncoder()
			{
				return _constructor();
			}
		}

		private class BitmapEncodingDescriptor<EncoderT> : BitmapEncodingDescriptor
			where EncoderT : BitmapEncoder, new()
		{
			public BitmapEncodingDescriptor(string extension)
				: base(extension, () => new EncoderT())
			{ }
		}

		private static readonly BitmapEncodingDescriptor<PngBitmapEncoder> _png = new BitmapEncodingDescriptor<PngBitmapEncoder>("png");
		private static readonly BitmapEncodingDescriptor<BmpBitmapEncoder> _bmp = new BitmapEncodingDescriptor<BmpBitmapEncoder>("bmp");
		private static readonly BitmapEncodingDescriptor<JpegBitmapEncoder> _jpeg = new BitmapEncodingDescriptor<JpegBitmapEncoder>("jpeg");
		private static readonly BitmapEncodingDescriptor<GifBitmapEncoder> _gif = new BitmapEncodingDescriptor<GifBitmapEncoder>("gif");
		private static readonly BitmapEncodingDescriptor<TiffBitmapEncoder> _tiff = new BitmapEncodingDescriptor<TiffBitmapEncoder>("tiff");
		private static readonly BitmapEncodingDescriptor<WmpBitmapEncoder> _wmp = new BitmapEncodingDescriptor<WmpBitmapEncoder>("wmp");

		private static readonly IDictionary<string, BitmapEncodingDescriptor> _descriptors = new Dictionary<string, BitmapEncodingDescriptor>
		{
			{ "." + _png.Extension, _png },
			{ "." + _bmp.Extension, _bmp },
			{ "." + _jpeg.Extension, _jpeg },
			{ "." + _gif.Extension, _gif },
			{ "." + _tiff.Extension, _tiff },
			{ "." + _wmp.Extension, _wmp },
		};

		#endregion

		public static string GetDialogFilter()
		{
			return string.Join("|", _descriptors.Values.Select(descriptor => $"{descriptor.Extension.ToUpperInvariant()} image|*.{descriptor.Extension.ToLowerInvariant()}"));
		}

		public static string DefaultExt
		{ get { return _descriptors.Keys.First(); } }

		public static BitmapEncoder CreateEncoder(string extension)
		{
			BitmapEncodingDescriptor descriptor;
			if (_descriptors.TryGetValue(extension, out descriptor))
			{
				return descriptor.CreateEncoder();
			}
			else
			{
				throw new NotSupportedException($"Impossible to save file of \"{extension}\" format.");
			}
		}
	}
}
