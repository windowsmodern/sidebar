using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Sidebar;

namespace WindowsModern.StickyNote
{
	/// <summary>
	/// StickyNoteBubble.xaml 的交互逻辑
	/// </summary>
	public partial class StickyNoteBubble: Window, IWin32WindowInterop, IWindowInterop
	{
		bool layeredTransparency = false;
		public StickyNoteBubble ()
		{
			layeredTransparency = DWMAPI.IsElderWindows () && !(Tile.Instance?.Features?.Config?.ForceTransparency ?? false);
			InitializeComponent ();
			if (layeredTransparency) AllowsTransparency = false;
			InteropHelper = new WindowInteropHelper (this);
			var colorObj = TryFindResource ("TileStickyNoteFillDefault");
			if (colorObj == null) colorObj = Color.FromRgb (0xFB, 0xF7, 0xCB);
			var initStoreData = new StickyNoteStoreData () {
				BackgroundColor = ((Color)colorObj).ToDrawingColor () ?? System.Drawing.Color.FromArgb (0xFB, 0xF7, 0xCB)
			};
			NoteStoreData = initStoreData;
		}
		public void OnThemeChanged ()
		{
			UpdateImages ();
		}
		private StickyNoteStoreData noteStoreData;
		public StickyNoteStoreData NoteStoreData
		{
			get { return noteStoreData; }
			set
			{
				noteStoreData = value;
				UpdateImages ();
			}
		}
		public HWND HWnd => Handle;
		public WindowInteropHelper InteropHelper { get; private set; }
		public IntPtr Handle => InteropHelper?.Handle ?? IntPtr.Zero;
		public IntPtr WndOwner => InteropHelper?.Owner ?? IntPtr.Zero;
		public void UpdateBackground ()
		{
			var drawingColor = noteStoreData?.BackgroundColor ?? System.Drawing.Color.FromArgb (0xFB, 0xF7, 0xCB);
			var grayBackImage = TryFindResource ((noteStoreData?.IsFlat ?? false) ? "TileStickyNoteBubbleBackgroundFlatSticky" : "TileStickyNoteBubbleBackgroundSticky");
			var colorBackImage = ImageColorMapper.MapImageColors (grayBackImage as ImageSource, drawingColor);
			NoteBackground.Source = colorBackImage;
			UpdateMask ();
		}
		public void UpdateImages ()
		{
			var drawingColor = noteStoreData?.BackgroundColor ?? System.Drawing.Color.FromArgb (0xFB, 0xF7, 0xCB);
			UpdateBackground ();
			var grayDiveder = TryFindResource ("TileStickyNoteBubbleMenuItemDividerImage") as ImageSource;
			var colorDivider = ImageColorMapper.MapImageColors (grayDiveder, drawingColor);
			DividerItem.Header = colorDivider;
			var imageItemStyle = TryFindResource ("TileStickyNoteBubbleMenuItemImageItemButton") as Style;
			var isImageItemColored = false;

		}
		private void NoteHeaderThumb_DragDelta (object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
		{
			Left += e.HorizontalChange;
			Top += e.VerticalChange;
		}
		private void CloseButton_Click (object sender, RoutedEventArgs e)
		{
			try { Close (); } catch { }
		}
		private void ThumbSizeChanger_DragDelta (object sender, System.Windows.Controls.Primitives.DragDeltaEventArgs e)
		{
			var minW = double.IsNaN (MinWidth) ? 0 : MinWidth;
			var minH = double.IsNaN (MinHeight) ? 0 : MinHeight;
			var maxW = double.IsNaN (MaxWidth) ? double.PositiveInfinity : MaxWidth;
			var maxH = double.IsNaN (MaxHeight) ? double.PositiveInfinity : MaxHeight;
			Width = Range (Width + e.HorizontalChange, 0, minW, maxW);
			Height = Range (Height + e.VerticalChange, 0, minH, maxH);
		}
		private void UpdateMinHeight ()
		{
			MinHeight = Math.Max (NoteControlPart.ActualHeight + NoteHeader.ActualHeight + NoteSizer.ActualHeight, 0);
		}
		private static double Range (double value, params double [] edge)
		{
			var min = edge.Min ();
			var max = edge.Max ();
			if (min == max) throw new ArgumentException ("Invalid params: the maximun number equals the minimun number.", nameof (edge));
			return Math.Min (Math.Max (value, min), max);
		}
		private void Window_Loaded (object sender, RoutedEventArgs e)
		{
			UpdateImages ();
		}
		private void Window_Closed (object sender, EventArgs e)
		{
			InteropHelper = null;
			Utilities.ReleaseLargeResourcesAsync ();
		}
		private void Window_SourceInitialized (object sender, EventArgs e)
		{
			var hwnd = HWnd;
			if (hwnd.IsValid)
			{
				hwnd.Styles &= Sidebar.Win32.WindowStyles.WS_BORDER;
				//hwnd.StylesEx |= Sidebar.Win32.ExtendedWindowStyles.WS_EX_LAYERED;
			}
		}
		private Dictionary<string, MaskRegionHelper.MaskRectData> _maskCache = new Dictionary<string, MaskRegionHelper.MaskRectData> ();
		private void UpdateMask ()
		{
			if (!IsLoaded || Handle == IntPtr.Zero) return;
			double dpiX = 1.0;
			double dpiY = 1.0;
			var source = PresentationSource.FromVisual (this);
			if (source != null && source.CompositionTarget != null)
			{
				dpiX = source.CompositionTarget.TransformToDevice.M11;
				dpiY = source.CompositionTarget.TransformToDevice.M22;
			}
			bool isFlat = noteStoreData?.IsFlat ?? false;
			string cacheKey = "Mask_" + isFlat + "_" + dpiX.ToString ("F2") + "_" + dpiY.ToString ("F2");
			MaskRegionHelper.MaskRectData maskData;
			if (!_maskCache.TryGetValue (cacheKey, out maskData))
			{
				var prefix = isFlat ? "TileStickyNoteBubbleWindowFlatMask" : "TileStickyNoteBubbleWindowMask";
				var maskLT = ConvertToBgra32 (TryFindResource (prefix + "LT") as ImageSource);
				var maskRT = ConvertToBgra32 (TryFindResource (prefix + "RT") as ImageSource);
				var maskLB = ConvertToBgra32 (TryFindResource (prefix + "LB") as ImageSource);
				var maskRB = ConvertToBgra32 (TryFindResource (prefix + "RB") as ImageSource);
				maskData = new MaskRegionHelper.MaskRectData ();
				if (maskLT != null) maskData.Corners.Add (MaskRegionHelper.ExtractRects (maskLT, dpiX, dpiY, 0));
				if (maskRT != null) maskData.Corners.Add (MaskRegionHelper.ExtractRects (maskRT, dpiX, dpiY, 1));
				if (maskLB != null) maskData.Corners.Add (MaskRegionHelper.ExtractRects (maskLB, dpiX, dpiY, 2));
				if (maskRB != null) maskData.Corners.Add (MaskRegionHelper.ExtractRects (maskRB, dpiX, dpiY, 3));
				_maskCache [cacheKey] = maskData;
			}
			int w = (int)Math.Round (ActualWidth * dpiX);
			int h = (int)Math.Round (ActualHeight * dpiY);
			if (w <= 0 || h <= 0) return;
			var finalRects = new List<MaskRegionHelper.RECT> ();
			foreach (var corner in maskData.Corners)
			{
				int offsetX = 0;
				int offsetY = 0;
				switch (corner.Alignment)
				{
					case 0:
						offsetX = 0;
						offsetY = 0;
						break;
					case 1:
						offsetX = w - corner.Width;
						offsetY = 0;
						break;
					case 2:
						offsetX = 0;
						offsetY = h - corner.Height;
						break;
					case 3:
						offsetX = w - corner.Width;
						offsetY = h - corner.Height;
						break;
				}
				foreach (var r in corner.Rects)
				{
					finalRects.Add (new MaskRegionHelper.RECT {
						Left = r.Left + offsetX,
						Top = r.Top + offsetY,
						Right = r.Right + offsetX,
						Bottom = r.Bottom + offsetY
					});
				}
			}
			IntPtr cornerRgn = MaskRegionHelper.CreateRegionFromRects (finalRects);
			if (cornerRgn == IntPtr.Zero) return;
			IntPtr mainRgn = MaskRegionHelper.CreateRectRgn (0, 0, w, h);
			if (mainRgn != IntPtr.Zero)
			{
				MaskRegionHelper.CombineRgn (mainRgn, mainRgn, cornerRgn, MaskRegionHelper.RGN_DIFF);
				MaskRegionHelper.SetWindowRgn (Handle, mainRgn, true);
			}
			MaskRegionHelper.DeleteObject (cornerRgn);
		}
		private BitmapSource ConvertToBgra32 (ImageSource imageSource)
		{
			if (imageSource == null) return null;
			var bitmapSource = imageSource as BitmapSource;
			if (bitmapSource == null) return null;
			if (bitmapSource.Format == PixelFormats.Bgra32) return bitmapSource;
			var formatted = new FormatConvertedBitmap ();
			formatted.BeginInit ();
			formatted.Source = bitmapSource;
			formatted.DestinationFormat = PixelFormats.Bgra32;
			formatted.EndInit ();
			formatted.Freeze ();
			return formatted;
		}
		private void Window_SizeChanged (object sender, System.Windows.SizeChangedEventArgs e)
		{
			UpdateMask ();
		}
		private BitmapSource ScaleMask (BitmapSource mask, double dpiX, double dpiY)
		{
			if (Math.Abs (dpiX - 1.0) < 0.001 && Math.Abs (dpiY - 1.0) < 0.001) return mask;
			var transformed = new TransformedBitmap ();
			transformed.BeginInit ();
			transformed.Source = mask;
			transformed.Transform = new ScaleTransform (dpiX, dpiY);
			transformed.EndInit ();
			transformed.Freeze ();
			if (transformed.Format == PixelFormats.Bgra32) return transformed;
			var formatted = new FormatConvertedBitmap ();
			formatted.BeginInit ();
			formatted.Source = transformed;
			formatted.DestinationFormat = PixelFormats.Bgra32;
			formatted.EndInit ();
			formatted.Freeze ();
			return formatted;
		}
	}
}
