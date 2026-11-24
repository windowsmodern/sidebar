using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Sidebar
{
	public static class WindowAccent
	{
		// 定义 AccentState 枚举
		public enum AccentState
		{
			ACCENT_DISABLED = 0,
			ACCENT_ENABLE_GRADIENT = 1,
			ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
			ACCENT_ENABLE_BLURBEHIND = 3,
			ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,   // Windows 10 1803+
			ACCENT_INVALID_STATE = 5
		}

		// AccentPolicy 结构
		[StructLayout (LayoutKind.Sequential)]
		private struct AccentPolicy
		{
			public AccentState AccentState;
			public uint AccentFlags;
			public uint GradientColor;
			public uint AnimationId;
		}

		// WindowCompositionAttribute 枚举
		private enum WindowCompositionAttribute
		{
			WCA_ACCENT_POLICY = 19
		}

		// WindowCompositionAttributeData 结构
		[StructLayout (LayoutKind.Sequential)]
		private struct WindowCompositionAttributeData
		{
			public WindowCompositionAttribute Attribute;
			public IntPtr Data;
			public int SizeOfData;
		}

		// 导入 user32.dll 中的 SetWindowCompositionAttribute
		[DllImport ("user32.dll")]
		private static extern int SetWindowCompositionAttribute (IntPtr hwnd, ref WindowCompositionAttributeData data);

		/// <summary>
		/// 为指定的窗口句柄启用或禁用亚克力/模糊效果。
		/// </summary>
		/// <param name="hwnd">目标窗口句柄</param>
		/// <param name="enable">true 启用，false 禁用</param>
		/// <param name="acrylic">true 使用亚克力（仅限 Win10 1803+），false 使用经典模糊</param>
		/// <param name="color">亚克力着色颜色（仅当 acrylic=true 时生效）</param>
		/// <returns>操作是否成功</returns>
		public static bool SetAccentPolicy (IntPtr hwnd, bool enable, bool acrylic = false, uint color = 0x00FFFFFF)
		{
			if (hwnd == IntPtr.Zero)
				return false;

			AccentPolicy accent = new AccentPolicy ();

			if (enable)
			{
				// 选择效果类型
				accent.AccentState = acrylic ? AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND
											 : AccentState.ACCENT_ENABLE_BLURBEHIND;

				// 如果使用亚克力，需要设置颜色（高字节为透明度，0x00 表示不透明，可调整）
				// 颜色格式: 0xAARRGGBB（此处默认白色，半透明）
				accent.GradientColor = color;
			}
			else
			{
				accent.AccentState = AccentState.ACCENT_DISABLED;
			}

			int size = Marshal.SizeOf (typeof (AccentPolicy));
			IntPtr ptr = Marshal.AllocHGlobal (size);
			Marshal.StructureToPtr (accent, ptr, false);

			WindowCompositionAttributeData data = new WindowCompositionAttributeData {
				Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY,
				Data = ptr,
				SizeOfData = size
			};

			int result = SetWindowCompositionAttribute (hwnd, ref data);

			Marshal.FreeHGlobal (ptr);
			return result != 0;
		}
	}
}