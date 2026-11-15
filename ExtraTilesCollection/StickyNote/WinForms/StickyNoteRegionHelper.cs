using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace WindowsModern.StickyNote
{
	public static class StickyNoteRegionHelper
	{
		public static Region GetStickyNoteRegion (Size windowSize, Stream leftMaskStream, Stream rightMaskStream)
		{
			GraphicsPath path = new GraphicsPath (FillMode.Winding);
			Size leftSize, rightSize;
			Point [] leftPoints = GetCornerPoints (leftMaskStream, true, out leftSize);
			Point [] rightPoints = GetCornerPoints (rightMaskStream, false, out rightSize);
			int width = windowSize.Width;
			int height = windowSize.Height;
			path.AddLine (width, height, 0, height);
			path.AddLine (0, height, 0, leftSize.Height);
			path.AddCurve (leftPoints);
			int num = width - 1 - rightSize.Width;
			path.AddLine (leftSize.Width, 0, num, 0);
			Point [] array = new Point [rightPoints.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array [i] = new Point (rightPoints [i].X + num + 1, rightPoints [i].Y);
			}
			path.AddCurve (array);
			path.AddLine (width, rightSize.Height, width, height);
			return new Region (path);
		}
		private static Point [] GetCornerPoints (Stream maskStream, bool leftCorner, out Size size)
		{
			Bitmap bitmap = new Bitmap (maskStream);
			Point [] array = new Point [bitmap.Height];
			int num = 0;
			Color color = leftCorner ? bitmap.GetPixel (bitmap.Width - 1, bitmap.Height - 1) : bitmap.GetPixel (bitmap.Width - 1, 0);
			int start = leftCorner ? bitmap.Height - 1 : 0;
			int end = leftCorner ? -1 : bitmap.Height;
			int step = leftCorner ? -1 : 1;
			for (int i = start; i != end; i += step)
			{
				bool flag = false;
				for (int j = 0; j < bitmap.Width; j++)
				{
					if (bitmap.GetPixel (j, i).Equals (color))
					{
						array [num++] = new Point (j, i);
						flag = true;
						break;
					}
				}
				if (!flag) array [num++] = new Point (bitmap.Width, i);
			}
			size = new Size (bitmap.Width, bitmap.Height);
			bitmap.Dispose ();
			return array;
		}
	}
}