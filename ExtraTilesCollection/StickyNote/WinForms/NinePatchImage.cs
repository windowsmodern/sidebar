using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace WindowsModern.StickyNote
{
	public sealed class NinePatchImage: IDisposable
	{
		private Bitmap _source;
		private int _left, _top, _right, _bottom;
		private Rectangle _srcLT, _srcT, _srcRT;
		private Rectangle _srcL, _srcC, _srcR;
		private Rectangle _srcLB, _srcB, _srcRB;
		public int LeftSlice { get { return _left; } }
		public int TopSlice { get { return _top; } }
		public int RightSlice { get { return _right; } }
		public int BottomSlice { get { return _bottom; } }
		public Bitmap Source { get { return _source; } }
		public NinePatchImage (Bitmap source, int left, int top, int right, int bottom)
		{
			if (source == null) throw new ArgumentNullException ("source");
			if (left < 0 || top < 0 || right < 0 || bottom < 0) throw new ArgumentException ("Slice size cannot be negative");
			if (left + right > source.Width || top + bottom > source.Height) throw new ArgumentException ("Slice size exceeds image bounds");
			_source = source;
			_left = left; _top = top; _right = right; _bottom = bottom;
			int w = source.Width;
			int h = source.Height;
			int mw = w - left - right;
			int mh = h - top - bottom;
			_srcLT = new Rectangle (0, 0, left, top);
			_srcT = new Rectangle (left, 0, mw, top);
			_srcRT = new Rectangle (w - right, 0, right, top);
			_srcL = new Rectangle (0, top, left, mh);
			_srcC = new Rectangle (left, top, mw, mh);
			_srcR = new Rectangle (w - right, top, right, mh);
			_srcLB = new Rectangle (0, h - bottom, left, bottom);
			_srcB = new Rectangle (left, h - bottom, mw, bottom);
			_srcRB = new Rectangle (w - right, h - bottom, right, bottom);
		}
		public NinePatchImage (string path, int left, int top, int right, int bottom) : this (new Bitmap (path), left, top, right, bottom) { }
		public void Draw (Graphics g, Rectangle dest)
		{
			if (g == null) throw new ArgumentNullException ("g");
			int dw = dest.Width, dh = dest.Height;
			int l = _left, t = _top, r = _right, b = _bottom;
			int mw = dw - l - r;
			int mh = dh - t - b;
			if (mw < 0) mw = 0;
			if (mh < 0) mh = 0;
			DrawPart (g, _srcLT, new Rectangle (dest.X, dest.Y, l, t));
			DrawPart (g, _srcRT, new Rectangle (dest.Right - r, dest.Y, r, t));
			DrawPart (g, _srcLB, new Rectangle (dest.X, dest.Bottom - b, l, b));
			DrawPart (g, _srcRB, new Rectangle (dest.Right - r, dest.Bottom - b, r, b));
			DrawPart (g, _srcT, new Rectangle (dest.X + l, dest.Y, mw, t));
			DrawPart (g, _srcB, new Rectangle (dest.X + l, dest.Bottom - b, mw, b));
			DrawPart (g, _srcL, new Rectangle (dest.X, dest.Y + t, l, mh));
			DrawPart (g, _srcR, new Rectangle (dest.Right - r, dest.Y + t, r, mh));
			DrawPart (g, _srcC, new Rectangle (dest.X + l, dest.Y + t, mw, mh));
		}
		private void DrawPart (Graphics g, Rectangle src, Rectangle dst)
		{
			if (dst.Width <= 0 || dst.Height <= 0) return;
			g.DrawImage (_source, dst, src, GraphicsUnit.Pixel);
		}
		public void Dispose ()
		{
			if (_source != null) { _source.Dispose (); _source = null; }
		}
	}
}