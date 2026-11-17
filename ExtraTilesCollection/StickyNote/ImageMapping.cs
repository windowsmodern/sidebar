using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace WindowsModern.StickyNote
{
	public static class ImageColorMapper
	{
		public static unsafe BitmapSource MapImageColors (ImageSource source, Color mapColor)
		{
			if (source == null) return null;
			BitmapSource bitmapSource = source as BitmapSource;
			if (bitmapSource == null)
			{
				double sourceWidth = source.Width;
				double sourceHeight = source.Height;
				if (double.IsNaN (sourceWidth) || double.IsInfinity (sourceWidth) || double.IsNaN (sourceHeight) || double.IsInfinity (sourceHeight) || sourceWidth <= 0 || sourceHeight <= 0) return null;
				int renderWidth = (int)Math.Ceiling (sourceWidth);
				int renderHeight = (int)Math.Ceiling (sourceHeight);
				RenderTargetBitmap rendered = new RenderTargetBitmap (renderWidth, renderHeight, 96, 96, PixelFormats.Pbgra32);
				DrawingVisual visual = new DrawingVisual ();
				using (DrawingContext context = visual.RenderOpen ())
				{
					context.DrawImage (source, new Rect (0, 0, renderWidth, renderHeight));
				}
				rendered.Render (visual);
				bitmapSource = rendered;
			}
			FormatConvertedBitmap converted = new FormatConvertedBitmap (bitmapSource, PixelFormats.Bgra32, null, 0);
			int width = converted.PixelWidth;
			int height = converted.PixelHeight;
			if (width <= 0 || height <= 0) return null;
			int sourceStride = checked(width * 4);
			byte [] pixels = new byte [checked(sourceStride * height)];
			converted.CopyPixels (pixels, sourceStride, 0);
			WriteableBitmap result = new WriteableBitmap (width, height, converted.DpiX, converted.DpiY, PixelFormats.Bgra32, null);
			int offsetR = 227 - mapColor.R;
			int offsetG = 227 - mapColor.G;
			int offsetB = 227 - mapColor.B;
			result.Lock ();
			try
			{
				byte* sourcePixels;
				fixed (byte* sourcePointer = pixels)
				{
					sourcePixels = sourcePointer;
					byte* destinationPixels = (byte*)result.BackBuffer.ToPointer ();
					int destinationStride = result.BackBufferStride;
					for (int y = 0; y < height; y++)
					{
						byte* src = sourcePixels + y * sourceStride;
						byte* dst = destinationPixels + y * destinationStride;
						for (int x = 0; x < width; x++)
						{
							byte b = src [0];
							byte g = src [1];
							byte r = src [2];
							byte a = src [3];
							if (a != 0 && r == g && g == b)
							{
								int newB = b - offsetB;
								int newG = g - offsetG;
								int newR = r - offsetR;
								dst [0] = (byte)(newB < 0 ? 0 : newB > 255 ? 255 : newB);
								dst [1] = (byte)(newG < 0 ? 0 : newG > 255 ? 255 : newG);
								dst [2] = (byte)(newR < 0 ? 0 : newR > 255 ? 255 : newR);
							}
							else
							{
								dst [0] = b;
								dst [1] = g;
								dst [2] = r;
							}
							dst [3] = a;
							src += 4;
							dst += 4;
						}
					}
				}
				result.AddDirtyRect (new Int32Rect (0, 0, width, height));
			}
			finally
			{
				result.Unlock ();
			}
			result.Freeze ();
			return result;
		}
	}
}
