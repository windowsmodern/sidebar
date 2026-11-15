using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace WindowsModern.StickyNote
{
	public partial class StickyNoteForm: LayeredForm
	{
		private NinePatchImage _ninePatch;
		private RichTextBox _textBox;
		private bool _isRendering;
		private int _sliceLeft = 41;
		private int _sliceTop = 37;
		private int _sliceRight = 5;
		private int _sliceBottom = 0;
		public StickyNoteForm ()
		{
			InitializeComponent ();
			UpdateIcon ();
			LoadResourcesFromTheme ();
			_textBox = new RichTextBox ();
			_textBox.BorderStyle = BorderStyle.None;
			_textBox.BackColor = GetCenterColor ();
			_textBox.ForeColor = Color.FromArgb (255, 51, 51, 51);
			_textBox.Font = new Font ("Segoe UI", 10f);
			_textBox.ScrollBars = RichTextBoxScrollBars.Vertical;
			_textBox.AcceptsTab = true;
			Controls.Add (_textBox);
			AllowResize = true;
			ResizeBorderWidth = 8;
			LayeredOpacity = 255;
			ClientSize = new Size (306, 231);
		}
		public void RefreshTheme ()
		{
			UpdateIcon ();
			LoadResourcesFromTheme ();
			if (_textBox != null)
			{
				_textBox.BackColor = GetCenterColor ();
			}
			LayoutContent ();
			RedrawLayeredBitmap ();
		}
		private void UpdateIcon ()
		{
			if (IsDesignTime) return;
			if (System.Windows.Application.Current != null)
			{
				try
				{
					System.Windows.Media.ImageSource iconSource = System.Windows.Application.Current.TryFindResource ("TileStickyNoteWindowMultipleStickiesIcon") as System.Windows.Media.ImageSource;
					if (iconSource != null)
					{
						Bitmap bmp = ConvertImageSourceToBitmap (iconSource);
						if (bmp != null)
						{
							IntPtr hIcon = bmp.GetHicon ();
							this.Icon = System.Drawing.Icon.FromHandle (hIcon);
						}
					}
				}
				catch { }
			}
		}
		private void LoadResourcesFromTheme ()
		{
			Bitmap bgBitmap = null;
			if (System.Windows.Application.Current != null)
			{
				try
				{
					System.Windows.Media.ImageSource imgSrc = System.Windows.Application.Current.TryFindResource ("TileStickyNoteWindowBackground") as System.Windows.Media.ImageSource;
					if (imgSrc != null)
					{
						bgBitmap = ConvertImageSourceToBitmap (imgSrc);
					}
					System.Windows.Thickness thickness = (System.Windows.Thickness)System.Windows.Application.Current.TryFindResource ("TileStickyNoteWindowBackgroundNineGrid");
					_sliceLeft = (int)thickness.Left;
					_sliceTop = (int)thickness.Top;
					_sliceRight = (int)thickness.Right;
					_sliceBottom = (int)thickness.Bottom;
				}
				catch { }
			}
			if (bgBitmap == null)
			{
				bgBitmap = StickyNoteBackgroundFactory.Create (306, 231, _sliceLeft, _sliceTop, _sliceRight, _sliceBottom);
			}
			if (_ninePatch != null)
			{
				_ninePatch.Dispose ();
				_ninePatch = null;
			}
			_ninePatch = new NinePatchImage (bgBitmap, _sliceLeft, _sliceTop, _sliceRight, _sliceBottom);
		}
		private static Bitmap ConvertImageSourceToBitmap (System.Windows.Media.ImageSource imageSource)
		{
			if (imageSource == null) return null;
			BitmapSource bitmapSource = imageSource as BitmapSource;
			if (bitmapSource == null)
			{
				int width = (int)(imageSource.Width > 0 ? imageSource.Width : 306);
				int height = (int)(imageSource.Height > 0 ? imageSource.Height : 231);
				RenderTargetBitmap rtb = new RenderTargetBitmap (width, height, 96, 96, System.Windows.Media.PixelFormats.Pbgra32);
				System.Windows.Media.DrawingVisual drawingVisual = new System.Windows.Media.DrawingVisual ();
				using (System.Windows.Media.DrawingContext context = drawingVisual.RenderOpen ())
				{
					context.DrawImage (imageSource, new System.Windows.Rect (0, 0, width, height));
				}
				rtb.Render (drawingVisual);
				bitmapSource = rtb;
			}
			using (MemoryStream outStream = new MemoryStream ())
			{
				PngBitmapEncoder enc = new PngBitmapEncoder ();
				enc.Frames.Add (BitmapFrame.Create (bitmapSource));
				enc.Save (outStream);
				outStream.Seek (0, SeekOrigin.Begin);
				using (var temp = new Bitmap (outStream))
				{
					return new Bitmap (temp);
				}
			}
		}
		private Color GetCenterColor ()
		{
			if (_ninePatch == null || _ninePatch.Source == null) return Color.FromArgb (255, 251, 247, 200);
			Bitmap src = _ninePatch.Source;
			int cx = src.Width / 2;
			int cy = src.Height / 2;
			Color c = src.GetPixel (cx, cy);
			return Color.FromArgb (255, c.R, c.G, c.B);
		}
		protected override void OnShown (EventArgs e)
		{
			base.OnShown (e);
			if (IsDesignTime) return;
			LayoutContent ();
			RedrawLayeredBitmap ();
		}
		protected override void OnResize (EventArgs e)
		{
			base.OnResize (e);
			if (IsDesignTime) return;
			if (_isRendering) return;
			LayoutContent ();
			RedrawLayeredBitmap ();
		}
		private void LayoutContent ()
		{
			if (_textBox == null || _ninePatch == null) return;
			int l = _ninePatch.LeftSlice, t = _ninePatch.TopSlice;
			int r = _ninePatch.RightSlice, b = _ninePatch.BottomSlice;
			int w = ClientSize.Width - l - r;
			int h = ClientSize.Height - t - b;
			if (w < 10) w = 10;
			if (h < 10) h = 10;
			_textBox.Bounds = new Rectangle (l, t, w, h);
		}
		private void RedrawLayeredBitmap ()
		{
			if (IsDesignTime || _isRendering || _ninePatch == null || !IsHandleCreated) return;
			_isRendering = true;
			try
			{
				int w = Math.Max (1, ClientSize.Width);
				int h = Math.Max (1, ClientSize.Height);
				Bitmap bmp = new Bitmap (w, h, PixelFormat.Format32bppArgb);
				using (Graphics g = Graphics.FromImage (bmp))
				{
					g.Clear (Color.Transparent);
					g.SmoothingMode = SmoothingMode.AntiAlias;
					g.InterpolationMode = InterpolationMode.HighQualityBicubic;
					g.PixelOffsetMode = PixelOffsetMode.HighQuality;
					g.CompositingQuality = CompositingQuality.HighQuality;
					_ninePatch.Draw (g, new Rectangle (0, 0, w, h));
				}
				SetBitmap (bmp, LayeredOpacity);
			}
			finally
			{
				_isRendering = false;
			}
		}
		protected override void Dispose (bool disposing)
		{
			if (disposing)
			{
				if (_textBox != null) { _textBox.Dispose (); _textBox = null; }
				if (_ninePatch != null) { _ninePatch.Dispose (); _ninePatch = null; }
			}
			base.Dispose (disposing);
		}
	}
}