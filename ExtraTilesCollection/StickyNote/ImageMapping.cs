using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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
		public static BitmapSource MapImageColors (ImageSource imgSrc, System.Drawing.Color mapColor)
			=> MapImageColors (imgSrc, Color.FromArgb (mapColor.A, mapColor.R, mapColor.G, mapColor.B));
	}
	public static class DrawingColorHelper
	{
		public static System.Drawing.Color ?ToDrawingColor (this Color color)
		{
			if (color == null) return null;
			return System.Drawing.Color.FromArgb (color.A, color.R, color.G, color.B);
		}
		public static Color ToMediaColor (this System.Drawing.Color color)
		{
			if (color == null) return new Color ();
			return Color.FromArgb (color.A, color.R, color.G, color.B);
		}
	}
	public static class LayeredWindowImageHelder
	{
		public static BitmapSource ConvertToPbgra32 (ImageSource imageSource)
		{
			if (imageSource == null) return null;
			BitmapSource bitmapSource = imageSource as BitmapSource;
			if (bitmapSource != null && bitmapSource.Format == PixelFormats.Pbgra32)
				return bitmapSource;
			var formattedBitmap = new FormatConvertedBitmap ();
			formattedBitmap.BeginInit ();
			formattedBitmap.Source = imageSource as BitmapSource;
			formattedBitmap.DestinationFormat = PixelFormats.Pbgra32;
			formattedBitmap.EndInit ();
			formattedBitmap.Freeze ();
			return formattedBitmap;
		}
		public static void UpdateWindowWithImage (Window window, BitmapSource pbgra32Image)
		{
			IntPtr hwnd = new WindowInteropHelper (window).Handle;
			int stride = pbgra32Image.PixelWidth * 4; // 32bpp = 4 bytes per pixel
			byte [] pixels = new byte [stride * pbgra32Image.PixelHeight];
			pbgra32Image.CopyPixels (pixels, stride, 0);
			IntPtr screenDc = GetDC (IntPtr.Zero);
			IntPtr memDc = CreateCompatibleDC (screenDc);
			var bmi = new BITMAPINFO ();
			bmi.bmiHeader.biSize = Marshal.SizeOf (bmi.bmiHeader);
			bmi.bmiHeader.biWidth = pbgra32Image.PixelWidth;
			bmi.bmiHeader.biHeight = -pbgra32Image.PixelHeight;
			bmi.bmiHeader.biPlanes = 1;
			bmi.bmiHeader.biBitCount = 32;
			bmi.bmiHeader.biCompression = 0;
			IntPtr ppvBits = IntPtr.Zero;
			IntPtr hBitmap = CreateDIBSection (memDc, ref bmi, 0, out ppvBits, IntPtr.Zero, 0);
			Marshal.Copy (pixels, 0, ppvBits, pixels.Length); 
			IntPtr hOldBitmap = SelectObject (memDc, hBitmap);
			var blend = new BLENDFUNCTION {
				BlendOp = AC_SRC_OVER, 
				BlendFlags = 0,
				SourceConstantAlpha = 255, 
				AlphaFormat = AC_SRC_ALPHA
			};
			var ptDst = new POINT { x = (int)window.Left, y = (int)window.Top };
			var size = new SIZE { cx = pbgra32Image.PixelWidth, cy = pbgra32Image.PixelHeight };
			var ptSrc = new POINT { x = 0, y = 0 };
			UpdateLayeredWindow (hwnd, screenDc, ref ptDst, ref size, memDc, ref ptSrc, 0, ref blend, ULW_ALPHA);
			SelectObject (memDc, hOldBitmap);
			DeleteObject (hBitmap);
			DeleteDC (memDc);
			ReleaseDC (IntPtr.Zero, screenDc);
		}
		[StructLayout (LayoutKind.Sequential)]
		public struct POINT { public int x; public int y; }
		[StructLayout (LayoutKind.Sequential)]
		public struct SIZE { public int cx; public int cy; }
		[StructLayout (LayoutKind.Sequential)]
		public struct BLENDFUNCTION { public byte BlendOp; public byte BlendFlags; public byte SourceConstantAlpha; public byte AlphaFormat; }
		[StructLayout (LayoutKind.Sequential)]
		public struct BITMAPINFOHEADER
		{
			public int biSize; public int biWidth; public int biHeight; public short biPlanes;
			public short biBitCount; public int biCompression; public int biSizeImage;
			public int biXPelsPerMeter; public int biYPelsPerMeter; public int biClrUsed; public int biClrImportant;
		}
		[StructLayout (LayoutKind.Sequential)]
		public struct BITMAPINFO { public BITMAPINFOHEADER bmiHeader; public int bmiColors; }

		public const int GWL_EXSTYLE = -20;
		public const int WS_EX_LAYERED = 0x00080000;
		public const byte AC_SRC_OVER = 0x00;
		public const byte AC_SRC_ALPHA = 0x01;
		public const int ULW_ALPHA = 0x00000002;

		[DllImport ("user32.dll")]
		public static extern IntPtr GetDC (IntPtr hWnd);
		[DllImport ("user32.dll")]
		public static extern int ReleaseDC (IntPtr hWnd, IntPtr hDC);
		[DllImport ("user32.dll")]
		public static extern bool UpdateLayeredWindow (IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, uint crKey, ref BLENDFUNCTION pblend, uint dwFlags);
		[DllImport ("gdi32.dll")]
		public static extern IntPtr CreateCompatibleDC (IntPtr hdc);
		[DllImport ("gdi32.dll")]
		public static extern bool DeleteDC (IntPtr hdc);
		[DllImport ("gdi32.dll")]
		public static extern IntPtr SelectObject (IntPtr hdc, IntPtr hgdiobj);
		[DllImport ("gdi32.dll")]
		public static extern bool DeleteObject (IntPtr hObject);
		[DllImport ("gdi32.dll")]
		public static extern IntPtr CreateDIBSection (IntPtr hdc, ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);
		[DllImport ("user32.dll")]
		public static extern int GetWindowLong (IntPtr hWnd, int nIndex);
		[DllImport ("user32.dll")]
		public static extern int SetWindowLong (IntPtr hWnd, int nIndex, int dwNewLong);
	}
}
