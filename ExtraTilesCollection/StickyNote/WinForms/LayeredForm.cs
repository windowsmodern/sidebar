using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowsModern.StickyNote
{
	public class LayeredForm: Form
	{
		[StructLayout (LayoutKind.Sequential)]
		private struct POINT { public int X, Y; public POINT (int x, int y) { X = x; Y = y; } }
		[StructLayout (LayoutKind.Sequential)]
		private struct SIZE { public int CX, CY; public SIZE (int cx, int cy) { CX = cx; CY = cy; } }
		[StructLayout (LayoutKind.Sequential, Pack = 1)]
		private struct BLENDFUNCTION { public byte BlendOp; public byte BlendFlags; public byte SourceConstantAlpha; public byte AlphaFormat; }
		[DllImport ("user32.dll", SetLastError = true)]
		private static extern bool UpdateLayeredWindow (IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);
		[DllImport ("user32.dll")]
		private static extern IntPtr GetDC (IntPtr hWnd);
		[DllImport ("user32.dll")]
		private static extern int ReleaseDC (IntPtr hWnd, IntPtr hDC);
		[DllImport ("gdi32.dll")]
		private static extern IntPtr CreateCompatibleDC (IntPtr hDC);
		[DllImport ("gdi32.dll")]
		private static extern bool DeleteDC (IntPtr hdc);
		[DllImport ("gdi32.dll")]
		private static extern IntPtr SelectObject (IntPtr hDC, IntPtr hObject);
		[DllImport ("gdi32.dll")]
		private static extern bool DeleteObject (IntPtr hObject);
		[DllImport ("user32.dll")]
		private static extern IntPtr SendMessage (IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
		[DllImport ("user32.dll")]
		private static extern bool ReleaseCapture ();
		private const int WS_EX_LAYERED = 0x00080000;
		private const int ULW_ALPHA = 0x00000002;
		private const byte AC_SRC_OVER = 0x00;
		private const byte AC_SRC_ALPHA = 0x01;
		private const int WM_NCHITTEST = 0x0084;
		private const int WM_LBUTTONDOWN = 0x0201;
		private const int WM_NCLBUTTONDOWN = 0x00A1;
		private const int HTTRANSPARENT = -1;
		private const int HTCLIENT = 1;
		private const int HTCAPTION = 2;
		private const int HTLEFT = 10;
		private const int HTRIGHT = 11;
		private const int HTTOP = 12;
		private const int HTTOPLEFT = 13;
		private const int HTTOPRIGHT = 14;
		private const int HTBOTTOM = 15;
		private const int HTBOTTOMLEFT = 16;
		private const int HTBOTTOMRIGHT = 17;
		private Bitmap _layeredBitmap;
		private byte _opacity = 255;
		private bool _isUpdatingBitmap;
		private int _lastLeft, _lastTop;
		protected bool IsDesignTime
		{
			get { return LicenseManager.UsageMode == LicenseUsageMode.Designtime; }
		}
		public byte LayeredOpacity
		{
			get { return _opacity; }
			set
			{
				if (_opacity == value) return;
				_opacity = value;
				if (_layeredBitmap != null) PushBitmap (_layeredBitmap, _opacity);
			}
		}
		public bool AllowResize = false;
		public int ResizeBorderWidth = 6;
		public LayeredForm ()
		{
			FormBorderStyle = FormBorderStyle.None;
			DoubleBuffered = true;
			SetStyle (ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
		}
		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams cp = base.CreateParams;
				if (!IsDesignTime) cp.ExStyle |= WS_EX_LAYERED;
				return cp;
			}
		}
		protected override void OnPaint (PaintEventArgs e)
		{
			base.OnPaint (e);
			if (IsDesignTime)
			{
				e.Graphics.Clear (Color.FromArgb (255, 251, 247, 200));
				using (Pen p = new Pen (Color.Gray, 2)) e.Graphics.DrawRectangle (p, 1, 1, Width - 3, Height - 3);
			}
		}
		public void SetBitmap (Bitmap bitmap, byte opacity)
		{
			if (IsDesignTime) return;
			if (bitmap == null) throw new ArgumentNullException ("bitmap");
			if (!IsHandleCreated) return;
			if (bitmap.PixelFormat != PixelFormat.Format32bppArgb && bitmap.PixelFormat != PixelFormat.Format32bppPArgb) throw new ArgumentException ("Bitmap must be 32bpp ARGB");
			PushBitmap (bitmap, opacity);
			if (_layeredBitmap != null && !ReferenceEquals (_layeredBitmap, bitmap)) _layeredBitmap.Dispose ();
			_layeredBitmap = bitmap;
		}
		private void PushBitmap (Bitmap bitmap, byte opacity)
		{
			IntPtr screenDc = GetDC (IntPtr.Zero);
			IntPtr memDc = CreateCompatibleDC (screenDc);
			IntPtr hBitmap = IntPtr.Zero;
			IntPtr oldBitmap = IntPtr.Zero;
			try
			{
				hBitmap = bitmap.GetHbitmap (Color.FromArgb (0));
				oldBitmap = SelectObject (memDc, hBitmap);
				SIZE size = new SIZE (bitmap.Width, bitmap.Height);
				POINT src = new POINT (0, 0);
				POINT dst = new POINT (Left, Top);
				BLENDFUNCTION blend = new BLENDFUNCTION ();
				blend.BlendOp = AC_SRC_OVER;
				blend.BlendFlags = 0;
				blend.SourceConstantAlpha = opacity;
				blend.AlphaFormat = AC_SRC_ALPHA;
				UpdateLayeredWindow (Handle, screenDc, ref dst, ref size, memDc, ref src, 0, ref blend, ULW_ALPHA);
				_lastLeft = Left;
				_lastTop = Top;
			}
			finally
			{
				ReleaseDC (IntPtr.Zero, screenDc);
				if (hBitmap != IntPtr.Zero)
				{
					SelectObject (memDc, oldBitmap);
					DeleteObject (hBitmap);
				}
				DeleteDC (memDc);
			}
		}
		protected virtual bool HitTestTransparent (int x, int y)
		{
			if (IsDesignTime) return false;
			if (_layeredBitmap == null) return false;
			if (x < 0 || y < 0 || x >= _layeredBitmap.Width || y >= _layeredBitmap.Height) return true;
			return _layeredBitmap.GetPixel (x, y).A < 10;
		}
		protected virtual bool IsPointOnChild (Point client)
		{
			for (int i = 0; i < Controls.Count; i++)
			{
				Control c = Controls [i];
				if (c.Visible && c.Bounds.Contains (client)) return true;
			}
			return false;
		}
		protected virtual int HitTestResize (int x, int y)
		{
			if (!AllowResize) return HTCLIENT;
			int w = Width, h = Height, b = ResizeBorderWidth;
			bool left = x < b, right = x >= w - b;
			bool top = y < b, bottom = y >= h - b;
			if (top && left) return HTTOPLEFT;
			if (top && right) return HTTOPRIGHT;
			if (bottom && left) return HTBOTTOMLEFT;
			if (bottom && right) return HTBOTTOMRIGHT;
			if (left) return HTLEFT;
			if (right) return HTRIGHT;
			if (top) return HTTOP;
			if (bottom) return HTBOTTOM;
			return HTCLIENT;
		}
		protected override void WndProc (ref Message m)
		{
			if (IsDesignTime)
			{
				base.WndProc (ref m);
				return;
			}
			if (m.Msg == WM_NCHITTEST)
			{
				int sx = (short)(m.LParam.ToInt32 () & 0xFFFF);
				int sy = (short)((m.LParam.ToInt32 () >> 16) & 0xFFFF);
				Point client = PointToClient (new Point (sx, sy));
				if (HitTestTransparent (client.X, client.Y)) { m.Result = (IntPtr)HTTRANSPARENT; return; }
				if (IsPointOnChild (client)) { m.Result = (IntPtr)HTCLIENT; return; }
				int ht = HitTestResize (client.X, client.Y);
				if (ht != HTCLIENT) { m.Result = (IntPtr)ht; return; }
				m.Result = (IntPtr)HTCAPTION;
				return;
			}
			if (m.Msg == WM_LBUTTONDOWN)
			{
				int sx = (short)(m.LParam.ToInt32 () & 0xFFFF);
				int sy = (short)((m.LParam.ToInt32 () >> 16) & 0xFFFF);
				Point client = PointToClient (new Point (sx, sy));
				if (!HitTestTransparent (client.X, client.Y) && !IsPointOnChild (client))
				{
					int ht = HitTestResize (client.X, client.Y);
					if (ht != HTCLIENT)
					{
						ReleaseCapture ();
						SendMessage (Handle, WM_NCLBUTTONDOWN, (IntPtr)ht, IntPtr.Zero);
						return;
					}
				}
			}
			base.WndProc (ref m);
		}
		protected override void OnLocationChanged (EventArgs e)
		{
			base.OnLocationChanged (e);
			if (IsDesignTime) return;
			if (!_isUpdatingBitmap && _layeredBitmap != null && (Left != _lastLeft || Top != _lastTop))
			{
				_isUpdatingBitmap = true;
				try { PushBitmap (_layeredBitmap, _opacity); }
				finally { _isUpdatingBitmap = false; }
			}
		}
		protected override void Dispose (bool disposing)
		{
			if (disposing && _layeredBitmap != null)
			{
				_layeredBitmap.Dispose ();
				_layeredBitmap = null;
			}
			base.Dispose (disposing);
		}
	}
}