using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Sidebar
{
	public static class ScreenHelper
	{
		// 结构体：把 CharSet 由 Ansi 改为 Unicode
		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private class DISPLAY_DEVICE
		{
			public int cb = 0;
			[MarshalAs (UnmanagedType.ByValTStr, SizeConst = 32)]
			public string DeviceName = string.Empty;
			[MarshalAs (UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceString = string.Empty;
			public int StateFlags = 0;
			[MarshalAs (UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceID = string.Empty;
			[MarshalAs (UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceKey = string.Empty;
		}
		[DllImport ("user32.dll", CharSet = CharSet.Auto)]
		private static extern bool EnumDisplayDevices (
			string lpDevice,
			int iDevNum,
			[In, Out] DISPLAY_DEVICE lpDisplayDevice,
			int dwFlags);
		private const uint MONITOR_DEFAULTTOPRIMARY = 0x00000001;
		[DllImport ("user32.dll")]
		private static extern IntPtr MonitorFromWindow (IntPtr hwnd, uint dwFlags);
		[DllImport ("user32.dll")]
		private static extern bool GetMonitorInfo (IntPtr hMonitor, ref MONITORINFO lpmi);
		[StructLayout (LayoutKind.Sequential)]
		private struct MONITORINFO
		{
			public int cbSize;
			public RECT rcMonitor;
			public RECT rcWork;
			public uint dwFlags;
		}
		[StructLayout (LayoutKind.Sequential)]
		private struct RECT
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}
		/// <summary>
		/// 获取指定窗口句柄所在的屏幕
		/// </summary>
		/// <param name="hwnd">窗口句柄</param>
		/// <returns>所在的 Screen 对象，失败时返回主屏幕</returns>
		public static Screen GetScreenByHWND (IntPtr hwnd)
		{
			if (hwnd == IntPtr.Zero)
				return Screen.PrimaryScreen;

			IntPtr hMonitor = MonitorFromWindow (hwnd, MONITOR_DEFAULTTOPRIMARY);
			if (hMonitor == IntPtr.Zero)
				return Screen.PrimaryScreen;

			MONITORINFO info = new MONITORINFO ();
			info.cbSize = Marshal.SizeOf (typeof (MONITORINFO));
			if (!GetMonitorInfo (hMonitor, ref info))
				return Screen.PrimaryScreen;

			Rectangle rect = new Rectangle (
				info.rcMonitor.Left,
				info.rcMonitor.Top,
				info.rcMonitor.Right - info.rcMonitor.Left,
				info.rcMonitor.Bottom - info.rcMonitor.Top);

			// 匹配 Screen.AllScreens
			foreach (Screen screen in Screen.AllScreens)
			{
				if (screen.Bounds.Equals (rect))
					return screen;
			}

			return Screen.PrimaryScreen;
		}
		public static string GetScreenFriendlyName (Screen screen)
		{
			if (screen == null) return string.Empty;

			DISPLAY_DEVICE info = new DISPLAY_DEVICE ();
			info.cb = Marshal.SizeOf (info);
			string deviceName = screen.DeviceName;

			if (EnumDisplayDevices (deviceName, 0, info, 0))
			{
				string name = info.DeviceString;
				if (!string.IsNullOrEmpty (name))
				{
					// 个别驱动可能不写 NUL 终止符，兜底裁剪
					int nul = name.IndexOf ('\0');
					if (nul >= 0) name = name.Substring (0, nul);
					name = name.Trim ();
					if (name.Length > 0) return name;
				}
			}
			return deviceName;
		}
		public static Screen GetScreenById (string screenId)
		{
			if (string.IsNullOrEmpty (screenId) || screenId == "Primary") return Screen.PrimaryScreen;
			var match = Screen.AllScreens.FirstOrDefault (
				s => s.DeviceName.Equals (screenId, StringComparison.OrdinalIgnoreCase));
			if (match != null) return match;
			foreach (var s in Screen.AllScreens)
			{
				if (string.Equals (GetScreenFriendlyName (s), screenId, StringComparison.OrdinalIgnoreCase))
					return s;
			}
			return Screen.PrimaryScreen;
		}
		public static void GetScreenPixelSize (Screen screen, out int width, out int height)
		{
			width = screen.Bounds.Width;
			height = screen.Bounds.Height;
		}
		public static void GetSystemDpi (out float dpiX, out float dpiY)
		{
			using (var g = Graphics.FromHwnd (IntPtr.Zero))
			{
				dpiX = g.DpiX;
				dpiY = g.DpiY;
			}
		}
		public static void GetScreenDpiPerMonitor (Screen screen, out uint dpiX, out uint dpiY)
		{
			dpiX = 96;
			dpiY = 96;
			try
			{
				var hmonitor = MonitorFromWindow (IntPtr.Zero, MONITOR_DEFAULTTOPRIMARY);
				GetDpiForMonitor (hmonitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out dpiX, out dpiY);
			}
			catch
			{
				float fx, fy;
				GetSystemDpi (out fx, out fy);
				dpiX = (uint)fx;
				dpiY = (uint)fy;
			}
		}
		[DllImport ("shcore.dll")]
		private static extern int GetDpiForMonitor (IntPtr hmonitor, MONITOR_DPI_TYPE dpiType, out uint dpiX, out uint dpiY);
		private enum MONITOR_DPI_TYPE
		{
			MDT_EFFECTIVE_DPI = 0,
			MDT_ANGULAR_DPI = 1,
			MDT_RAW_DPI = 2,
			MDT_DEFAULT = MDT_EFFECTIVE_DPI
		}
		public static int GetScreenDPI (Screen screen)
		{
			IntPtr hdc = IntPtr.Zero;
			try
			{
				string deviceName = screen.DeviceName;
				hdc = CreateDC (deviceName, null, null, IntPtr.Zero);
				if (hdc == IntPtr.Zero)
				{
					hdc = GetDC (IntPtr.Zero);
				}
				if (hdc == IntPtr.Zero) return 0;
				int dpiX = GetDeviceCaps (hdc, LOGPIXELSX);
				if (dpiX <= 0) return 0;
				return (int)Math.Round ((dpiX / 96.0) * 100.0);
			}
			catch
			{
				return 0;
			}
			finally
			{
				if (hdc != IntPtr.Zero)
				{
					if (hdc == GetDC (IntPtr.Zero))
						ReleaseDC (IntPtr.Zero, hdc);
					else
						DeleteDC (hdc);
				}
			}
		}
		[DllImport ("user32.dll")]
		private static extern IntPtr GetDC (IntPtr hWnd);
		[DllImport ("user32.dll")]
		private static extern int ReleaseDC (IntPtr hWnd, IntPtr hDC);
		[DllImport ("gdi32.dll")]
		private static extern IntPtr CreateDC (string lpszDriver, string lpszDevice, string lpszOutput, IntPtr lpInitData);
		[DllImport ("gdi32.dll")]
		private static extern bool DeleteDC (IntPtr hdc);
		[DllImport ("gdi32.dll")]
		private static extern int GetDeviceCaps (IntPtr hdc, int nIndex);

		private const int LOGPIXELSX = 88;
	}
	public static class ScreenExtraMethods
	{
		public static Screen GetScreen (string id)
		{
			return ScreenHelper.GetScreenById (id);
		}
		public static Size GetSize (this Screen screen)
		{
			int w, h;
			ScreenHelper.GetScreenPixelSize (screen, out w, out h);
			return new Size (w, h);
		}
		public static int GetWidth (this Screen screen)
		{
			int w, h;
			ScreenHelper.GetScreenPixelSize (screen, out w, out h);
			return w;
		}
		public static int GetHeight (this Screen screen)
		{
			int w, h;
			ScreenHelper.GetScreenPixelSize (screen, out w, out h);
			return h;
		}
		public static int GetDPIPercent (this Screen screen) => ScreenHelper.GetScreenDPI (screen);
		public static double GetDPI (this Screen screen) => GetDPIPercent (screen) * 0.01;
		public static string GetFriendlyName (this Screen screen)
		{
			return ScreenHelper.GetScreenFriendlyName (screen);
		}
		public static Screen GetScreen (this HWND hWnd, Screen dflt = null)
		{
			return ScreenHelper.GetScreenByHWND (hWnd) ?? dflt;
		}
	}
}