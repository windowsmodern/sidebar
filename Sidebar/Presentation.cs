using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Data;
using System.Windows.Markup;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.Windows.Threading;
using Sidebar.Win32;
using System.Collections;
using System.Collections.Specialized;

namespace Sidebar.Presentation
{
	internal static class DllMain
	{
		internal static ProgramFolder AppRoot => App.ProgramFolder;
		internal static SidebarConfig Config => App.CurrentUserConfig;
		internal static ILocaleResources StringResources => AppRoot?.StringResources;
		internal static string GetString (string resname, string fallback = null, string localename = null)
		{
			try { return StringResources?.SuitableResource (resname, fallback, localename) ?? fallback; }
			catch { return fallback; }
		}
	}
	public sealed class SidebarUIConfig: INotifyPropertyChanged, IDisposable
	{
		private static readonly Lazy<SidebarUIConfig> _lazy = new Lazy<SidebarUIConfig> (() => new SidebarUIConfig (), LazyThreadSafetyMode.ExecutionAndPublication);
		public static SidebarUIConfig Instance => _lazy.Value;
		public bool IsReversed => (DllMain.Config?.Direction ?? SidebarDirection.Right) == SidebarDirection.Left;
		private readonly object _handleLock = new object ();
		private readonly HashSet<PropertyChangedEventHandler> _handlers = new HashSet<PropertyChangedEventHandler> ();
		private int _disposed;
		public SidebarUIConfig ()
		{
			if (DllMain.Config != null) DllMain.Config.PropertyChanged += Config_PropertyChanged;
		}
		public event PropertyChangedEventHandler PropertyChanged
		{
			add { if (value == null) return; lock (_handleLock) _handlers.Add (value); }
			remove { if (value == null) return; lock (_handleLock) _handlers.Remove (value); }
		}
		private void OnPropertyChanged (string propname)
		{
			PropertyChangedEventHandler [] snapshot;
			lock (_handleLock)
			{
				if (_handlers.Count == 0) return;
				snapshot = new PropertyChangedEventHandler [_handlers.Count];
				_handlers.CopyTo (snapshot);
			}
			var dispatcher = System.Windows.Application.Current?.Dispatcher;
			if (dispatcher != null && !dispatcher.CheckAccess ())
			{
				dispatcher.BeginInvoke (new Action (() => Fire (snapshot, propname)));
				return;
			}
			Fire (snapshot, propname);
		}
		private void Fire (PropertyChangedEventHandler [] snapshot, string propname)
		{
			var e = new PropertyChangedEventArgs (propname);
			foreach (var h in snapshot)
			{
				try { h?.Invoke (this, e); }
				catch { }
			}
		}
		private void Config_PropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof (DllMain.Config.Direction):
					OnPropertyChanged (nameof (IsReversed));
					break;
			}
		}
		public void Dispose ()
		{
			if (Interlocked.Exchange (ref _disposed, 1) != 0) return;
			DllMain.Config.PropertyChanged -= Config_PropertyChanged;
			lock (_handleLock) _handlers.Clear ();
		}
	}
	public sealed class SystemExplorerShell: INotifyPropertyChanged, IDisposable
	{
		private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
		private const string AppsUseLightThemeValue = "AppsUseLightTheme";
		private const string SystemUsesLightThemeValue = "SystemUsesLightTheme";
		private static readonly Lazy<SystemExplorerShell> _lazy = new Lazy<SystemExplorerShell> (() => new SystemExplorerShell (), LazyThreadSafetyMode.ExecutionAndPublication);
		public static SystemExplorerShell Instance => _lazy.Value;
		private readonly object _handlersLock = new object ();
		private readonly HashSet<PropertyChangedEventHandler> _handlers = new HashSet<PropertyChangedEventHandler> ();
		private readonly Dispatcher _dispatcher;
		private int _disposed;
		private bool _isTaskbarTop;
		private bool _isAppDarkMode;
		private bool _isShellDarkMode;
		public bool IsTaskbarTop => _isTaskbarTop;
		public bool IsAppDarkMode => _isAppDarkMode;
		public bool IsShellDarkMode => _isShellDarkMode;
		public event PropertyChangedEventHandler PropertyChanged
		{
			add { if (value == null) return; lock (_handlersLock) _handlers.Add (value); }
			remove { if (value == null) return; lock (_handlersLock) _handlers.Remove (value); }
		}
		private SystemExplorerShell ()
		{
			_dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
			Refresh ();
			SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
		}
		private void OnUserPreferenceChanged (object sender, UserPreferenceChangedEventArgs e)
		{
			var d = _dispatcher;
			if (d == null || d.HasShutdownStarted || d.HasShutdownFinished) return;
			if (d.CheckAccess ()) Refresh ();
			else
			{
				try { d.BeginInvoke (DispatcherPriority.Background, new Action (Refresh)); }
				catch { }
			}
		}
		public void Refresh ()
		{
			if (Interlocked.CompareExchange (ref _disposed, 0, 0) != 0) return;
			RefreshTaskbarEdge ();
			RefreshDarkMode ();
		}
		private void RefreshTaskbarEdge ()
		{
			bool value = false;
			try { value = ExplorerShell.GetTaskbarEdge () == AppBarEdge.Top; }
			catch { value = false; }
			if (_isTaskbarTop == value) return;
			_isTaskbarTop = value;
			OnPropertyChanged (nameof (IsTaskbarTop));
		}
		private void RefreshDarkMode ()
		{
			bool appDark = ReadIsDarkMode (AppsUseLightThemeValue);
			if (_isAppDarkMode != appDark)
			{
				_isAppDarkMode = appDark;
				OnPropertyChanged (nameof (IsAppDarkMode));
			}
			bool shellDark = ReadIsDarkMode (SystemUsesLightThemeValue);
			if (_isShellDarkMode != shellDark)
			{
				_isShellDarkMode = shellDark;
				OnPropertyChanged (nameof (IsShellDarkMode));
			}
		}
		private static bool ReadIsDarkMode (string valueName)
		{
			try
			{
				using (var key = Registry.CurrentUser.OpenSubKey (PersonalizeKeyPath, false))
				{
					if (key == null) return false;
					object raw = key.GetValue (valueName);
					if (raw == null) return false;
					int value;
					try { value = Convert.ToInt32 (raw); }
					catch { return false; }
					return value == 0;
				}
			}
			catch { return false; }
		}
		private void OnPropertyChanged (string propertyName)
		{
			PropertyChangedEventHandler [] snapshot;
			lock (_handlersLock)
			{
				if (_handlers.Count == 0) return;
				snapshot = new PropertyChangedEventHandler [_handlers.Count];
				_handlers.CopyTo (snapshot);
			}
			var args = new PropertyChangedEventArgs (propertyName);
			foreach (var h in snapshot)
			{
				try { h?.Invoke (this, args); }
				catch { }
			}
		}
		public void Dispose ()
		{
			if (Interlocked.Exchange (ref _disposed, 1) != 0) return;
			SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
			lock (_handlersLock) _handlers.Clear ();
		}
	}
	/// <summary>
	/// 提供 DWM 系统主题色（Windows 窗口颜色 / 毛玻璃颜色）。
	/// 支持监听颜色变化、PropertyChanged 通知、多线程安全、可释放。
	/// </summary>
	public sealed class DwmThemeProvider: INotifyPropertyChanged, IDisposable
	{
		#region 单例
		private static readonly Lazy<DwmThemeProvider> _lazy = new Lazy<DwmThemeProvider> (() => new DwmThemeProvider (), LazyThreadSafetyMode.ExecutionAndPublication);
		public static DwmThemeProvider Instance => _lazy.Value;
		#endregion
		#region PropertyChanged（HashSet + 锁）
		private readonly object _handlersLock = new object ();
		private readonly HashSet<PropertyChangedEventHandler> handlers = new HashSet<PropertyChangedEventHandler> ();
		public event PropertyChangedEventHandler PropertyChanged
		{
			add
			{
				if (value == null) return;
				lock (_handlersLock) handlers.Add (value);
			}
			remove
			{
				if (value == null) return;
				lock (_handlersLock) handlers.Remove (value);
			}
		}
		protected void OnPropertyChanged (string propertyName)
		{
			PropertyChangedEventHandler [] snapshot;
			lock (_handlersLock)
			{
				if (handlers.Count == 0) return;
				snapshot = new PropertyChangedEventHandler [handlers.Count];
				handlers.CopyTo (snapshot);
			}
			var args = new PropertyChangedEventArgs (propertyName);
			foreach (var handler in snapshot)
			{
				try { handler (this, args); }
				catch { }
			}
		}
		#endregion
		#region 颜色
		private Color _themeColor;
		public Color ThemeColor
		{
			get { return _themeColor; }
			private set
			{
				if (_themeColor == value) return;
				var oldColor = _themeColor;
				_themeColor = value;
				OnPropertyChanged (nameof (ThemeColor));
				OnPropertyChanged (nameof (ThemeBrush));
				OnPropertyChanged (nameof (ForegroundBrush));
				OnPropertyChanged (nameof (ThemeColorOpaque));
				OnPropertyChanged (nameof (OpaqueThemeBrush));
				OnPropertyChanged (nameof (ForegroundColor));
				ColorChanged?.Invoke (this, new ColorChangedEventArgs (oldColor, value));
			}
		}

		public Color ThemeColorOpaque
			=> Color.FromArgb (255, ThemeColor.R, ThemeColor.G, ThemeColor.B);
		private SolidColorBrush _themeBrush;
		public SolidColorBrush ThemeBrush
		{
			get
			{
				if (_themeBrush == null)
					_themeBrush = new SolidColorBrush (ThemeColor);
				else
					_themeBrush.Color = ThemeColor;
				return _themeBrush;
			}
		}
		private SolidColorBrush _opaqueThemeBrush;
		public SolidColorBrush OpaqueThemeBrush
		{
			get
			{
				var opaque = ThemeColorOpaque;
				if (_opaqueThemeBrush == null)
					_opaqueThemeBrush = new SolidColorBrush (opaque);
				else
					_opaqueThemeBrush.Color = opaque;
				return _opaqueThemeBrush;
			}
		}
		private SolidColorBrush _foregroundBrush;
		public SolidColorBrush ForegroundBrush
		{
			get
			{
				var fg = GetForegroundColor (ThemeColor);
				if (_foregroundBrush == null)
					_foregroundBrush = new SolidColorBrush (fg);
				else
					_foregroundBrush.Color = fg;
				return _foregroundBrush;
			}
		}
		public Color ForegroundColor => GetForegroundColor (ThemeColor);
		private static Color GetForegroundColor (Color bg)
		{
			double luminance = 0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B;
			return luminance > 140 ? Colors.Black : Colors.White;
		}
		public event EventHandler<ColorChangedEventArgs> ColorChanged;
		#endregion
		#region 构造 / 更新颜色
		private DwmThemeProvider ()
		{
			UpdateThemeColor ();
		}
		private void UpdateThemeColor ()
		{
			try
			{
				uint color;
				bool opaque;
				int hr = DwmGetColorizationColor (out color, out opaque);
				if (hr == 0)
				{
					byte a = (byte)((color >> 24) & 0xFF);
					byte r = (byte)((color >> 16) & 0xFF);
					byte g = (byte)((color >> 8) & 0xFF);
					byte b = (byte)(color & 0xFF);
					ThemeColor = Color.FromArgb (a, r, g, b);
					return;
				}
			}
			catch { }
			ThemeColor = Color.FromRgb (0x38, 0x41, 0x46);
		}
		[DllImport ("dwmapi.dll")]
		private static extern int DwmGetColorizationColor (out uint pcrColorization, out bool pfOpaqueBlend);
		#endregion
		#region 窗口监听
		private readonly object _sourceLock = new object ();
		private HwndSource _hwndSource;
		private const int WM_DWMCOMPOSITIONCHANGED = 0x0000031E;
		private const int WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320;
		public void StartListening (HwndSource window)
		{
			if (window == null) return;
			lock (_sourceLock)
			{
				if (_disposed) return;
				if (ReferenceEquals (_hwndSource, window)) return;
				StopListeningLocked ();
				_hwndSource = window;
				_hwndSource.AddHook (WndProc);
			}
		}
		public void StopListening ()
		{
			lock (_sourceLock) StopListeningLocked ();
		}
		private void StopListeningLocked ()
		{
			if (_hwndSource == null) return;
			try { _hwndSource.RemoveHook (WndProc); }
			catch { }
			_hwndSource = null;
		}
		private IntPtr WndProc (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
		{
			if (msg == WM_DWMCOLORIZATIONCOLORCHANGED)
				UpdateThemeColor ();
			return IntPtr.Zero;
		}
		#endregion
		#region Dispose
		private volatile bool _disposed;
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		private void Dispose (bool disposing)
		{
			if (Interlocked.Exchange (ref _disposeFlag, 1) != 0) return;
			_disposed = true;

			if (disposing)
			{
				StopListening ();
				lock (_handlersLock) handlers.Clear ();
				ColorChanged = null;
			}
		}
		private int _disposeFlag;
		~DwmThemeProvider ()
		{
			Dispose (false);
		}
		#endregion
	}
	public class ColorChangedEventArgs: EventArgs
	{
		public Color OldColor { get; private set; }
		public Color NewColor { get; private set; }
		public ColorChangedEventArgs (Color oldColor, Color newColor)
		{
			OldColor = oldColor;
			NewColor = newColor;
		}
	}
	/// <summary>
	/// XAML 标记扩展：把资源 key 直接解析成当前语言的字符串。
	/// 用法：
	///   Text="{local:StringResource SIDEBAR_TITLE}"
	///   Text="{local:StringResource Key=ABOUT_TITLE, Fallback='关于'}"
	/// </summary>
	[MarkupExtensionReturnType (typeof (string))]
	public sealed class StringResourceExtension: MarkupExtension
	{
		[ConstructorArgument ("key")]
		public string Key { get; set; }
		public string Fallback { get; set; }
		public string Locale { get; set; }
		public StringResourceExtension () { }
		public StringResourceExtension (string key) { Key = key; }
		public override object ProvideValue (IServiceProvider serviceProvider)
		{
			if (string.IsNullOrEmpty (Key)) return string.Empty;
			var fallback = Fallback ?? Key;
			return DllMain.GetString (Key, fallback, Locale) ?? fallback;
		}
	}
	/// <summary>
	/// 把字符串 key 解析为本地化资源。
	/// 用途：绑定源本身是 key 的场景（静态 key 请优先用 {local:StringResource} 标记扩展）。
	///   <TextBlock Text="{Binding Config.ThemeName, Converter={x:Static local:StringResourceConverter.Instance}}"/>
	/// </summary>
	public sealed class StringResourceConverter: IValueConverter
	{
		public static StringResourceConverter Instance { get; private set; } = new StringResourceConverter ();
		public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			var key = value as string;
			if (string.IsNullOrEmpty (key)) return string.Empty;
			var fallback = parameter as string ?? key;
			try
			{
				return DllMain.GetString (key, fallback) ?? fallback;
			}
			catch
			{
				return fallback;
			}
		}
		public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
	}
	/// <summary>
	/// 根据对象是否为 null 决定 Visibility。
	/// 默认：非 null → Visible，null → Collapsed。
	/// ConverterParameter="Invert" 可反转；"Hidden" 可将 Collapsed 换成 Hidden。
	/// 兼容旧参数 "CollapsedWhenNull"（等价于默认行为）。
	/// </summary>
	public sealed class ObjectToVisibilityConverter: IValueConverter
	{
		public static ObjectToVisibilityConverter Instance { get; private set; } = new ObjectToVisibilityConverter ();

		public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool hasValue = value != null;
			var param = (parameter as string ?? string.Empty).Trim ();
			switch (param.ToLowerInvariant ())
			{
				case "":
				case "collapsedwhennull":
					return hasValue ? Visibility.Visible : Visibility.Collapsed;
				case "invert":
				case "visiblewhennull":
					return hasValue ? Visibility.Collapsed : Visibility.Visible;
				case "hidden":
					return hasValue ? Visibility.Visible : Visibility.Hidden;
				default:
					return hasValue ? Visibility.Visible : Visibility.Collapsed;
			}
		}
		public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
	}
	/// <summary>
	/// CalendarMode.Month → true，其他模式 → false。
	/// null / UnsetValue → true（防止设计期或未初始化时报错）。
	/// 用于 Calendar 控件模板里按显示模式切换动画。
	/// </summary>
	public sealed class CalendarModeToBooleanConverter: IValueConverter
	{
		public static CalendarModeToBooleanConverter Instance { get; private set; } = new CalendarModeToBooleanConverter ();
		public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null || ReferenceEquals (value, DependencyProperty.UnsetValue))
				return true;
			if (value is CalendarMode)
				return (CalendarMode)value == CalendarMode.Month;
			return true;
		}
		public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException ();
		}
	}
	public static class ItemsControlAutoCollapseHelper
	{
		public static readonly DependencyProperty AutoCollapseWhenEmptyProperty = DependencyProperty.RegisterAttached ( "AutoCollapseWhenEmpty", typeof (bool), typeof (ItemsControlAutoCollapseHelper), new PropertyMetadata (false, OnAutoCollapseWhenEmptyChanged));
		public static bool GetAutoCollapseWhenEmpty (DependencyObject obj)
		{
			return (bool)obj.GetValue (AutoCollapseWhenEmptyProperty);
		}
		public static void SetAutoCollapseWhenEmpty (DependencyObject obj, bool value)
		{
			obj.SetValue (AutoCollapseWhenEmptyProperty, value);
		}
		private static void OnAutoCollapseWhenEmptyChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var itemsControl = d as ItemsControl;
			if (itemsControl == null) return;
			bool enable = (bool)e.NewValue;
			if (enable)
			{
				DependencyPropertyDescriptor.FromProperty (ItemsControl.ItemsSourceProperty, typeof (ItemsControl))
					.AddValueChanged (itemsControl, ItemsSource_Changed);
				AttachCollectionListener (itemsControl);
			}
			else
			{
				DependencyPropertyDescriptor.FromProperty (ItemsControl.ItemsSourceProperty, typeof (ItemsControl))
					.RemoveValueChanged (itemsControl, ItemsSource_Changed);
				DetachCollectionListener (itemsControl);
				itemsControl.Visibility = Visibility.Visible;
			}
		}
		private static void ItemsSource_Changed (object sender, EventArgs e)
		{
			var itemsControl = sender as ItemsControl;
			AttachCollectionListener (itemsControl);
		}
		private static readonly DependencyProperty TrackedCollectionProperty = DependencyProperty.RegisterAttached ( "TrackedCollection", typeof (INotifyCollectionChanged), typeof (ItemsControlAutoCollapseHelper));
		private static void AttachCollectionListener (ItemsControl itemsControl)
		{
			DetachCollectionListener (itemsControl);

			var itemsSource = itemsControl.ItemsSource as INotifyCollectionChanged;
			if (itemsSource != null)
			{
				itemsSource.CollectionChanged += (s, args) => UpdateVisibility (itemsControl);
				itemsControl.SetValue (TrackedCollectionProperty, itemsSource);
			}

			UpdateVisibility (itemsControl);
		}
		private static void DetachCollectionListener (ItemsControl itemsControl)
		{
			var oldCollection = itemsControl.GetValue (TrackedCollectionProperty) as INotifyCollectionChanged;
			if (oldCollection != null)
			{
				oldCollection.CollectionChanged -= (s, args) => UpdateVisibility (itemsControl);
				itemsControl.ClearValue (TrackedCollectionProperty);
			}
		}
		private static void UpdateVisibility (ItemsControl ctrl)
		{
			var collection = ctrl.ItemsSource as IEnumerable;
			if (collection == null)
			{
				ctrl.Visibility = Visibility.Collapsed;
				return;
			}
			var enumerator = collection.GetEnumerator ();
			bool hasItem = enumerator.MoveNext ();
			ctrl.Visibility = hasItem ? Visibility.Visible : Visibility.Collapsed;
		}
	}
	public sealed class NonZeroConverter: IValueConverter
	{
		public static NonZeroConverter Instance { get; private set; } = new NonZeroConverter ();
		public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null) return false;
			if (value is double) return Math.Abs ((double)value) > 0.01;
			if (value is float) return Math.Abs ((float)value) > 0.01f;
			if (value is decimal) return Math.Abs ((decimal)value) > 0.01m;
			if (value is int) return (int)value != 0;
			if (value is long) return (long)value != 0;
			if (value is short) return (short)value != 0;
			if (value is sbyte) return (sbyte)value != 0;
			if (value is uint) return (uint)value != 0;
			if (value is ulong) return (ulong)value != 0;
			if (value is ushort) return (ushort)value != 0;
			if (value is byte) return (byte)value != 0;
			return false;
		}
		public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
	}
}
