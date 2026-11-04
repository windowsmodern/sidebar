using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Sidebar
{
	/// <summary>
	/// FlyoutWindow.xaml 的交互逻辑
	/// </summary>
	public partial class FlyoutWindow: Window, IWindowInterop, IWin32WindowInterop, IFlyoutToolMembers
	{
		TileControl tileComp = null;
		public TileControl Tile => tileComp;
		public FlyoutWindow (TileControl tc)
		{
			InitializeComponent ();
			tileComp = tc;
			if (DWMAPI.IsElderWindows () && !App.CurrentUserConfig.ForceTransparency) AllowsTransparency = false;
			InteropHelper = new WindowInteropHelper (this);
		}
		#region Public Members
		public IntPtr Handle => InteropHelper.Handle;
		public HWND HWnd => (HWND)Handle;
		public WindowInteropHelper InteropHelper { get; set; }
		public IntPtr WndOwner => InteropHelper.Owner;
		public System.Drawing.Rectangle TileRect
		{
			get
			{
				if (tileComp == null || !tileComp.IsLoaded) return System.Drawing.Rectangle.Empty;
				var mainWindow = Window.GetWindow (tileComp);
				if (mainWindow == null) return System.Drawing.Rectangle.Empty;
				var interop = new WindowInteropHelper (mainWindow);
				if (interop == null) return System.Drawing.Rectangle.Empty;
				HWND hwnd = interop.Handle;
				if (hwnd == IntPtr.Zero) return System.Drawing.Rectangle.Empty;
				Win32.RECT windowRect;
				if (!Sidebar.Win32WindowNative.GetWindowRect (hwnd, out windowRect))
					return System.Drawing.Rectangle.Empty;
				var relativeDip = tileComp.TransformToAncestor (mainWindow)
											.Transform (new System.Windows.Point (0, 0));
				var source = PresentationSource.FromVisual (tileComp);
				if (source == null)
					return System.Drawing.Rectangle.Empty;
				Matrix m = source.CompositionTarget.TransformToDevice;
				int pixelOffsetX = (int)Math.Round (relativeDip.X * m.M11);
				int pixelOffsetY = (int)Math.Round (relativeDip.Y * m.M22);
				int tileScreenX = windowRect.Left + pixelOffsetX;
				int tileScreenY = windowRect.Top + pixelOffsetY;
				double pixelWidth = tileComp.ActualWidth * m.M11;
				double pixelHeight = tileComp.ActualHeight * m.M22;
				return new System.Drawing.Rectangle (
					tileScreenX,
					tileScreenY,
					(int)Math.Round (pixelWidth),
					(int)Math.Round (pixelHeight)
				);
			}
		}
		public System.Drawing.Size PixelSize => HWnd.Size;
		public ContextMenu FlyoutContextMenu
		{
			get { return FlyoutHeader.ContextMenu; }
			set { FlyoutHeader.ContextMenu = value; }
		}
		#endregion
		public SidebarWindow SidebarHost => tileComp?.SidebarHost;
		private void FlyoutContentContainer_SizeChanged (object sender, System.Windows.SizeChangedEventArgs e)
		{
			if (WindowStyle == WindowStyle.None)
			{
				Width = FlyoutContentContainer.Margin.Left + FlyoutContentContainer.Margin.Right + FlyoutContentContainer.ActualWidth;
				Height = FlyoutContentContainer.Margin.Top + FlyoutContentContainer.Margin.Bottom + FlyoutContentContainer.ActualHeight;
			}
			else
			{
				var clientWidth = FlyoutContentContainer.Margin.Left + FlyoutContentContainer.Margin.Right + FlyoutContentContainer.ActualWidth;
				var clientHeight = FlyoutContentContainer.Margin.Top + FlyoutContentContainer.Margin.Bottom + FlyoutContentContainer.ActualHeight;
				double borderWidth = SystemParameters.ResizeFrameVerticalBorderWidth + SystemParameters.FixedFrameVerticalBorderWidth;
				double borderHeight = SystemParameters.ResizeFrameHorizontalBorderHeight + SystemParameters.FixedFrameHorizontalBorderHeight + SystemParameters.CaptionHeight;
				Width = clientWidth + borderWidth;
				Height = clientHeight + borderHeight;
			}
		}
		bool isclosing = false;
		private void Window_Deactivated (object sender, EventArgs e)
		{
			try { if (IsLoaded && !isclosing) Dispatcher.BeginInvoke (new Action (() => Close ()), DispatcherPriority.Background); } catch { }
		}
		private void Window_SourceInitialized (object sender, EventArgs e)
		{
			IntPtr hwnd = this.HWnd;
			bool enableBlur = false;
			object val;
			if (Application.Current.Resources.Contains ("EnableBlur"))
				enableBlur = (bool)Application.Current.Resources ["EnableBlur"];
			if (!AllowsTransparency) enableBlur = false;
			if (!enableBlur)
			{
				if (Application.Current.Resources.Contains ("EnableBlurForFlyout"))
					enableBlur = (bool)Application.Current.Resources ["EnableBlurForFlyout"];
			}
			try { if (Environment.OSVersion.Version.Major > 6) WindowAccent.SetAccentPolicy (hwnd, enableBlur); } catch { }
			if (!enableBlur || !DWMAPI.IsDwmAvailable ())
			{
				DWMAPI.DisableBlur (ref hwnd);
				return;
			}
			DWMAPI.EnableBlur (ref hwnd, IntPtr.Zero);
		}
		public void FixPosition ()
		{
			SidebarHost?.FixFlyoutPosition (this);
		}
		public Task TransToNewHeight (FrameworkElement component, double elderHeight, double? newHeight = default (double?), TimeSpan? timeout = default (TimeSpan?))
		{
			return Task.Factory.StartNew (() => { });
		}
		private void Window_SizeChanged (object sender, System.Windows.SizeChangedEventArgs e)
		{
			FixPosition ();
			return;
			var objIsFlyoutClipRound = TryFindResource ("FlyoutForceClipRoundWindow");
			if (objIsFlyoutClipRound != null && (bool)objIsFlyoutClipRound)
			{
				double ltRadius = (double?)TryFindResource ("FlyoutClipRadiusLeftTop") ?? 0,
					lbRadius = (double?)TryFindResource ("FlyoutClipRadiusLeftBottom") ?? 0,
					rtRadius = (double?)TryFindResource ("FlyoutClipRadiusRightTop") ?? 0,
					rbRadius = (double?)TryFindResource ("FlyoutClipRadiusRightBottom") ?? 0;
				ApplyRoundedRegion (this.GetPixelLeft (), this.GetPixelTop (), this.GetPixelWidth (), this.GetPixelHeight (), ltRadius, lbRadius, rtRadius, rbRadius);
			}
			else ClearRoundedRegion ();
		}
		bool isSetWndRgn = false;
		private void ApplyRoundedRegion (int l, int t, int w, int h, int r1, int r2)
		{
			isSetWndRgn = true;
			IntPtr hRgn = Win32WindowNative.CreateRoundRectRgn (l, t, w, h, r1, r2);
			Win32WindowNative.SetWindowRgn (this.Handle, hRgn, true);
			Win32WindowNative.DeleteObject (hRgn);
		}
		/// <summary>
		/// 设置窗口圆角区域（四个角可独立指定半径，单位像素）。
		/// 半径会被自动限制在 0 ~ min(w,h)/2 之间，避免圆弧重叠导致区域异常。
		/// </summary>
		/// <param name="l">窗口客户区/窗口坐标的左边界（通常是 0）</param>
		/// <param name="t">上边界（通常是 0）</param>
		/// <param name="w">宽度</param>
		/// <param name="h">高度</param>
		/// <param name="leftTopR">左上圆角半径</param>
		/// <param name="leftBottomR">左下圆角半径</param>
		/// <param name="rightTopR">右上圆角半径</param>
		/// <param name="rightBottomR">右下圆角半径</param>
		private void ApplyRoundedRegion (int l, int t, int w, int h, int leftTopR, int leftBottomR, int rightTopR, int rightBottomR)
		{
			if (w <= 0 || h <= 0) return;
			isSetWndRgn = true;
			int maxR = Math.Min (w, h) / 2;
			leftTopR = Math.Max (0, Math.Min (leftTopR, maxR));
			leftBottomR = Math.Max (0, Math.Min (leftBottomR, maxR));
			rightTopR = Math.Max (0, Math.Min (rightTopR, maxR));
			rightBottomR = Math.Max (0, Math.Min (rightBottomR, maxR));
			if (leftTopR == 0 && leftBottomR == 0 && rightTopR == 0 && rightBottomR == 0)
			{
				IntPtr rectRgn = Win32WindowNative.CreateRectRgn (l, t, l + w, t + h);
				Win32WindowNative.SetWindowRgn (this.Handle, rectRgn, true);
				Win32WindowNative.DeleteObject (rectRgn);
				return;
			}
			int right = l + w;
			int bottom = t + h;
			using (var path = new System.Drawing.Drawing2D.GraphicsPath ())
			{
				if (leftTopR > 0)
					path.AddArc (l, t, leftTopR * 2, leftTopR * 2, 180, 90);
				if (rightTopR > 0)
					path.AddArc (right - rightTopR * 2, t, rightTopR * 2, rightTopR * 2, 270, 90);
				if (rightBottomR > 0)
					path.AddArc (right - rightBottomR * 2, bottom - rightBottomR * 2,
								 rightBottomR * 2, rightBottomR * 2, 0, 90);
				if (leftBottomR > 0)
					path.AddArc (l, bottom - leftBottomR * 2,
								 leftBottomR * 2, leftBottomR * 2, 90, 90);
				path.CloseFigure ();
				using (var g = System.Drawing.Graphics.FromHwnd (IntPtr.Zero))
				using (var region = new System.Drawing.Region (path))
				{
					IntPtr hRgn = region.GetHrgn (g);
					if (hRgn != IntPtr.Zero)
					{
						Win32WindowNative.SetWindowRgn (this.Handle, hRgn, true);
						Win32WindowNative.DeleteObject (hRgn);
					}
				}
			}
		}
		/// <summary>
		/// 清除窗口的圆角/自定义区域，恢复为系统默认的矩形区域。
		/// 对应 SetWindowRgn(hWnd, NULL, bRedraw)。
		/// </summary>
		/// <param name="redraw">是否立即重绘窗口（true 时系统会重算裁剪并刷新）</param>
		/// <returns>成功返回 true；句柄无效或调用失败返回 false</returns>
		private bool ClearRoundedRegion (bool redraw = true)
		{
			if (!isSetWndRgn) return !isSetWndRgn;
			var hwnd = this.HWnd;
			if (!hwnd.IsValid) return false;
			return Win32WindowNative.SetWindowRgn (this.Handle, IntPtr.Zero, redraw) != 0;
		}
		/// <summary>
		/// 获取窗口当前所在显示器的 DPI 缩放比例（96 DPI = 1.0）。
		/// 优先 GetDpiForWindow（Win10 1607+，per-monitor）；回退 GetDeviceCaps（系统 DPI）。
		/// </summary>
		private double GetDpiScale ()
		{
			IntPtr hwnd = this.Handle;
			try
			{
				uint dpi = Win32WindowNative.GetDpiForWindow (hwnd);
				if (dpi > 0) return dpi / 96.0;
			}
			catch (EntryPointNotFoundException) { }
			catch (DllNotFoundException) { }
			IntPtr hdc = Win32WindowNative.GetDC (hwnd);
			if (hdc != IntPtr.Zero)
			{
				try
				{
					int dpi = Win32WindowNative.GetDeviceCaps (hdc, 88); // LOGPIXELSX
					if (dpi > 0) return dpi / 96.0;
				}
				finally
				{
					Win32WindowNative.ReleaseDC (hwnd, hdc);
				}
			}
			return 1.0;
		}
		/// <summary>
		/// 设置窗口圆角区域（四个角可独立指定半径，参数为 DIP，内部按当前窗口 DPI 换算为像素）。
		/// </summary>
		/// <param name="l">左边界（像素，通常是 0）</param>
		/// <param name="t">上边界（像素，通常是 0）</param>
		/// <param name="w">宽度（像素）</param>
		/// <param name="h">高度（像素）</param>
		/// <param name="leftTopR">左上圆角半径（DIP，可含小数）</param>
		/// <param name="leftBottomR">左下圆角半径（DIP）</param>
		/// <param name="rightTopR">右上圆角半径（DIP）</param>
		/// <param name="rightBottomR">右下圆角半径（DIP）</param>
		private void ApplyRoundedRegion (int l, int t, int w, int h, double leftTopR, double leftBottomR, double rightTopR, double rightBottomR)
		{
			double scale = GetDpiScale ();
			ApplyRoundedRegion (l, t, w, h,
				(int)Math.Round (leftTopR * scale, MidpointRounding.AwayFromZero),
				(int)Math.Round (leftBottomR * scale, MidpointRounding.AwayFromZero),
				(int)Math.Round (rightTopR * scale, MidpointRounding.AwayFromZero),
				(int)Math.Round (rightBottomR * scale, MidpointRounding.AwayFromZero));
		}
		private void Window_Closed (object sender, EventArgs e)
		{
			Utilities.ReleaseLargeResourcesAsync ();
		}
	}
	internal sealed class FlyoutDoubleClickHelper: IDisposable
	{
		private const int DoubleTapIntervalMs = 300;
		private const double DoubleTapDistancePx = 16.0;
		private readonly FrameworkElement _target;
		private readonly DependencyObject _filterRoot;
		private readonly Func<DependencyObject, bool> _customIgnore;
		private DateTime _lastStylusTime = DateTime.MinValue;
		private System.Windows.Point _lastStylusPos;
		private DateTime _lastTouchTime = DateTime.MinValue;
		private System.Windows.Point _lastTouchPos;
		private bool _attached;
		private bool _disposed;
		public event EventHandler DoubleClicked;
		public bool IsAttached { get { return _attached; } }
		public FlyoutDoubleClickHelper (FrameworkElement target, DependencyObject filterRoot = null, Func<DependencyObject, bool> customIgnore = null)
		{
			if (target == null) throw new ArgumentNullException ("target");
			_target = target;
			_filterRoot = filterRoot ?? target;
			_customIgnore = customIgnore;
		}
		public void Attach ()
		{
			if (_disposed) return;
			if (_attached) return;
			_attached = true;
			_target.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
			_target.PreviewStylusDown += OnPreviewStylusDown;
			_target.PreviewTouchDown += OnPreviewTouchDown;
		}
		public void Detach ()
		{
			if (!_attached) return;
			_attached = false;
			_target.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
			_target.PreviewStylusDown -= OnPreviewStylusDown;
			_target.PreviewTouchDown -= OnPreviewTouchDown;
			ResetPending ();
		}
		public void Dispose ()
		{
			if (_disposed) return;
			_disposed = true;
			Detach ();
			DoubleClicked = null;
		}
		private void ResetPending ()
		{
			_lastStylusTime = DateTime.MinValue;
			_lastTouchTime = DateTime.MinValue;
		}
		private void RaiseDoubleClicked ()
		{
			EventHandler handler = DoubleClicked;
			if (handler != null) handler (this, EventArgs.Empty);
		}
		private bool IsIgnored (DependencyObject source)
		{
			if (source == null) return false;
			if (_customIgnore != null && _customIgnore (source)) return true;
			DependencyObject cur = source;
			while (cur != null && !ReferenceEquals (cur, _filterRoot))
			{
				if (cur is Thumb) return true;
				if (cur is ScrollBar) return true;
				if (cur is ButtonBase) return true;
				if (cur is TextBoxBase) return true;
				if (cur is Selector) return true;
				if (cur is Slider) return true;
				if (cur is PasswordBox) return true;
				cur = GetParentSafe (cur);
			}
			return false;
		}
		private void OnPreviewMouseLeftButtonDown (object sender, MouseButtonEventArgs e)
		{
			if (e.StylusDevice != null) return;
			if (e.ChangedButton != MouseButton.Left) return;
			if (e.ClickCount != 2) return;
			if (IsIgnored (e.OriginalSource as DependencyObject)) return;
			RaiseDoubleClicked ();
			e.Handled = true;
		}
		private void OnPreviewStylusDown (object sender, StylusDownEventArgs e)
		{
			StylusDevice dev = e.StylusDevice;
			if (dev == null || dev.TabletDevice == null) return;
			if (dev.TabletDevice.Type == TabletDeviceType.Touch) return;
			if (dev.InAir) return;
			if (IsIgnored (e.OriginalSource as DependencyObject)) return;
			DateTime now = DateTime.UtcNow;
			var pos = e.GetPosition (_target);
			bool isDouble =
				(now - _lastStylusTime).TotalMilliseconds < DoubleTapIntervalMs &&
				Math.Abs (pos.X - _lastStylusPos.X) < DoubleTapDistancePx &&
				Math.Abs (pos.Y - _lastStylusPos.Y) < DoubleTapDistancePx;
			if (isDouble)
			{
				ResetPending ();
				RaiseDoubleClicked ();
				e.Handled = true;
			}
			else
			{
				_lastStylusTime = now;
				_lastStylusPos = pos;
			}
		}
		private void OnPreviewTouchDown (object sender, TouchEventArgs e)
		{
			TouchDevice dev = e.TouchDevice;
			if (dev == null) return;
			if (IsIgnored (e.OriginalSource as DependencyObject)) return;
			DateTime now = DateTime.UtcNow;
			var pos = e.GetTouchPoint (_target).Position;
			bool isDouble =
				(now - _lastTouchTime).TotalMilliseconds < DoubleTapIntervalMs &&
				Math.Abs (pos.X - _lastTouchPos.X) < DoubleTapDistancePx &&
				Math.Abs (pos.Y - _lastTouchPos.Y) < DoubleTapDistancePx;
			if (isDouble)
			{
				ResetPending ();
				RaiseDoubleClicked ();
				e.Handled = true;
			}
			else
			{
				_lastTouchTime = now;
				_lastTouchPos = pos;
			}
		}
		private static DependencyObject GetParentSafe (DependencyObject cur)
		{
			if (cur == null) return null;
			if (cur is Visual) return VisualTreeHelper.GetParent (cur);
			if (cur is System.Windows.Media.Media3D.Visual3D) return VisualTreeHelper.GetParent (cur);
			return LogicalTreeHelper.GetParent (cur);
		}
	}
}
