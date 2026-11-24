using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Sidebar
{
	public static class DWMAPI
	{
		private const int DWM_BB_ENABLE = 1;
		private const int DWM_BB_BLURREGION = 2;
		private const int DWM_BB_TRANSITIONONMAXIMIZED = 4;
		private const int DWMWA_EXCLUDED_FROM_PEEK = 12;
		private const int DWMWA_FLIP3D_POLICY = 8;
		private enum Flip3DPolicyValue
		{
			Default = 0,
			ExcludeBelow = 1,
			ExcludeAbove = 2
		}
		[DllImport ("dwmapi.dll")]
		private static extern int DwmEnableBlurBehindWindow (IntPtr hWnd, ref BlurBehindStruct blurBehind);
		[DllImport ("dwmapi.dll")]
		private static extern int DwmIsCompositionEnabled (ref bool enabled);
		[DllImport ("dwmapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int DwmGetColorizationColor (out int color, out bool opaque);
		[DllImport ("dwmapi.dll", PreserveSig = false)]
		private static extern int DwmSetWindowAttribute (IntPtr hwnd, int attribute, ref int attributeValue, int attributeSize);
		[StructLayout (LayoutKind.Sequential)]
		private struct BlurBehindStruct
		{
			public int flags;
			public bool enable;
			public IntPtr region;
			public bool transitionOnMaximized;
		}
		#region Aero Peek
		// 内部类：隔离不同版本的 P/Invoke 声明，避免重名二义性
		// V4：Vista / 7 / 8 / 8.1 使用的四参数版本
		internal static class AeroPeekNativeV4
		{
			[DllImport ("dwmapi.dll", EntryPoint = "#113", SetLastError = true)]
			internal static extern uint DwmpActivateLivePreview (uint activate, IntPtr hWnd, IntPtr topmostHwnd, uint peekType);
		}
		// V5：Windows 10+ 使用的五参数版本（末参数语义未知，传 UIntPtr.Zero）
		internal static class AeroPeekNativeV5
		{
			[DllImport ("dwmapi.dll", EntryPoint = "#113", SetLastError = true)]
			internal static extern uint DwmpActivateLivePreview (uint activate, IntPtr hWnd, IntPtr topmostHwnd, uint peekType, UIntPtr extra);
		}
		// 探测 dwmapi.dll #113 导出是否存在
		internal static class AeroPeekProbe
		{
			[DllImport ("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			private static extern IntPtr GetModuleHandle (string lpModuleName);
			[DllImport ("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
			private static extern IntPtr GetProcAddress (IntPtr hModule, string lpProcName);
			internal static bool IsExportAvailable ()
			{
				try
				{
					IntPtr hMod = GetModuleHandle ("dwmapi.dll");
					if (hMod == IntPtr.Zero) return false;
					return GetProcAddress (hMod, "#113") != IntPtr.Zero;
				}
				catch { return false; }
			}
		}
		// DWMAPI 对外只暴露这一对路由方法
		private static bool? _aeroPeekSupported = null;
		public static bool IsAeroPeekAvailable ()
		{
			if (_aeroPeekSupported.HasValue) return _aeroPeekSupported.Value;
			_aeroPeekSupported = false;
			if (Environment.OSVersion.Version.Major < 6) return false;
			if (!IsDwmAvailable ()) return false;
			if (!AeroPeekProbe.IsExportAvailable ()) return false;
			_aeroPeekSupported = true;
			return true;
		}
		private static bool IsWindows10OrHigher => Environment.OSVersion.Version.Major >= 10;
		public static bool ActivateLivePreview (IntPtr hwnd, IntPtr topmostHwnd)
		{
			if (!IsAeroPeekAvailable ()) return false;
			if (hwnd == IntPtr.Zero) return false;
			try
			{
				if (IsWindows10OrHigher)
				{
					uint hr = AeroPeekNativeV5.DwmpActivateLivePreview (1, hwnd, topmostHwnd, 1, UIntPtr.Zero);
					return hr == 0;
				}
				else
				{
					uint hr = AeroPeekNativeV4.DwmpActivateLivePreview (1, hwnd, topmostHwnd, 1);
					return hr == 0;
				}
			}
			catch { _aeroPeekSupported = false; return false; }
		}
		public static bool DeactivateLivePreview (IntPtr hwnd, IntPtr topmostHwnd)
		{
			if (!IsAeroPeekAvailable ()) return false;
			if (hwnd == IntPtr.Zero) return false;
			try
			{
				if (IsWindows10OrHigher)
				{
					uint hr = AeroPeekNativeV5.DwmpActivateLivePreview (0, hwnd, topmostHwnd, 1, UIntPtr.Zero);
					return hr == 0;
				}
				else
				{
					uint hr = AeroPeekNativeV4.DwmpActivateLivePreview (0, hwnd, topmostHwnd, 1);
					return hr == 0;
				}
			}
			catch { return false; }
		}
		#endregion
		/// <summary>
		/// 检查当前系统是否支持 DWM 玻璃效果（Windows Vista 或更高版本，且 DWM 已启用）。
		/// </summary>
		public static bool IsDwmAvailable ()
		{
			if (Environment.OSVersion.Version.Major < 6 ||
				(Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 0 && Environment.OSVersion.Version.Build < 5600))
				return false;
			if (!File.Exists (Path.Combine (Environment.SystemDirectory, "dwmapi.dll")))
				return false;
			bool enabled = false;
			int hr = DwmIsCompositionEnabled (ref enabled);
			return hr == 0 && enabled;
		}
		/// <summary>
		/// 为指定窗口启用 Aero 玻璃模糊效果。
		/// </summary>
		/// <param name="handle">目标窗口句柄。</param>
		/// <param name="region">模糊区域的 HRGN 句柄。传入 IntPtr.Zero 表示整个窗口区域。</param>
		/// <returns>成功返回 true，否则返回 false。</returns>
		public static bool EnableBlur (ref IntPtr handle, IntPtr region)
		{
			if (!IsDwmAvailable ()) return false;
			var blurBehind = new BlurBehindStruct {
				enable = true,
				flags = DWM_BB_ENABLE,
				transitionOnMaximized = false
			};
			if (region != IntPtr.Zero)
			{
				blurBehind.flags |= DWM_BB_BLURREGION;
				blurBehind.region = region;
			}
			return DwmEnableBlurBehindWindow (handle, ref blurBehind) == 0;
		}
		/// <summary>
		/// 为指定窗口禁用 Aero 玻璃模糊效果。
		/// </summary>
		/// <param name="handle">目标窗口句柄。</param>
		/// <returns>成功返回 true，否则返回 false。</returns>
		public static bool DisableBlur (ref IntPtr handle)
		{
			if (!IsDwmAvailable ()) return false;
			var blurBehind = new BlurBehindStruct {
				enable = false,
				flags = DWM_BB_ENABLE,
				region = IntPtr.Zero,
				transitionOnMaximized = false
			};
			return DwmEnableBlurBehindWindow (handle, ref blurBehind) == 0;
		}
		/// <summary>
		/// 从 Aero Peek（任务栏预览）中排除指定窗口。
		/// </summary>
		/// <param name="hwnd">目标窗口句柄。</param>
		public static void RemoveFromAeroPeek (IntPtr hwnd)
		{
			if (!IsDwmAvailable ()) return;
			int value = 1; // TRUE
			DwmSetWindowAttribute (hwnd, DWMWA_EXCLUDED_FROM_PEEK, ref value, sizeof (int));
		}
		/// <summary>
		/// 从 Flip3D（窗口切换器）中排除指定窗口。
		/// </summary>
		/// <param name="hwnd">目标窗口句柄。</param>
		public static void RemoveFromFlip3D (IntPtr hwnd)
		{
			if (!IsDwmAvailable ()) return;
			int policy = (int)Flip3DPolicyValue.ExcludeBelow;
			DwmSetWindowAttribute (hwnd, DWMWA_FLIP3D_POLICY, ref policy, sizeof (int));
		}
		public static bool IsElderWindows () { return Environment.OSVersion.Version.Major < 6; }
		public static bool IsWindows10AndHigher () { return Environment.OSVersion.Version.Major >= 10; }
		public static bool IsNT6AndHigher () { return Environment.OSVersion.Version.Major >= 6; }
		public static bool TryEnableBlur (IntPtr hwnd, bool enable)
		{
			if (!enable) return false;
			if (!IsDwmAvailable ()) return false;
			return EnableBlur (ref hwnd, IntPtr.Zero);
		}
	}
}
