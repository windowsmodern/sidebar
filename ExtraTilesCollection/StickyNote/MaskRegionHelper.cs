using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
public static class MaskRegionHelper
{
	[DllImport ("gdi32.dll")]
	public static extern IntPtr CreateRectRgn (int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
	[DllImport ("gdi32.dll")]
	public static extern int CombineRgn (IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int iMode);
	[DllImport ("gdi32.dll")]
	public static extern bool DeleteObject (IntPtr hObject);
	[DllImport ("user32.dll")]
	public static extern int SetWindowRgn (IntPtr hWnd, IntPtr hRgn, bool bRedraw);
	[DllImport ("gdi32.dll")]
	public static extern IntPtr ExtCreateRegion (IntPtr lpXform, uint nCount, IntPtr lpRgnData);
	public const int RGN_DIFF = 4;
	private const int RDH_RECTANGLES = 1;
	[StructLayout (LayoutKind.Sequential)]
	public struct RECT
	{
		public int Left;
		public int Top;
		public int Right;
		public int Bottom;
	}
	[StructLayout (LayoutKind.Sequential)]
	public struct RGNDATAHEADER
	{
		public int dwSize;
		public int iType;
		public int nCount;
		public int nRgnSize;
		public RECT rcBound;
	}
	public class CornerRects
	{
		public int Alignment;
		public int Width;
		public int Height;
		public List<RECT> Rects = new List<RECT> ();
	}
	public class MaskRectData
	{
		public List<CornerRects> Corners = new List<CornerRects> ();
	}
	public static unsafe CornerRects ExtractRects (BitmapSource mask, double scaleX, double scaleY, int alignment)
	{
		int width = mask.PixelWidth;
		int height = mask.PixelHeight;
		int stride = width * 4;
		IntPtr pixelsPtr = Marshal.AllocHGlobal (stride * height);
		try
		{
			mask.CopyPixels (new Int32Rect (0, 0, width, height), pixelsPtr, stride * height, stride);
			byte* p = (byte*)pixelsPtr;
			var result = new CornerRects {
				Alignment = alignment,
				Width = (int)Math.Round (width * scaleX),
				Height = (int)Math.Round (height * scaleY)
			};
			for (int y = 0; y < height; y++)
			{
				int runStart = -1;
				byte* row = p + y * stride;
				for (int x = 0; x < width; x++)
				{
					int i = x * 4;
					bool isBlack = row [i] < 128 && row [i + 1] < 128 && row [i + 2] < 128;
					if (isBlack)
					{
						if (runStart == -1) runStart = x;
					}
					else
					{
						if (runStart != -1)
						{
							int left = (int)Math.Round (runStart * scaleX);
							int right = (int)Math.Round (x * scaleX);
							int top = (int)Math.Round (y * scaleY);
							int bottom = (int)Math.Round ((y + 1) * scaleY);
							if (right > left && bottom > top)
								result.Rects.Add (new RECT { Left = left, Top = top, Right = right, Bottom = bottom });
							runStart = -1;
						}
					}
				}
				if (runStart != -1)
				{
					int left = (int)Math.Round (runStart * scaleX);
					int right = (int)Math.Round (width * scaleX);
					int top = (int)Math.Round (y * scaleY);
					int bottom = (int)Math.Round ((y + 1) * scaleY);
					if (right > left && bottom > top)
						result.Rects.Add (new RECT { Left = left, Top = top, Right = right, Bottom = bottom });
				}
			}
			return result;
		}
		finally
		{
			Marshal.FreeHGlobal (pixelsPtr);
		}
	}
	public static IntPtr CreateRegionFromRects (List<RECT> rects)
	{
		if (rects.Count == 0) return IntPtr.Zero;
		int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
		foreach (var r in rects)
		{
			if (r.Left < minX) minX = r.Left;
			if (r.Top < minY) minY = r.Top;
			if (r.Right > maxX) maxX = r.Right;
			if (r.Bottom > maxY) maxY = r.Bottom;
		}
		int count = rects.Count;
		int headerSize = Marshal.SizeOf (typeof (RGNDATAHEADER));
		int rectSize = Marshal.SizeOf (typeof (RECT));
		int totalSize = headerSize + count * rectSize;
		IntPtr rgnDataPtr = Marshal.AllocHGlobal (totalSize);
		try
		{
			var header = new RGNDATAHEADER {
				dwSize = headerSize,
				iType = RDH_RECTANGLES,
				nCount = count,
				nRgnSize = count * rectSize,
				rcBound = new RECT { Left = minX, Top = minY, Right = maxX, Bottom = maxY }
			};
			Marshal.StructureToPtr (header, rgnDataPtr, false);
			IntPtr rectsPtr = new IntPtr (rgnDataPtr.ToInt64 () + headerSize);
			for (int i = 0; i < count; i++)
			{
				Marshal.StructureToPtr (rects [i], new IntPtr (rectsPtr.ToInt64 () + i * rectSize), false);
			}
			return ExtCreateRegion (IntPtr.Zero, (uint)totalSize, rgnDataPtr);
		}
		finally
		{
			Marshal.FreeHGlobal (rgnDataPtr);
		}
	}
}