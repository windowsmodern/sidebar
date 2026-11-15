using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace WindowsModern.StickyNote
{
	public static class StickyNoteBackgroundFactory
	{
		public static Bitmap Create (int width, int height, int sliceLeft, int sliceTop, int sliceRight, int sliceBottom)
		{
			Bitmap bmp = new Bitmap (width, height, PixelFormat.Format32bppArgb);
			using (Graphics g = Graphics.FromImage (bmp))
			{
				g.SmoothingMode = SmoothingMode.AntiAlias;
				g.Clear (Color.Transparent);
				int radius = 8;
				int dogearW = sliceRight;
				int dogearH = sliceBottom;
				Color bg = Color.FromArgb (255, 251, 247, 200);
				Color dogear = Color.FromArgb (255, 230, 225, 175);
				Color border = Color.FromArgb (255, 210, 205, 160);
				using (GraphicsPath path = new GraphicsPath ())
				{
					path.AddArc (0, 0, radius * 2, radius * 2, 180, 90);
					path.AddLine (radius, 0, width - radius, 0);
					path.AddArc (width - radius * 2, 0, radius * 2, radius * 2, 270, 90);
					path.AddLine (width, radius, width, height - dogearH);
					path.AddLine (width, height - dogearH, width - dogearW, height);
					path.AddLine (width - dogearW, height, radius, height);
					path.AddArc (0, height - radius * 2, radius * 2, radius * 2, 90, 90);
					path.AddLine (0, height - radius, 0, radius);
					path.CloseFigure ();
					using (SolidBrush b = new SolidBrush (bg)) g.FillPath (b, path);
					using (Pen p = new Pen (border, 1)) g.DrawPath (p, path);
				}
				Point [] tri = new Point []
				{
					new Point(width - dogearW, height),
					new Point(width, height - dogearH),
					new Point(width - dogearW, height - dogearH)
				};
				using (SolidBrush b = new SolidBrush (dogear)) g.FillPolygon (b, tri);
				using (Pen p = new Pen (border, 1)) g.DrawPolygon (p, tri);
			}
			return bmp;
		}
	}
}