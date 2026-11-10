using System;
using System.ComponentModel;
using System.Threading;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace WindowsModern.SnowFall
{
	/// <summary>
	/// SnowFall 主题的配色提供者。监听 Windows 系统深色模式（Shell 深色 / App 深色），
	/// 通过 INotifyPropertyChanged 通知主题层绑定刷新。
	/// 
	/// 只提供 Color 属性，XAML 中统一使用 SolidColorBrush 绑定，避免隐式转换失败和跨线程崩溃。
	/// </summary>
	public sealed class SnowfallThemeProvider: INotifyPropertyChanged, IDisposable
	{
		private const string PersonalizeKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
		private const string SystemUsesLightThemeValue = "SystemUsesLightTheme";
		private const string AppsUseLightThemeValue = "AppsUseLightTheme";

		private static readonly Lazy<SnowfallThemeProvider> _lazy =
			new Lazy<SnowfallThemeProvider> (() => new SnowfallThemeProvider (), true);

		public static SnowfallThemeProvider Instance => _lazy.Value;

		private readonly object _lock = new object ();
		private readonly Dispatcher _dispatcher;
		private bool _isShellDark;
		private bool _isAppDark;
		private int _disposed;

		private SnowfallThemeProvider ()
		{
			_dispatcher = System.Windows.Application.Current?.Dispatcher
						  ?? Dispatcher.CurrentDispatcher;
			Refresh ();
			SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
		}

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged (string propertyName)
		{
			var h = PropertyChanged;
			if (h != null) h (this, new PropertyChangedEventArgs (propertyName));
		}
		#endregion

		#region 状态
		public bool IsShellDark => _isShellDark;
		public bool IsAppDark => _isAppDark;
		#endregion

		#region 颜色：浅色 / 深色
		// ---- 文字 ----
		public Color ForegroundColor => _isShellDark
			? Color.FromRgb (0xE8, 0xF0, 0xFF)   // 浅冰蓝白
			: Color.FromRgb (0x2A, 0x42, 0x68);  // 深蓝黑

		public Color ForegroundSoftColor => _isShellDark
			? Color.FromRgb (0xC8, 0xD8, 0xE8)
			: Color.FromRgb (0x3A, 0x5A, 0x82);

		// ---- 边框 / 边缘 ----
		public Color BorderColor => _isShellDark
			? Color.FromArgb (0xE8, 0xE8, 0xF0, 0xFF)
			: Color.FromArgb (0xE8, 0x3A, 0x5A, 0x82);

		public Color EdgeColor => _isShellDark
			? Color.FromArgb (0xB0, 0x1A, 0x2A, 0x3E)   // 深蓝边缘，避免白边太刺眼
			: Color.FromArgb (0xB0, 0x3A, 0x5A, 0x82);

		// ---- 分隔条 ----
		public Color SplitTopColor => _isShellDark
			? Color.FromArgb (0x60, 0xE8, 0xF0, 0xFF)
			: Color.FromArgb (0x60, 0x3A, 0x5A, 0x82);

		public Color SplitBottomColor => _isShellDark
			? Color.FromArgb (0x30, 0xE8, 0xF0, 0xFF)
			: Color.FromArgb (0x30, 0x3A, 0x5A, 0x82);

		public Color SplitTopHoverColor => _isShellDark
			? Color.FromArgb (0xA0, 0xE8, 0xF0, 0xFF)
			: Color.FromArgb (0xA0, 0x3A, 0x5A, 0x82);

		public Color SplitBottomHoverColor => _isShellDark
			? Color.FromArgb (0x80, 0xE8, 0xF0, 0xFF)
			: Color.FromArgb (0x80, 0x3A, 0x5A, 0x82);

		// ---- 箭头 (Restore Button) ----
		public Color ArrowColor => _isShellDark
			? Color.FromArgb (0xE0, 0xE8, 0xF0, 0xFF) // 提高了不透明度
			: Color.FromArgb (0xE0, 0x2A, 0x42, 0x68);

		public Color ArrowHoverColor => _isShellDark
			? Colors.White
			: Color.FromRgb (0x1A, 0x28, 0x42);

		public Color ArrowPressedColor => _isShellDark
			? Color.FromArgb (0xA0, 0xE8, 0xF0, 0xFF)
			: Color.FromArgb (0xA0, 0x3A, 0x5A, 0x82);

		// ---- Hover ----
		public Color HoverColor => _isShellDark
			? Color.FromArgb (0x40, 0xE8, 0xF0, 0xFF)
			: Color.FromArgb (0x40, 0x3A, 0x5A, 0x82);

		public Color HoverStrongColor => _isShellDark
			? Color.FromArgb (0x60, 0xE8, 0xF0, 0xFF)
			: Color.FromArgb (0x60, 0x3A, 0x5A, 0x82);

		// ---- 背景（提亮后的雪夜蓝） ----
		public Color SidebarBgStartColor => _isShellDark
			? Color.FromArgb (0xF0, 0x2A, 0x42, 0x68)
			: Color.FromArgb (0xF0, 0xB8, 0xDC, 0xF0);

		public Color SidebarBgEndColor => _isShellDark
			? Color.FromArgb (0xF0, 0x16, 0x24, 0x3C)
			: Color.FromArgb (0xF0, 0x70, 0xB4, 0xDC);

		public Color TaskbarBgStartColor => _isShellDark
			? Color.FromArgb (0xC8, 0x2A, 0x42, 0x68)
			: Color.FromArgb (0xC8, 0xB8, 0xDC, 0xF0);

		public Color TaskbarBgEndColor => _isShellDark
			? Color.FromArgb (0xC8, 0x16, 0x24, 0x3C)
			: Color.FromArgb (0xC8, 0x70, 0xB4, 0xDC);

		public Color FlyoutBgStartColor => _isShellDark
			? Color.FromArgb (0xF0, 0x2A, 0x42, 0x68)
			: Color.FromArgb (0xF0, 0xB8, 0xDC, 0xF0);

		public Color FlyoutBgEndColor => _isShellDark
			? Color.FromArgb (0xF0, 0x16, 0x24, 0x3C)
			: Color.FromArgb (0xF0, 0xA0, 0xC8, 0xE4);

		public Color DragBgColor => _isShellDark
			? Color.FromArgb (0xF8, 0x2A, 0x42, 0x68)
			: Color.FromArgb (0xF8, 0xA8, 0xD0, 0xE8);

		public Color NotifBgColor => _isShellDark
			? Color.FromArgb (0xFA, 0x2A, 0x42, 0x68)
			: Color.FromArgb (0xFA, 0xA8, 0xD0, 0xE8);

		public Color NotifTextBgColor => _isShellDark
			? Color.FromArgb (0xFF, 0x16, 0x24, 0x3C)
			: Color.FromArgb (0xF8, 0xFF, 0xFF, 0xFF);

		// ---- 时钟投影 ----
		public Color ClockShadowColor => _isShellDark
			? Colors.Black
			: Color.FromRgb (0x2A, 0x42, 0x68);
		#endregion

		#region 监听系统事件
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

		private void Refresh ()
		{
			if (Thread.VolatileRead (ref _disposed) != 0) return;
			bool shellDark = ReadIsDark (SystemUsesLightThemeValue);
			bool appDark = ReadIsDark (AppsUseLightThemeValue);
			bool shellChanged, appChanged;
			lock (_lock)
			{
				shellChanged = _isShellDark != shellDark;
				appChanged = _isAppDark != appDark;
				_isShellDark = shellDark;
				_isAppDark = appDark;
			}

			if (!shellChanged && !appChanged) return;

			if (shellChanged)
			{
				OnPropertyChanged (nameof (IsShellDark));
				OnPropertyChanged (nameof (ForegroundColor));
				OnPropertyChanged (nameof (ForegroundSoftColor));
				OnPropertyChanged (nameof (BorderColor));
				OnPropertyChanged (nameof (EdgeColor));
				OnPropertyChanged (nameof (SplitTopColor));
				OnPropertyChanged (nameof (SplitBottomColor));
				OnPropertyChanged (nameof (SplitTopHoverColor));
				OnPropertyChanged (nameof (SplitBottomHoverColor));
				OnPropertyChanged (nameof (ArrowColor));
				OnPropertyChanged (nameof (ArrowHoverColor));
				OnPropertyChanged (nameof (ArrowPressedColor));
				OnPropertyChanged (nameof (HoverColor));
				OnPropertyChanged (nameof (HoverStrongColor));
				OnPropertyChanged (nameof (SidebarBgStartColor));
				OnPropertyChanged (nameof (SidebarBgEndColor));
				OnPropertyChanged (nameof (TaskbarBgStartColor));
				OnPropertyChanged (nameof (TaskbarBgEndColor));
				OnPropertyChanged (nameof (FlyoutBgStartColor));
				OnPropertyChanged (nameof (FlyoutBgEndColor));
				OnPropertyChanged (nameof (DragBgColor));
				OnPropertyChanged (nameof (NotifBgColor));
				OnPropertyChanged (nameof (NotifTextBgColor));
				OnPropertyChanged (nameof (ClockShadowColor));
			}
			if (appChanged)
			{
				OnPropertyChanged (nameof (IsAppDark));
			}
		}

		private static bool ReadIsDark (string valueName)
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
		#endregion

		#region Dispose
		public void Dispose ()
		{
			if (Interlocked.Exchange (ref _disposed, 1) != 0) return;
			SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
			lock (_lock) { }
		}
		#endregion
	}
}