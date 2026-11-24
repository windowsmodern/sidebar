using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Interop;

namespace Sidebar
{
	/// <summary>
	/// 表示窗口句柄的封装结构
	/// </summary>
	public partial struct HWND
	{
		private IntPtr _handle;
		public HWND (IntPtr handle)
		{
			_handle = handle;
		}
		public bool IsValid => _handle != IntPtr.Zero;
		public IntPtr Handle => _handle;
		public static implicit operator IntPtr (HWND hwnd) => hwnd._handle;
		public static implicit operator HWND (IntPtr handle) => new HWND (handle);
		public override string ToString () => $"HWND: 0x{_handle.ToInt64 ():X}";
	}
	namespace Win32
	{
		/// <summary>
		/// 矩形结构，用于表示窗口或屏幕的矩形区域。
		/// 对应 API：GetWindowRect, GetClientRect, GetMonitorInfo, MapWindowPoints 等。
		/// </summary>
		[StructLayout (LayoutKind.Sequential)]
		public struct RECT
		{
			/// <summary>矩形左边界（X 坐标）</summary>
			public int Left;
			/// <summary>矩形上边界（Y 坐标）</summary>
			public int Top;
			/// <summary>矩形右边界（X 坐标，不包含）</summary>
			public int Right;
			/// <summary>矩形下边界（Y 坐标，不包含）</summary>
			public int Bottom;
			/// <summary>构造矩形（使用左、上、右、下坐标）</summary>
			public RECT (int left, int top, int right, int bottom)
			{
				this.Left = left;
				this.Top = top;
				this.Right = right;
				this.Bottom = bottom;
			}
			/// <summary>矩形宽度（Right - Left）</summary>
			public int Width
			{
				get { return Right - Left; }
				set { Right = Left + value; }
			}
			/// <summary>矩形高度（Bottom - Top）</summary>
			public int Height
			{
				get { return Bottom - Top; }
				set { Bottom = Top + value; }
			}
			/// <summary>矩形是否为空（宽度 ≤ 0 或高度 ≤ 0）</summary>
			public bool IsEmpty
			{
				get { return Width <= 0 || Height <= 0; }
			}
			/// <summary>矩形左上角的 X 坐标</summary>
			public int X
			{
				get { return Left; }
				set { Left = value; }
			}
			/// <summary>矩形左上角的 Y 坐标</summary>
			public int Y
			{
				get { return Top; }
				set { Top = value; }
			}
			/// <summary>判断两个矩形是否相等（值比较）</summary>
			public bool Equals (RECT other)
			{
				return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
			}
			/// <summary>重写 Equals</summary>
			public override bool Equals (object obj)
			{
				if (obj is RECT)
					return Equals ((RECT)obj);
				return false;
			}
			/// <summary>重写 GetHashCode（手动计算）</summary>
			public override int GetHashCode ()
			{
				// 简单的异或组合，适合矩形坐标
				return Left ^ Top ^ Right ^ Bottom;
			}
			/// <summary>返回形如 (Left,Top)-(Right,Bottom) W=Width H=Height 的字符串</summary>
			public override string ToString ()
			{
				return string.Format ("({0},{1})-({2},{3}) W={4} H={5}", Left, Top, Right, Bottom, Width, Height);
			}
		}
		/// <summary>
		/// SetWindowPos 的 uFlags 参数选项，用于控制窗口位置/大小调整行为。
		/// 对应 API：SetWindowPos, BeginDeferWindowPos, DeferWindowPos
		/// </summary>
		[Flags]
		public enum SetWindowPosFlags: uint
		{
			/// <summary>不改变窗口大小 (SWP_NOSIZE)</summary>
			SWP_NOSIZE = 0x0001,
			/// <summary>不改变窗口位置 (SWP_NOMOVE)</summary>
			SWP_NOMOVE = 0x0002,
			/// <summary>不改变 Z 顺序 (SWP_NOZORDER)</summary>
			SWP_NOZORDER = 0x0004,
			/// <summary>不重绘窗口 (SWP_NOREDRAW)</summary>
			SWP_NOREDRAW = 0x0008,
			/// <summary>不激活窗口 (SWP_NOACTIVATE)</summary>
			SWP_NOACTIVATE = 0x0010,
			/// <summary>应用窗口框架变化，重新计算非客户区 (SWP_FRAMECHANGED)</summary>
			SWP_FRAMECHANGED = 0x0020,
			/// <summary>显示窗口 (SWP_SHOWWINDOW)</summary>
			SWP_SHOWWINDOW = 0x0040,
			/// <summary>隐藏窗口 (SWP_HIDEWINDOW)</summary>
			SWP_HIDEWINDOW = 0x0080,
			/// <summary>不改变所有者 Z 顺序 (SWP_NOCOPYBITS)</summary>
			SWP_NOCOPYBITS = 0x0100,
			/// <summary>不改变所有者位置 (SWP_NOOWNERZORDER)</summary>
			SWP_NOOWNERZORDER = 0x0200,
			/// <summary>不生成 WM_WINDOWPOSCHANGING 消息 (SWP_NOSENDCHANGING)</summary>
			SWP_NOSENDCHANGING = 0x0400,
			/// <summary>绘制窗口框架 (SWP_DRAWFRAME = SWP_FRAMECHANGED)</summary>
			SWP_DRAWFRAME = SWP_FRAMECHANGED,
			/// <summary>不改变所有者的 Z 顺序 (SWP_NOREPOSITION = SWP_NOOWNERZORDER)</summary>
			SWP_NOREPOSITION = SWP_NOOWNERZORDER,
			/// <summary>窗口始终置顶 (SWP_DEFERERASE)</summary>
			SWP_DEFERERASE = 0x2000,
			/// <summary>异步更新窗口位置 (SWP_ASYNCWINDOWPOS)</summary>
			SWP_ASYNCWINDOWPOS = 0x4000
		}
		/// <summary>
		/// ShowWindow 的 nCmdShow 参数，控制窗口的显示/隐藏状态。
		/// 对应 API：ShowWindow, ShowWindowAsync
		/// </summary>
		public enum ShowWindowCommand: int
		{
			/// <summary>隐藏窗口 (SW_HIDE)</summary>
			SW_HIDE = 0,
			/// <summary>正常显示窗口，激活并还原 (SW_SHOWNORMAL)</summary>
			SW_SHOWNORMAL = 1,
			/// <summary>最小化窗口 (SW_SHOWMINIMIZED)</summary>
			SW_SHOWMINIMIZED = 2,
			/// <summary>最大化窗口 (SW_SHOWMAXIMIZED)</summary>
			SW_SHOWMAXIMIZED = 3,
			/// <summary>显示最近大小位置，不激活 (SW_SHOWNOACTIVATE)</summary>
			SW_SHOWNOACTIVATE = 4,
			/// <summary>显示窗口当前状态 (SW_SHOW)</summary>
			SW_SHOW = 5,
			/// <summary>最小化窗口，激活下一个顶层窗口 (SW_MINIMIZE)</summary>
			SW_MINIMIZE = 6,
			/// <summary>显示最小化窗口，不激活 (SW_SHOWMINNOACTIVE)</summary>
			SW_SHOWMINNOACTIVE = 7,
			/// <summary>按当前大小位置显示，激活 (SW_SHOWNA)</summary>
			SW_SHOWNA = 8,
			/// <summary>还原窗口 (SW_RESTORE)</summary>
			SW_RESTORE = 9,
			/// <summary>显示默认大小位置，激活 (SW_SHOWDEFAULT)</summary>
			SW_SHOWDEFAULT = 10,
			/// <summary>强制最小化 (SW_FORCEMINIMIZE) - Windows 2000+</summary>
			SW_FORCEMINIMIZE = 11
		}
		/// <summary>
		/// SetWindowPos 的 hWndInsertAfter 预定义值，用于 Z 序。
		/// 对应 API：SetWindowPos
		/// </summary>
		public static class HWndForSetWindowPos
		{
			/// <summary>将窗口置于 Z 序顶部 (HWND_TOP)</summary>
			public static readonly IntPtr TOP = IntPtr.Zero;
			/// <summary>将窗口置于 Z 序底部 (HWND_BOTTOM)</summary>
			public static readonly IntPtr BOTTOM = (IntPtr)1;
			/// <summary>将窗口置于所有非顶层窗口之上，始终置顶 (HWND_TOPMOST)</summary>
			public static readonly IntPtr TOPMOST = (IntPtr)(-1);
			/// <summary>将窗口置于所有非顶层窗口之下，取消置顶 (HWND_NOTOPMOST)</summary>
			public static readonly IntPtr NOTOPMOST = (IntPtr)(-2);
			/// <summary>调整左边框大小 (SC_SIZE_LEFT)</summary>
			public static readonly IntPtr SC_SIZE_LEFT = (IntPtr)0xF001;
			/// <summary>调整右边框大小 (SC_SIZE_RIGHT)</summary>
			public static readonly IntPtr SC_SIZE_RIGHT = (IntPtr)0xF002;
			/// <summary>调整上边框大小 (SC_SIZE_TOP)</summary>
			public static readonly IntPtr SC_SIZE_TOP = (IntPtr)0xF003;
			/// <summary>调整左上角大小 (SC_SIZE_TOPLEFT)</summary>
			public static readonly IntPtr SC_SIZE_TOPLEFT = (IntPtr)0xF004;
			/// <summary>调整右上角大小 (SC_SIZE_TOPRIGHT)</summary>
			public static readonly IntPtr SC_SIZE_TOPRIGHT = (IntPtr)0xF005;
			/// <summary>调整下边框大小 (SC_SIZE_BOTTOM)</summary>
			public static readonly IntPtr SC_SIZE_BOTTOM = (IntPtr)0xF006;
			/// <summary>调整左下角大小 (SC_SIZE_BOTTOMLEFT)</summary>
			public static readonly IntPtr SC_SIZE_BOTTOMLEFT = (IntPtr)0xF007;
			/// <summary>调整右下角大小 (SC_SIZE_BOTTOMRIGHT)</summary>
			public static readonly IntPtr SC_SIZE_BOTTOMRIGHT = (IntPtr)0xF008;
			/// <summary>移动窗口 (SC_MOVE)</summary>
			public static readonly IntPtr SC_MOVE = (IntPtr)0xF010;
			/// <summary>最小化窗口 (SC_MINIMIZE)</summary>
			public static readonly IntPtr SC_MINIMIZE = (IntPtr)0xF020;
			/// <summary>最大化窗口 (SC_MAXIMIZE)</summary>
			public static readonly IntPtr SC_MAXIMIZE = (IntPtr)0xF030;
			/// <summary>关闭窗口 (SC_CLOSE)</summary>
			public static readonly IntPtr SC_CLOSE = (IntPtr)0xF060;
			/// <summary>应用程序键 (SC_KEYMENU)</summary>
			public static readonly IntPtr SC_KEYMENU = (IntPtr)0xF100;
			/// <summary>客户区 (HTCLIENT)</summary>
			public static readonly IntPtr HTCLIENT = (IntPtr)1;
			/// <summary>标题栏 (HTCAPTION)</summary>
			public static readonly IntPtr HTCAPTION = (IntPtr)2;
			/// <summary>系统菜单 (HTSYSMENU)</summary>
			public static readonly IntPtr HTSYSMENU = (IntPtr)3;
			/// <summary>大小调整框 (HTGROWBOX)</summary>
			public static readonly IntPtr HTGROWBOX = (IntPtr)4;
			/// <summary>最小化按钮 (HTMINBUTTON)</summary>
			public static readonly IntPtr HTMINBUTTON = (IntPtr)8;
			/// <summary>最大化按钮 (HTMAXBUTTON)</summary>
			public static readonly IntPtr HTMAXBUTTON = (IntPtr)9;
			/// <summary>左边框 (HTLEFT)</summary>
			public static readonly IntPtr HTLEFT = (IntPtr)10;
			/// <summary>右边框 (HTRIGHT)</summary>
			public static readonly IntPtr HTRIGHT = (IntPtr)11;
			/// <summary>上边框 (HTTOP)</summary>
			public static readonly IntPtr HTTOP = (IntPtr)12;
			/// <summary>左上角 (HTTOPLEFT)</summary>
			public static readonly IntPtr HTTOPLEFT = (IntPtr)13;
			/// <summary>右上角 (HTTOPRIGHT)</summary>
			public static readonly IntPtr HTTOPRIGHT = (IntPtr)14;
			/// <summary>下边框 (HTBOTTOM)</summary>
			public static readonly IntPtr HTBOTTOM = (IntPtr)15;
			/// <summary>左下角 (HTBOTTOMLEFT)</summary>
			public static readonly IntPtr HTBOTTOMLEFT = (IntPtr)16;
			/// <summary>右下角 (HTBOTTOMRIGHT)</summary>
			public static readonly IntPtr HTBOTTOMRIGHT = (IntPtr)17;
			/// <summary>细边框 (HTBORDER)</summary>
			public static readonly IntPtr HTBORDER = (IntPtr)18;
			/// <summary>透明区域（穿透）(HTTRANSPARENT)</summary>
			public static readonly IntPtr HTTRANSPARENT = (IntPtr)(-1);
		}
		/// <summary>
		/// 窗口消息（WM_*）常量，用于窗口过程的消息处理。
		/// 对应 API：窗口过程（WindowProc）、SendMessage、PostMessage 等。
		/// </summary>
		public enum WindowMessage: uint
		{
			/// <summary>窗口大小改变 (WM_SIZE)</summary>
			WM_SIZE = 0x0005,
			/// <summary>窗口移动 (WM_MOVE)</summary>
			WM_MOVE = 0x0003,
			/// <summary>窗口显示/隐藏 (WM_SHOWWINDOW)</summary>
			WM_SHOWWINDOW = 0x0018,
			/// <summary>窗口位置改变前 (WM_WINDOWPOSCHANGING)</summary>
			WM_WINDOWPOSCHANGING = 0x0046,
			/// <summary>窗口位置改变后 (WM_WINDOWPOSCHANGED)</summary>
			WM_WINDOWPOSCHANGED = 0x0047,
			/// <summary>进入大小移动循环 (WM_ENTERSIZEMOVE)</summary>
			WM_ENTERSIZEMOVE = 0x0231,
			/// <summary>退出大小移动循环 (WM_EXITSIZEMOVE)</summary>
			WM_EXITSIZEMOVE = 0x0232,
			/// <summary>DPI 改变 (WM_DPICHANGED) - Windows Vista+ (实际在 Windows 8.1 引入)</summary>
			WM_DPICHANGED = 0x02E0,
			/// <summary>系统颜色改变 (WM_SYSCOLORCHANGE)</summary>
			WM_SYSCOLORCHANGE = 0x0015,
			/// <summary>主题改变 (WM_THEMECHANGED) - Windows XP+</summary>
			WM_THEMECHANGED = 0x031A,
			/// <summary>非客户区绘制 (WM_NCPAINT)</summary>
			WM_NCPAINT = 0x0085,
			/// <summary>系统命令 (WM_SYSCOMMAND)</summary>
			WM_SYSCOMMAND = 0x0112,
			// 补充常用消息
			/// <summary>销毁窗口 (WM_DESTROY)</summary>
			WM_DESTROY = 0x0002,
			/// <summary>关闭窗口 (WM_CLOSE)</summary>
			WM_CLOSE = 0x0010,
			/// <summary>查询结束 (WM_QUIT)</summary>
			WM_QUIT = 0x0012,
			/// <summary>按键按下 (WM_KEYDOWN)</summary>
			WM_KEYDOWN = 0x0100,
			/// <summary>按键释放 (WM_KEYUP)</summary>
			WM_KEYUP = 0x0101,
			/// <summary>字符输入 (WM_CHAR)</summary>
			WM_CHAR = 0x0102,
			/// <summary>鼠标移动 (WM_MOUSEMOVE)</summary>
			WM_MOUSEMOVE = 0x0200,
			/// <summary>左键按下 (WM_LBUTTONDOWN)</summary>
			WM_LBUTTONDOWN = 0x0201,
			/// <summary>左键弹起 (WM_LBUTTONUP)</summary>
			WM_LBUTTONUP = 0x0202,
			/// <summary>右键按下 (WM_RBUTTONDOWN)</summary>
			WM_RBUTTONDOWN = 0x0204,
			/// <summary>右键弹起 (WM_RBUTTONUP)</summary>
			WM_RBUTTONUP = 0x0205,
			/// <summary>设定光标 (WM_SETCURSOR)</summary>
			WM_SETCURSOR = 0x0020,
			/// <summary>设定焦点 (WM_SETFOCUS)</summary>
			WM_SETFOCUS = 0x0007,
			/// <summary>失去焦点 (WM_KILLFOCUS)</summary>
			WM_KILLFOCUS = 0x0008,
			/// <summary>激活窗口 (WM_ACTIVATE)</summary>
			WM_ACTIVATE = 0x0006,
			/// <summary>开启窗口 (WM_CREATE)</summary>
			WM_CREATE = 0x0001,
			/// <summary>擦除背景 (WM_ERASEBKGND)</summary>
			WM_ERASEBKGND = 0x0014,
			/// <summary>绘客户区 (WM_PAINT)</summary>
			WM_PAINT = 0x000F,
			/// <summary>定时器 (WM_TIMER)</summary>
			WM_TIMER = 0x0113,
			/// <summary>通知父窗口 (WM_NOTIFY)</summary>
			WM_NOTIFY = 0x004E,
			/// <summary>用户自定义消息开始 (WM_USER)</summary>
			WM_USER = 0x0400,
			// Vista+ 消息
			/// <summary>DWM 组合状态改变 (WM_DWMCOMPOSITIONCHANGED) - Windows Vista+</summary>
			WM_DWMCOMPOSITIONCHANGED = 0x031E,
			/// <summary>DPI 缩放改变后 (WM_DPICHANGED_BEFOREPARENT) - Windows 10 1607+</summary>
			WM_DPICHANGED_BEFOREPARENT = 0x02E2,
			/// <summary>DPI 缩放改变后 (WM_DPICHANGED_AFTERPARENT) - Windows 10 1607+</summary>
			WM_DPICHANGED_AFTERPARENT = 0x02E3,
		}
		/// <summary>
		/// 扩展窗口样式（WS_EX_*），用于 GetWindowLong / SetWindowLong 的 GWL_EXSTYLE。
		/// 对应 API：CreateWindowEx, GetWindowLong, SetWindowLong
		/// </summary>
		[Flags]
		public enum ExtendedWindowStyles: uint
		{
			/// <summary>透明窗口，鼠标穿透 (WS_EX_TRANSPARENT)</summary>
			WS_EX_TRANSPARENT = 0x00000020,
			/// <summary>工具窗口，不在任务栏显示 (WS_EX_TOOLWINDOW)</summary>
			WS_EX_TOOLWINDOW = 0x00000080,
			/// <summary>允许 Layered 窗口（透明/半透明效果）(WS_EX_LAYERED)</summary>
			WS_EX_LAYERED = 0x00080000,
			/// <summary>支持 DPI 缩放 (WS_EX_DPI_AWARE) - Windows Vista+ (实际 Windows 8.1+)</summary>
			WS_EX_DPI_AWARE = 0x00040000,
			// 补充常用扩展样式
			/// <summary>顶层窗口 (WS_EX_TOPMOST)</summary>
			WS_EX_TOPMOST = 0x00000008,
			/// <summary>接受文件拖放 (WS_EX_ACCEPTFILES)</summary>
			WS_EX_ACCEPTFILES = 0x00000010,
			/// <summary>显示在任务栏 (WS_EX_APPWINDOW)</summary>
			WS_EX_APPWINDOW = 0x00040000,
			/// <summary>客户端边缘 (WS_EX_CLIENTEDGE)</summary>
			WS_EX_CLIENTEDGE = 0x00000200,
			/// <summary>窗口具有立体感边框 (WS_EX_STATICEDGE)</summary>
			WS_EX_STATICEDGE = 0x00020000,
			/// <summary>窗口具有对话框边框 (WS_EX_DLGMODALFRAME)</summary>
			WS_EX_DLGMODALFRAME = 0x00000001,
			/// <summary>不能最大化的窗口 (WS_EX_NOACTIVATE) - Windows 2000+</summary>
			WS_EX_NOACTIVATE = 0x08000000,
			/// <summary>不允许子窗口继承 (WS_EX_NOPARENTNOTIFY)</summary>
			WS_EX_NOPARENTNOTIFY = 0x00000004,
			/// <summary>窗口可拖拽调整大小 (WS_EX_PALETTEWINDOW) 组合</summary>
			WS_EX_PALETTEWINDOW = 0x00000188,
			/// <summary>分层窗口使用颜色键 (WS_EX_LAYERED)</summary>
			// Vista+ 扩展样式
			/// <summary>重定向位图 (WS_EX_NOREDIRECTIONBITMAP) - Windows Vista+</summary>
			WS_EX_NOREDIRECTIONBITMAP = 0x00200000,
			/// <summary>支持布局从右到左 (WS_EX_LAYOUTRTL)</summary>
			WS_EX_LAYOUTRTL = 0x00400000,
		}
		/// <summary>
		/// 普通窗口样式（WS_*），用于 GetWindowLong / SetWindowLong 的 GWL_STYLE。
		/// 对应 API：CreateWindow, CreateWindowEx, GetWindowLong, SetWindowLong
		/// </summary>
		[Flags]
		public enum WindowStyles: uint
		{
			/// <summary>重叠窗口 (WS_OVERLAPPED)</summary>
			WS_OVERLAPPED = 0x00000000,
			/// <summary>弹出窗口 (WS_POPUP)</summary>
			WS_POPUP = 0x80000000,
			/// <summary>子窗口 (WS_CHILD)</summary>
			WS_CHILD = 0x40000000,
			/// <summary>最小化按钮 (WS_MINIMIZEBOX)</summary>
			WS_MINIMIZEBOX = 0x00020000,
			/// <summary>最大化按钮 (WS_MAXIMIZEBOX)</summary>
			WS_MAXIMIZEBOX = 0x00010000,
			/// <summary>可调整边框 (WS_THICKFRAME)</summary>
			WS_THICKFRAME = 0x00040000,
			/// <summary>系统菜单 (WS_SYSMENU)</summary>
			WS_SYSMENU = 0x00080000,
			/// <summary>水平滚动条 (WS_HSCROLL)</summary>
			WS_HSCROLL = 0x00100000,
			/// <summary>垂直滚动条 (WS_VSCROLL)</summary>
			WS_VSCROLL = 0x00200000,
			/// <summary>标题栏 (WS_CAPTION = WS_BORDER | WS_DLGFRAME)</summary>
			WS_CAPTION = 0x00C00000,
			/// <summary>边框 (WS_BORDER)</summary>
			WS_BORDER = 0x00800000,
			/// <summary>对话框边框 (WS_DLGFRAME)</summary>
			WS_DLGFRAME = 0x00400000,
			/// <summary>可见 (WS_VISIBLE)</summary>
			WS_VISIBLE = 0x10000000,
			/// <summary>启用 (WS_ENABLED)</summary>
			WS_ENABLED = 0x08000000,
			// 补充常用样式
			/// <summary>剪片子窗口 (WS_CLIPCHILDREN)</summary>
			WS_CLIPCHILDREN = 0x02000000,
			/// <summary>剪片兄弟窗口 (WS_CLIPSIBLINGS)</summary>
			WS_CLIPSIBLINGS = 0x04000000,
			/// <summary>控制面板窗口 (WS_GROUP)</summary>
			WS_GROUP = 0x00020000,
			/// <summary>标签组 (WS_TABSTOP)</summary>
			WS_TABSTOP = 0x00010000,
			/// <summary>最小化状态 (WS_MINIMIZE)</summary>
			WS_MINIMIZE = 0x20000000,
			/// <summary>最大化状态 (WS_MAXIMIZE)</summary>
			WS_MAXIMIZE = 0x01000000,
		}
		/// <summary>
		/// GetWindowLong/SetWindowLong 的索引值（nIndex）。
		/// 对应 API：GetWindowLong, SetWindowLong, GetWindowLongPtr, SetWindowLongPtr
		/// </summary>
		public enum WindowLongIndex: int
		{
			/// <summary>窗口过程地址 (GWL_WNDPROC)</summary>
			GWL_WNDPROC = -4,
			/// <summary>实例句柄 (GWL_HINSTANCE)</summary>
			GWL_HINSTANCE = -6,
			/// <summary>父窗口句柄 (GWL_HWNDPARENT)</summary>
			GWL_HWNDPARENT = -8,
			/// <summary>窗口样式 (GWL_STYLE)</summary>
			GWL_STYLE = -16,
			/// <summary>扩展样式 (GWL_EXSTYLE)</summary>
			GWL_EXSTYLE = -20,
			/// <summary>用户数据 (GWL_USERDATA)</summary>
			GWL_USERDATA = -21,
			/// <summary>窗口标识符 (GWL_ID)</summary>
			GWL_ID = -12
		}
		/// <summary>
		/// SetLayeredWindowAttributes 的标志（dwFlags）。
		/// 对应 API：SetLayeredWindowAttributes, GetLayeredWindowAttributes
		/// </summary>
		[Flags]
		public enum LayeredWindowFlags: uint
		{
			/// <summary>使用颜色键（透明色）(LWA_COLORKEY)</summary>
			LWA_COLORKEY = 0x00000001,
			/// <summary>使用 Alpha 透明度 (LWA_ALPHA)</summary>
			LWA_ALPHA = 0x00000002
		}
		/// <summary>
		/// GetSystemMetrics 的索引（nIndex），获取系统度量信息。
		/// 对应 API：GetSystemMetrics
		/// </summary>
		public enum SystemMetric: int
		{
			/// <summary>屏幕宽度 (SM_CXSCREEN)</summary>
			SM_CXSCREEN = 0,
			/// <summary>屏幕高度 (SM_CYSCREEN)</summary>
			SM_CYSCREEN = 1,
			/// <summary>垂直滚动条宽度 (SM_CXVSCROLL)</summary>
			SM_CXVSCROLL = 2,
			/// <summary>水平滚动条高度 (SM_CYHSCROLL)</summary>
			SM_CYHSCROLL = 3,
			/// <summary>标题栏高度 (SM_CYCAPTION)</summary>
			SM_CYCAPTION = 4,
			/// <summary>边框宽度 (SM_CXBORDER)</summary>
			SM_CXBORDER = 5,
			/// <summary>边框高度 (SM_CYBORDER)</summary>
			SM_CYBORDER = 6,
			/// <summary>对话框框架宽度 (SM_CXDLGFRAME)</summary>
			SM_CXDLGFRAME = 7,
			/// <summary>对话框框架高度 (SM_CYDLGFRAME)</summary>
			SM_CYDLGFRAME = 8,
			/// <summary>垂直滚动条中滑块高度 (SM_CYVTHUMB)</summary>
			SM_CYVTHUMB = 9,
			/// <summary>水平滚动条中滑块宽度 (SM_CXHTHUMB)</summary>
			SM_CXHTHUMB = 10,
			/// <summary>图标宽度 (SM_CXICON)</summary>
			SM_CXICON = 11,
			/// <summary>图标高度 (SM_CYICON)</summary>
			SM_CYICON = 12,
			/// <summary>光标宽度 (SM_CXCURSOR)</summary>
			SM_CXCURSOR = 13,
			/// <summary>光标高度 (SM_CYCURSOR)</summary>
			SM_CYCURSOR = 14,
			/// <summary>菜单栏高度 (SM_CYMENU)</summary>
			SM_CYMENU = 15,
			/// <summary>全屏窗口宽度 (SM_CXFULLSCREEN)</summary>
			SM_CXFULLSCREEN = 16,
			/// <summary>全屏窗口高度 (SM_CYFULLSCREEN)</summary>
			SM_CYFULLSCREEN = 17,
			/// <summary>日文窗口高度 (SM_CYKANJIWINDOW)</summary>
			SM_CYKANJIWINDOW = 18,
			/// <summary>鼠标存在 (SM_MOUSEPRESENT)</summary>
			SM_MOUSEPRESENT = 19,
			/// <summary>垂直滚动条高度 (SM_CYVSCROLL)</summary>
			SM_CYVSCROLL = 20,
			/// <summary>水平滚动条宽度 (SM_CXHSCROLL)</summary>
			SM_CXHSCROLL = 21,
			/// <summary>调试版本 (SM_DEBUG)</summary>
			SM_DEBUG = 22,
			/// <summary>鼠标按钮交换 (SM_SWAPBUTTON)</summary>
			SM_SWAPBUTTON = 23,
			/// <summary>最小窗口宽度 (SM_CXMIN)</summary>
			SM_CXMIN = 28,
			/// <summary>最小窗口高度 (SM_CYMIN)</summary>
			SM_CYMIN = 29,
			/// <summary>标题按钮宽度 (SM_CXSIZE)</summary>
			SM_CXSIZE = 30,
			/// <summary>标题按钮高度 (SM_CYSIZE)</summary>
			SM_CYSIZE = 31,
			/// <summary>可调整边框宽度 (SM_CXFRAME)</summary>
			SM_CXFRAME = 32,
			/// <summary>可调整边框高度 (SM_CYFRAME)</summary>
			SM_CYFRAME = 33,
			/// <summary>最小跟踪宽度 (SM_CXMINTRACK)</summary>
			SM_CXMINTRACK = 34,
			/// <summary>最小跟踪高度 (SM_CYMINTRACK)</summary>
			SM_CYMINTRACK = 35,
			/// <summary>双击区域宽度 (SM_CXDOUBLECLK)</summary>
			SM_CXDOUBLECLK = 36,
			/// <summary>双击区域高度 (SM_CYDOUBLECLK)</summary>
			SM_CYDOUBLECLK = 37,
			/// <summary>图标间距宽度 (SM_CXICONSPACING)</summary>
			SM_CXICONSPACING = 38,
			/// <summary>图标间距高度 (SM_CYICONSPACING)</summary>
			SM_CYICONSPACING = 39,
			/// <summary>菜单对齐方式 (SM_MENUDROPALIGNMENT)</summary>
			SM_MENUDROPALIGNMENT = 40,
			/// <summary>笔输入支持 (SM_PENWINDOWS)</summary>
			SM_PENWINDOWS = 41,
			/// <summary>双字节字符集 (SM_DBCSENABLED)</summary>
			SM_DBCSENABLED = 42,
			/// <summary>鼠标按键数 (SM_CMOUSEBUTTONS)</summary>
			SM_CMOUSEBUTTONS = 43,
			/// <summary>安全模式 (SM_SECURE)</summary>
			SM_SECURE = 44,
			/// <summary>3D 边框宽 (SM_CXEDGE)</summary>
			SM_CXEDGE = 45,
			/// <summary>3D 边框高 (SM_CYEDGE)</summary>
			SM_CYEDGE = 46,
			/// <summary>最小间距宽 (SM_CXMINSPACING)</summary>
			SM_CXMINSPACING = 47,
			/// <summary>最小间距高 (SM_CYMINSPACING)</summary>
			SM_CYMINSPACING = 48,
			/// <summary>小图标宽 (SM_CXSMICON)</summary>
			SM_CXSMICON = 49,
			/// <summary>小图标高 (SM_CYSMICON)</summary>
			SM_CYSMICON = 50,
			/// <summary>小标题高 (SM_CYSMCAPTION)</summary>
			SM_CYSMCAPTION = 51,
			/// <summary>小标题按钮宽 (SM_CXSMSIZE)</summary>
			SM_CXSMSIZE = 52,
			/// <summary>小标题按钮高 (SM_CYSMSIZE)</summary>
			SM_CYSMSIZE = 53,
			/// <summary>菜单按钮宽 (SM_CXMENUSIZE)</summary>
			SM_CXMENUSIZE = 54,
			/// <summary>菜单按钮高 (SM_CYMENUSIZE)</summary>
			SM_CYMENUSIZE = 55,
			/// <summary>排列窗口方式 (SM_ARRANGE)</summary>
			SM_ARRANGE = 56,
			/// <summary>最小化窗口宽 (SM_CXMINIMIZED)</summary>
			SM_CXMINIMIZED = 57,
			/// <summary>最小化窗口高 (SM_CYMINIMIZED)</summary>
			SM_CYMINIMIZED = 58,
			/// <summary>最大跟踪宽 (SM_CXMAXTRACK)</summary>
			SM_CXMAXTRACK = 59,
			/// <summary>最大跟踪高 (SM_CYMAXTRACK)</summary>
			SM_CYMAXTRACK = 60,
			/// <summary>最大化窗口宽 (SM_CXMAXIMIZED)</summary>
			SM_CXMAXIMIZED = 61,
			/// <summary>最大化窗口高 (SM_CYMAXIMIZED)</summary>
			SM_CYMAXIMIZED = 62,
			/// <summary>网络存在 (SM_NETWORK)</summary>
			SM_NETWORK = 63,
			/// <summary>启动模式 (SM_CLEANBOOT)</summary>
			SM_CLEANBOOT = 67,
			/// <summary>拖拽宽度 (SM_CXDRAG)</summary>
			SM_CXDRAG = 68,
			/// <summary>拖拽高度 (SM_CYDRAG)</summary>
			SM_CYDRAG = 69,
			/// <summary>声音显示 (SM_SHOWSOUNDS)</summary>
			SM_SHOWSOUNDS = 70,
			/// <summary>菜单复选标记宽 (SM_CXMENUCHECK)</summary>
			SM_CXMENUCHECK = 71,
			/// <summary>菜单复选标记高 (SM_CYMENUCHECK)</summary>
			SM_CYMENUCHECK = 72,
			/// <summary>慢速机器 (SM_SLOWMACHINE)</summary>
			SM_SLOWMACHINE = 73,
			/// <summary>中东启用 (SM_MIDEASTENABLED)</summary>
			SM_MIDEASTENABLED = 74,
			/// <summary>鼠标滚轮存在 (SM_MOUSEWHEELPRESENT)</summary>
			SM_MOUSEWHEELPRESENT = 75,
			/// <summary>虚拟屏幕左 (SM_XVIRTUALSCREEN)</summary>
			SM_XVIRTUALSCREEN = 76,
			/// <summary>虚拟屏幕上 (SM_YVIRTUALSCREEN)</summary>
			SM_YVIRTUALSCREEN = 77,
			/// <summary>虚拟屏幕宽 (SM_CXVIRTUALSCREEN)</summary>
			SM_CXVIRTUALSCREEN = 78,
			/// <summary>虚拟屏幕高 (SM_CYVIRTUALSCREEN)</summary>
			SM_CYVIRTUALSCREEN = 79,
			/// <summary>显示器数量 (SM_CMONITORS)</summary>
			SM_CMONITORS = 80,
			/// <summary>相同显示格式 (SM_SAMEDISPLAYFORMAT)</summary>
			SM_SAMEDISPLAYFORMAT = 81,
			/// <summary>输入法启用 (SM_IMMENABLED)</summary>
			SM_IMMENABLED = 82,
			/// <summary>焦点边框宽 (SM_CXFOCUSBORDER)</summary>
			SM_CXFOCUSBORDER = 83,
			/// <summary>焦点边框高 (SM_CYFOCUSBORDER)</summary>
			SM_CYFOCUSBORDER = 84,
			/// <summary>Tablet PC 支持 (SM_TABLETPC) - Windows XP Tablet PC Edition</summary>
			SM_TABLETPC = 86,
			/// <summary>媒体中心支持 (SM_MEDIACENTER) - Windows XP Media Center Edition</summary>
			SM_MEDIACENTER = 87,
			/// <summary>简化版本 (SM_STARTER) - Windows XP</summary>
			SM_STARTER = 88,
			/// <summary>服务器 R2 (SM_SERVERR2) - Windows Server 2003 R2+</summary>
			SM_SERVERR2 = 89,
			/// <summary>公制数量 (SM_CMETRICS)</summary>
			SM_CMETRICS = 91,
			/// <summary>远程会话 (SM_REMOTESESSION) - Windows XP+</summary>
			SM_REMOTESESSION = 0x1000,
			/// <summary>系统关闭中 (SM_SHUTTINGDOWN) - Windows XP+</summary>
			SM_SHUTTINGDOWN = 0x2000,
			/// <summary>远程控制 (SM_REMOTECONTROL) - Windows 2000+</summary>
			SM_REMOTECONTROL = 0x2001,
			/// <summary>光标闪烁启用 (SM_CARETBLINKINGENABLED) - Windows 8+</summary>
			SM_CARETBLINKINGENABLED = 0x2002,
			// 补充一部分常用但可能缺失的指标
			/// <summary>悬停时间 (SM_CURSORHOVER) - Windows 2000+</summary>
			SM_CURSORHOVER = 0x2003,
			/// <summary>悬停拖拽延迟 (SM_CURSORHOVER) 同类</summary>
			SM_CURSORHOVER_HIDDEN = 0x2004,
		}
		/// <summary>
		/// 显示器信息结构体（XP 兼容）
		/// </summary>
		/// <summary>
		/// 显示器信息结构体（兼容 Windows XP）。
		/// 对应 API：GetMonitorInfo
		/// </summary>
		[StructLayout (LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct MONITORINFO
		{
			/// <summary>结构体大小（字节），调用前需设置为 Marshal.SizeOf(typeof(MONITORINFO))</summary>
			public int cbSize;
			/// <summary>显示器的完整工作区矩形（屏幕坐标）</summary>
			public RECT rcMonitor;
			/// <summary>显示器的工作区矩形（排除任务栏等）</summary>
			public RECT rcWork;
			/// <summary>显示器标志（MONITORINFOF_PRIMARY = 0x00000001 表示主显示器）</summary>
			public uint dwFlags;
			/// <summary>是否为显示器</summary>
			public bool IsPrimary
			{
				get { return (dwFlags & 0x00000001) != 0; }
			}
			/// <summary>工作区宽度</summary>
			public int WorkWidth
			{
				get { return rcWork.Width; }
			}
			/// <summary>工作区高度</summary>
			public int WorkHeight
			{
				get { return rcWork.Height; }
			}
			/// <summary>初始化结构体并设置 cbSize 字段</summary>
			public static MONITORINFO Create ()
			{
				MONITORINFO mi = new MONITORINFO ();
				mi.cbSize = Marshal.SizeOf (typeof (MONITORINFO));
				return mi;
			}
		}
		/// <summary>
		/// 窗口位置信息结构，用于 WM_WINDOWPOSCHANGING / WM_WINDOWPOSCHANGED 消息的 lParam。
		/// 对应 API：窗口消息处理
		/// </summary>
		[StructLayout (LayoutKind.Sequential)]
		public struct WINDOWPOS
		{
			/// <summary>窗口句柄</summary>
			public IntPtr hwnd;
			/// <summary>Z 序中的后置窗口句柄</summary>
			public IntPtr hwndInsertAfter;
			/// <summary>窗口 X 坐标（客户区坐标）</summary>
			public int x;
			/// <summary>窗口 Y 坐标（客户区坐标）</summary>
			public int y;
			/// <summary>窗口宽度（像素）</summary>
			public int cx;
			/// <summary>窗口高度（像素）</summary>
			public int cy;
			/// <summary>窗口位置标志（SWP_* 的组合）</summary>
			public uint flags;
			/// <summary>是否设置了 SWP_NOMOVE 标志</summary>
			public bool NoMove
			{
				get { return (flags & (uint)SetWindowPosFlags.SWP_NOMOVE) != 0; }
			}
			/// <summary>是否设置了 SWP_NOSIZE 标志</summary>
			public bool NoSize
			{
				get { return (flags & (uint)SetWindowPosFlags.SWP_NOSIZE) != 0; }
			}
			/// <summary>是否设置了 SWP_NOZORDER 标志</summary>
			public bool NoZOrder
			{
				get { return (flags & (uint)SetWindowPosFlags.SWP_NOZORDER) != 0; }
			}
		}
		/// <summary>
		/// 点结构（屏幕或客户区坐标）。
		/// 对应 API：GetCursorPos, SetCursorPos, ClientToScreen, ScreenToClient, GetWindowPlacement 等。
		/// </summary>
		[StructLayout (LayoutKind.Sequential)]
		public struct POINT
		{
			/// <summary>X 坐标</summary>
			public int X;
			/// <summary>Y 坐标</summary>
			public int Y;
			/// <summary>构造点</summary>
			public POINT (int x, int y)
			{
				X = x;
				Y = y;
			}
			/// <summary>判断两个点是否相等</summary>
			public bool Equals (POINT other)
			{
				return X == other.X && Y == other.Y;
			}
			/// <summary>重写 Equals</summary>
			public override bool Equals (object obj)
			{
				if (obj is POINT)
					return Equals ((POINT)obj);
				return false;
			}
			/// <summary>重写 GetHashCode</summary>
			public override int GetHashCode ()
			{
				return X ^ Y;
			}
			/// <summary>返回形如 (X,Y) 的字符串</summary>
			public override string ToString ()
			{
				return string.Format ("({0},{1})", X, Y);
			}
		}
		/// <summary>
		/// 窗口位置信息（用于 GetWindowPlacement / SetWindowPlacement）。
		/// 对应 API：GetWindowPlacement, SetWindowPlacement
		/// </summary>
		[StructLayout (LayoutKind.Sequential)]
		public struct WINDOWPLACEMENT
		{
			/// <summary>结构体大小（字节），调用前需初始化</summary>
			public int length;
			/// <summary>窗口放置标志（WPF_*，通常为0）</summary>
			public int flags;
			/// <summary>窗口当前显示状态（ShowWindowCommand 值）</summary>
			public int showCmd;
			/// <summary>最小化时的位置</summary>
			public POINT ptMinPosition;
			/// <summary>最大化时的位置</summary>
			public POINT ptMaxPosition;
			/// <summary>正常状态时的窗口矩形（屏幕坐标）</summary>
			public RECT rcNormalPosition;
			/// <summary>初始化结构体，设置 length 字段</summary>
			public static WINDOWPLACEMENT Create ()
			{
				WINDOWPLACEMENT wp = new WINDOWPLACEMENT ();
				wp.length = Marshal.SizeOf (typeof (WINDOWPLACEMENT));
				return wp;
			}
			/// <summary>窗口是否最小化</summary>
			public bool IsMinimized
			{
				get { return showCmd == (int)ShowWindowCommand.SW_SHOWMINIMIZED || showCmd == (int)ShowWindowCommand.SW_MINIMIZE; }
			}
			/// <summary>窗口是否最大化</summary>
			public bool IsMaximized
			{
				get { return showCmd == (int)ShowWindowCommand.SW_SHOWMAXIMIZED; }
			}
			/// <summary>窗口是否可见（非隐藏）</summary>
			public bool IsVisible
			{
				get { return showCmd != (int)ShowWindowCommand.SW_HIDE; }
			}
		}
		/// <summary>
		/// AppBar 停靠边缘，用于 SHAppBarMessage。
		/// 对应 API：SHAppBarMessage
		/// </summary>
		public enum AppBarEdge: int
		{
			/// <summary>左侧边缘</summary>
			Left = 0,
			/// <summary>顶部边缘</summary>
			Top = 1,
			/// <summary>右侧边缘</summary>
			Right = 2,
			/// <summary>底部边缘</summary>
			Bottom = 3
		}
		/// <summary>
		/// AppBar 消息命令，用于 SHAppBarMessage 的 dwMessage 参数。
		/// 对应 API：SHAppBarMessage
		/// </summary>
		public enum AppBarMessage: int
		{
			/// <summary>注册一个新的应用程序桌面工具栏 (ABM_NEW)</summary>
			New = 0x00,
			/// <summary>移除已注册的工具栏 (ABM_REMOVE)</summary>
			Remove = 0x01,
			/// <summary>查询 Toolbar 的位置信息 (ABM_QUERYPOS)</summary>
			QueryPos = 0x02,
			/// <summary>设置 Toolbar 的位置信息 (ABM_SETPOS)</summary>
			SetPos = 0x03,
			/// <summary>获取状态 (ABM_GETSTATE)</summary>
			GetState = 0x04,
			/// <summary>获取任务栏位置 (ABM_GETTASKBARPOS)</summary>
			GetTaskBarPos = 0x05,
			/// <summary>激活工具栏 (ABM_ACTIVATE)</summary>
			Activate = 0x06,
			/// <summary>获取自动隐藏栏 (ABM_GETAUTOHIDEBAR)</summary>
			GetAutoHideBar = 0x07,
			/// <summary>设置自动隐藏栏 (ABM_SETAUTOHIDEBAR)</summary>
			SetAutoHideBar = 0x08,
			/// <summary>窗口位置改变时通知 (ABM_WINDOWPOSCHANGED)</summary>
			WindowPosChanged = 0x09,
			/// <summary>设置状态 (ABM_SETSTATE)</summary>
			SetState = 0x0A
		}
		/// <summary>
		/// AppBar 通知消息，用于 APPBARDATA 的 uCallBackMessage。
		/// 对应 API：SHAppBarMessage 与注册的窗口消息。
		/// </summary>
		public enum AppBarNotification: int
		{
			/// <summary>状态改变 (ABN_STATECHANGE)</summary>
			StateChanged = 0,
			/// <summary>位置改变 (ABN_POSCHANGED)</summary>
			PosChanged = 1,
			/// <summary>全屏应用进入/退出 (ABN_FULLSCREENAPP)</summary>
			FullScreenApp = 2,
			/// <summary>窗口排列 (ABN_WINDOWARRANGE)</summary>
			WindowArrange = 3
		}
		/// <summary>
		/// AppBar 数据结构，用于 SHAppBarMessage。
		/// 对应 API：SHAppBarMessage
		/// </summary>
		[StructLayout (LayoutKind.Sequential)]
		public struct APPBARDATA
		{
			/// <summary>结构体大小（字节），调用前需设置</summary>
			public int cbSize;
			/// <summary>目标窗口句柄（注册的工具栏窗口）</summary>
			public IntPtr hWnd;
			/// <summary>回调消息 ID（由 RegisterWindowMessage 注册）</summary>
			public int uCallBackMessage;
			/// <summary>停靠边缘（AppBarEdge 值）</summary>
			public int uEdge;
			/// <summary>边界矩形（屏幕坐标）</summary>
			public RECT rc;
			/// <summary>消息参数（取决于消息类型）</summary>
			public int lParam;
			/// <summary>初始化结构体，设置 cbSize 为正确值</summary>
			public static APPBARDATA Create ()
			{
				APPBARDATA data = new APPBARDATA ();
				data.cbSize = Marshal.SizeOf (typeof (APPBARDATA));
				return data;
			}
		}
		/// <summary>
		/// FLASHWINFO 结构体，用于 FlashWindowEx 函数。[Windows XP: 兼容]
		/// 对应 API：FlashWindowEx
		/// </summary>
		[StructLayout (LayoutKind.Sequential)]
		public struct FLASHWINFO
		{
			/// <summary>结构体大小（字节）</summary>
			public uint cbSize;
			/// <summary>要闪烁的窗口句柄</summary>
			public IntPtr hwnd;
			/// <summary>闪烁标志（FlashFlags 组合）</summary>
			public uint dwFlags;
			/// <summary>闪烁次数</summary>
			public uint uCount;
			/// <summary>闪烁间隔（毫秒），0 表示使用默认光标闪烁速率</summary>
			public uint dwTimeout;

			/// <summary>创建并初始化的 FLASHWINFO 实例（已设 cbSize）</summary>
			public static FLASHWINFO Create ()
			{
				FLASHWINFO fi = new FLASHWINFO ();
				fi.cbSize = (uint)Marshal.SizeOf (typeof (FLASHWINFO));
				return fi;
			}
		}
		/// <summary>
		/// FlashWindowEx 的 dwFlags 标志。[Windows XP: 兼容]
		/// </summary>
		[Flags]
		public enum FlashFlags: uint
		{
			/// <summary>停止闪烁</summary>
			FLASHW_STOP = 0,
			/// <summary>闪烁窗口标题栏</summary>
			FLASHW_CAPTION = 1,
			/// <summary>闪烁任务栏按钮</summary>
			FLASHW_TRAY = 2,
			/// <summary>同时闪烁标题栏和任务栏按钮（FLASHW_CAPTION | FLASHW_TRAY）</summary>
			FLASHW_ALL = 3,
			/// <summary>持续闪烁直到窗口被激活</summary>
			FLASHW_TIMER = 4,
			/// <summary>持续闪烁直到窗口被激活，且无限制闪烁（需要配合 FLASHW_TIMER）</summary>
			FLASHW_TIMERNOFG = 12
		}
		[Flags]
		public enum RedrawWindowFlags: uint
		{
			 RDW_INVALIDATE = 0x0001,
			 RDW_UPDATENOW = 0x0100,
			 RDW_ERASE = 0x0004,
			 RDW_ALLCHILDREN = 0x0080
		}
		/// <summary>
		/// 表示 Win32 COLORREF 结构体（用于颜色键透明等操作）。
		/// 格式为 0x00BBGGRR：低位为红色，中位为绿色，高位为蓝色。
		/// 对应 API：SetLayeredWindowAttributes 等。
		/// </summary>
		[StructLayout (LayoutKind.Sequential)]
		public struct COLORREF
		{
			/// <summary>颜色的整数值（格式：AABBGGRR）</summary>
			public uint Value;
			public COLORREF (uint value)
			{
				Value = value;
			}
			/// <summary>Alpha 通道（0-255）</summary>
			public byte Alpha
			{
				get { return (byte)((Value >> 24) & 0xFF); }
				set { Value = (Value & 0x00FFFFFF) | ((uint)value << 24); }
			}
			/// <summary>红色通道（0-255）</summary>
			public byte Red
			{
				get { return (byte)(Value & 0xFF); }
				set { Value = (Value & 0xFFFFFF00) | value; }
			}
			/// <summary>绿色通道（0-255）</summary>
			public byte Green
			{
				get { return (byte)((Value >> 8) & 0xFF); }
				set { Value = (Value & 0xFFFF00FF) | ((uint)value << 8); }
			}
			/// <summary>蓝色通道（0-255）</summary>
			public byte Blue
			{
				get { return (byte)((Value >> 16) & 0xFF); }
				set { Value = (Value & 0xFF00FFFF) | ((uint)value << 16); }
			}
			/// <summary>根据 RGBA 值创建 COLORREF（便于构造）</summary>
			public static COLORREF FromArgb (byte alpha, byte red, byte green, byte blue)
			{
				return new COLORREF (((uint)alpha << 24) | ((uint)blue << 16) | ((uint)green << 8) | red);
			}
			/// <summary>从 System.Drawing.Color 隐式转换为 COLORREF（格式：AABBGGRR）</summary>
			public static implicit operator COLORREF (System.Drawing.Color color)
			{
				uint argb = (uint)color.ToArgb ();
				uint a = (argb & 0xFF000000) >> 24;
				uint r = argb & 0x000000FF;
				uint g = (argb & 0x0000FF00) >> 8;
				uint b = (argb & 0x00FF0000) >> 16;
				return new COLORREF ((a << 24) | (b << 16) | (g << 8) | r);
			}
			/// <summary>从 COLORREF 隐式转换为 System.Drawing.Color</summary>
			public static implicit operator System.Drawing.Color (COLORREF colorRef)
			{
				uint val = colorRef.Value;
				byte a = (byte)((val >> 24) & 0xFF);
				byte r = (byte)(val & 0xFF);
				byte g = (byte)((val >> 8) & 0xFF);
				byte b = (byte)((val >> 16) & 0xFF);
				return System.Drawing.Color.FromArgb (a, r, g, b);
			}
			/// <summary>从 uint 隐式转换为 COLORREF</summary>
			public static implicit operator COLORREF (uint value)
			{
				return new COLORREF (value);
			}
			/// <summary>从 COLORREF 隐式转换为 uint</summary>
			public static implicit operator uint (COLORREF colorRef)
			{
				return colorRef.Value;
			}
			/// <summary>从 int 隐式转换为 COLORREF</summary>
			public static implicit operator COLORREF (int value)
			{
				return new COLORREF ((uint)value);
			}
			/// <summary>从 COLORREF 隐式转换为 int</summary>
			public static implicit operator int (COLORREF colorRef)
			{
				return (int)colorRef.Value;
			}
			/// <summary>从 System.Windows.Media.Color 隐式转换为 COLORREF</summary>
			public static implicit operator COLORREF (System.Windows.Media.Color color)
			{
				// 注意：WPF 的 Color 使用 A,R,G,B 属性，不要用 ToArgb（那是 Drawing 的扩展）
				uint a = color.A;
				uint r = color.R;
				uint g = color.G;
				uint b = color.B;
				return new COLORREF ((a << 24) | (b << 16) | (g << 8) | r);
			}
			/// <summary>从 COLORREF 隐式转换为 System.Windows.Media.Color</summary>
			public static implicit operator System.Windows.Media.Color (COLORREF colorRef)
			{
				uint val = colorRef.Value;
				byte a = (byte)((val >> 24) & 0xFF);
				byte r = (byte)(val & 0xFF);
				byte g = (byte)((val >> 8) & 0xFF);
				byte b = (byte)((val >> 16) & 0xFF);
				return System.Windows.Media.Color.FromArgb (a, r, g, b);
			}
			/// <summary>返回形如 COLORREF: 0x00BBGGRR 的字符串</summary>
			public override string ToString ()
			{
				return $"COLORREF: 0x{Value:X8}";
			}
		}
	}
	/// <summary>
	/// Win32 API 原生方法的静态封装（兼容 Windows XP）。
	/// 对于涉及字符串的函数，同时提供了 Auto、A、W 三种版本，推荐显式调用 W 版本以避免编码问题。
	/// </summary>
	public static class Win32WindowNative
	{
		/// <summary>获取窗口的屏幕坐标矩形（包含标题栏、边框）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool GetWindowRect (IntPtr hWnd, out Win32.RECT lpRect);
		/// <summary>获取窗口客户区矩形（客户区坐标，左上角恒为(0,0)）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool GetClientRect (IntPtr hWnd, out Win32.RECT lpRect);
		/// <summary>移动或改变窗口大小（基于左上角屏幕坐标）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool MoveWindow (IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);
		/// <summary>设置窗口位置、大小、Z序、显示状态等。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool SetWindowPos (IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
		/// <summary>显示或隐藏窗口。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool ShowWindow (IntPtr hWnd, int nCmdShow);
		/// <summary>获取窗口样式或扩展样式（32位值）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern int GetWindowLong (IntPtr hWnd, int nIndex);
		/// <summary>修改窗口样式或扩展样式（32位值）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern int SetWindowLong (IntPtr hWnd, int nIndex, int dwNewLong);
		/// <summary>设置分层窗口的透明色键或全局Alpha。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool SetLayeredWindowAttributes (IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);
		/// <summary>获取分层窗口的透明参数。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool GetLayeredWindowAttributes (IntPtr hwnd, out uint crKey, out byte bAlpha, out uint dwFlags);
		/// <summary>获取系统度量值（如屏幕尺寸）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern int GetSystemMetrics (int nIndex);
		/// <summary>获取包含指定窗口的显示器句柄。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll")]
		public static extern IntPtr MonitorFromWindow (IntPtr hwnd, uint dwFlags);
		/// <summary>获取显示器详细信息。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool GetMonitorInfo (IntPtr hMonitor, ref Win32.MONITORINFO lpmi);
		/// <summary>获取光标屏幕位置。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll")]
		public static extern bool GetCursorPos (out Win32.POINT lpPoint);
		/// <summary>设置光标屏幕位置。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll")]
		public static extern bool SetCursorPos (int X, int Y);
		/// <summary>将客户区坐标转换为屏幕坐标。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll")]
		public static extern bool ClientToScreen (IntPtr hWnd, ref Win32.POINT lpPoint);
		/// <summary>将屏幕坐标转换为客户区坐标。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll")]
		public static extern bool ScreenToClient (IntPtr hWnd, ref Win32.POINT lpPoint);
		/// <summary>查找子窗口（Auto 字符集，兼容旧代码）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern IntPtr FindWindowEx (IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
		/// <summary>查找子窗口（ANSI 版本）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
		public static extern IntPtr FindWindowExA (IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
		/// <summary>查找子窗口（Unicode 版本，推荐）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindowExW (IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
		/// <summary>注册窗口消息（Auto 字符集，兼容旧代码）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int RegisterWindowMessage (string lpString);
		/// <summary>注册窗口消息（ANSI 版本）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
		public static extern int RegisterWindowMessageA (string lpString);
		/// <summary>注册窗口消息（Unicode 版本，推荐）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern int RegisterWindowMessageW (string lpString);
		/// <summary>发送消息（Auto 字符集，兼容旧代码）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage (IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
		/// <summary>发送消息（ANSI 版本）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", CharSet = CharSet.Ansi)]
		public static extern IntPtr SendMessageA (IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
		/// <summary>发送消息（Unicode 版本，推荐）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", CharSet = CharSet.Unicode)]
		public static extern IntPtr SendMessageW (IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
		/// <summary>设置窗口标题（Auto 字符集）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool SetWindowText (IntPtr hWnd, string lpString);
		/// <summary>设置窗口标题（ANSI 版本）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
		public static extern bool SetWindowTextA (IntPtr hWnd, string lpString);
		/// <summary>设置窗口标题（Unicode 版本）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern bool SetWindowTextW (IntPtr hWnd, string lpString);
		/// <summary>获取窗口标题（Auto 字符集）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetWindowText (IntPtr hWnd, StringBuilder lpString, int nMaxCount);
		/// <summary>获取窗口标题（ANSI 版本）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
		public static extern int GetWindowTextA (IntPtr hWnd, StringBuilder lpString, int nMaxCount);
		/// <summary>获取窗口标题（Unicode 版本）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern int GetWindowTextW (IntPtr hWnd, StringBuilder lpString, int nMaxCount);
		/// <summary>获取窗口标题长度（Auto 字符集）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetWindowTextLength (IntPtr hWnd);
		/// <summary>获取窗口标题长度（ANSI 版本）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
		public static extern int GetWindowTextLengthA (IntPtr hWnd);
		/// <summary>获取窗口标题长度（Unicode 版本）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern int GetWindowTextLengthW (IntPtr hWnd);
		/// <summary>获取窗口位置状态（含最小化/最大化信息）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool GetWindowPlacement (IntPtr hWnd, ref Win32.WINDOWPLACEMENT lpwndpl);
		/// <summary>设置窗口区域（创建异形窗口）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern int SetWindowRgn (IntPtr hWnd, IntPtr hRgn, bool bRedraw);
		/// <summary>创建矩形区域（需调用 DeleteObject 释放）。[Windows XP: 兼容]</summary>
		[DllImport ("gdi32.dll", SetLastError = true)]
		public static extern IntPtr CreateRectRgn (int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);
		/// <summary>删除 GDI 对象（如区域、画笔等）。[Windows XP: 兼容]</summary>
		[DllImport ("gdi32.dll", SetLastError = true)]
		public static extern bool DeleteObject (IntPtr hObject);
		/// <summary>与 Windows 任务栏通信（应用程序桌面工具栏）。[Windows XP: 兼容]</summary>
		[DllImport ("shell32.dll", SetLastError = true)]
		public static extern int SHAppBarMessage (int dwMessage, ref Win32.APPBARDATA pData);
		/// <summary>根据窗口样式计算所需的窗口矩形（包含边框和标题栏）。[Windows 2000+，XP 兼容]</summary>
		/// <param name="lpRect">指向所需客户区矩形的指针（输入/输出为窗口矩形）。</param>
		/// <param name="dwStyle">窗口样式（WS_* 组合）。</param>
		/// <param name="bMenu">是否有菜单（菜单会增加高度）。</param>
		/// <param name="dwExStyle">扩展窗口样式（WS_EX_* 组合）。</param>
		/// <returns>成功返回 true。</returns>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool AdjustWindowRectEx (ref Win32.RECT lpRect, uint dwStyle, bool bMenu, uint dwExStyle);
		/// <summary>使用 RECT 结构移动窗口（调用 MoveWindow）。[Windows XP: 兼容]</summary>
		public static bool MoveWindow (IntPtr hWnd, Win32.RECT rect, bool bRepaint)
		{
			return MoveWindow (hWnd, rect.Left, rect.Top, rect.Width, rect.Height, bRepaint);
		}
		/// <summary>使用 RECT 结构设置窗口位置与大小（调用 SetWindowPos）。[Windows XP: 兼容]</summary>
		public static bool SetWindowPos (IntPtr hWnd, IntPtr hWndInsertAfter, Win32.RECT rect, uint uFlags)
		{
			return SetWindowPos (hWnd, hWndInsertAfter, rect.Left, rect.Top, rect.Width, rect.Height, uFlags);
		}
		/// <summary>获取结构体在托管内存中的大小（字节）。[Windows XP: 兼容]</summary>
		public static int SizeOf<T> (T obj)
		{
			return Marshal.SizeOf (obj);
		}
		[DllImport ("user32.dll")]
		public static extern bool IsIconic (IntPtr hWnd);
		[DllImport ("user32.dll")]
		public static extern bool IsZoomed (IntPtr hWnd);
		[DllImport ("user32.dll")]
		public static extern bool SetForegroundWindow (IntPtr hWnd);
		[DllImport ("user32.dll")]
		public static extern bool FlashWindowEx (ref Win32.FLASHWINFO pwfi);
		/// <summary>标记窗口的指定客户区为无效（需要重绘）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool InvalidateRect (IntPtr hWnd, IntPtr lpRect, bool bErase);
		/// <summary>立即发送 WM_PAINT 消息到窗口（若有无效区域）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern bool UpdateWindow (IntPtr hWnd);
		/// <summary>直接重绘窗口的指定区域（支持更多标志）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll")]
		public static extern bool RedrawWindow (IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);
		/// <summary>查找顶层窗口（Auto 字符集，兼容旧代码）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern IntPtr FindWindow (string lpClassName, string lpWindowName);
		/// <summary>查找顶层窗口（ANSI 版本）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
		public static extern IntPtr FindWindowA (string lpClassName, string lpWindowName);
		/// <summary>查找顶层窗口（Unicode 版本，推荐）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
		public static extern IntPtr FindWindowW (string lpClassName, string lpWindowName);
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern IntPtr SetWindowLongPtr (IntPtr hWnd, int nIndex, IntPtr dwNewLong);
		public delegate bool EnumWindowsProc (IntPtr hWnd, IntPtr lParam);
		[DllImport ("user32.dll")]
		[return: MarshalAs (UnmanagedType.Bool)]
		public static extern bool EnumWindows (EnumWindowsProc lpEnumFunc, IntPtr lParam);
		[DllImport ("gdi32.dll", SetLastError = true)]
		public static extern IntPtr CreateRoundRectRgn (int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);
		/// <summary>获取窗口所在显示器的 DPI（Win10 1607+）。旧系统调用会抛 EntryPointNotFoundException。</summary>
		[DllImport ("user32.dll", SetLastError = true)]
		public static extern uint GetDpiForWindow (IntPtr hwnd);
		/// <summary>获取窗口设备上下文（旧系统回退方案）。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll")]
		public static extern IntPtr GetDC (IntPtr hWnd);
		/// <summary>释放设备上下文。[Windows XP: 兼容]</summary>
		[DllImport ("user32.dll")]
		public static extern int ReleaseDC (IntPtr hWnd, IntPtr hDC);
		/// <summary>获取设备能力（如 DPI）。LOGPIXELSX = 88。[Windows XP: 兼容]</summary>
		[DllImport ("gdi32.dll")]
		public static extern int GetDeviceCaps (IntPtr hdc, int nIndex);
	}
	public static class HWndExtraMethods
	{
		public static bool SetShow (this HWND hWnd, Win32.ShowWindowCommand cmd) => Win32WindowNative.ShowWindow (hWnd, (int)cmd); 
		public static bool Show (this HWND hWnd) => Win32WindowNative.ShowWindow (hWnd, (int)Win32.ShowWindowCommand.SW_SHOW);
		public static bool Hide (this HWND hWnd) => Win32WindowNative.ShowWindow (hWnd, (int)Win32.ShowWindowCommand.SW_HIDE);
		public static bool Move (this HWND hWnd, int ?left = null, int ?top = null, int ?width = null, int ?height = null, bool repaint = true)
		{
			var rect = hWnd.Rect;
			return Win32WindowNative.MoveWindow (hWnd, left ?? rect.Left, top ?? rect.Top, width ?? rect.Width, height ?? rect.Height, repaint);
		}
		public static bool Resize (this HWND hWnd, int ?width = null, int ?height = null, bool repaint = true)
			=> hWnd.Move (null, null, width, height, repaint);
		public static bool SetPosition (this HWND hWnd, HWND insertAfter, int ?left = null, int ?top = null, int ?width = null, int ?height = null, uint ?flags = null)
		{
			var rect = hWnd.Rect;
			return Win32WindowNative.SetWindowPos (hWnd, insertAfter, left ?? rect.Left, top ?? rect.Top, width ?? rect.Width, height ?? rect.Height, flags ?? 0);
		}
		public static bool SetPosition (this HWND hWnd, int? left = null, int? top = null, int? width = null, int? height = null, uint? flags = null)
		{
			var rect = hWnd.Rect;
			return SetPosition (hWnd, IntPtr.Zero, left ?? rect.Left, top ?? rect.Top, width ?? rect.Width, height ?? rect.Height, flags ?? 0);
		}
		/// <summary>
		/// 获取窗口样式
		/// </summary>
		public static int GetStyle (this HWND hwnd)
		{
			return Win32WindowNative.GetWindowLong (hwnd, (int)Win32.WindowLongIndex.GWL_STYLE);
		}
		/// <summary>
		/// 设置窗口样式
		/// </summary>
		public static int SetStyle (this HWND hwnd, int style)
		{
			return Win32WindowNative.SetWindowLong (hwnd, (int)Win32.WindowLongIndex.GWL_STYLE, style);
		}
		public static bool SetOpacity (this HWND hwnd, byte alpha)
		{
			int exStyle = Win32WindowNative.GetWindowLong (hwnd, (int)Win32.WindowLongIndex.GWL_EXSTYLE);
			if ((exStyle & (int)Win32.ExtendedWindowStyles.WS_EX_LAYERED) == 0)
			{
				Win32WindowNative.SetWindowLong (hwnd, (int)Win32.WindowLongIndex.GWL_EXSTYLE, exStyle | (int)Win32.ExtendedWindowStyles.WS_EX_LAYERED);
			}
			return Win32WindowNative.SetLayeredWindowAttributes (hwnd, 0, alpha, (uint)Win32.LayeredWindowFlags.LWA_ALPHA);
		}
		public static byte? GetOpacity (this HWND hwnd)
		{
			int exStyle = Win32WindowNative.GetWindowLong (hwnd, (int)Win32.WindowLongIndex.GWL_EXSTYLE);
			if ((exStyle & (int)Win32.ExtendedWindowStyles.WS_EX_LAYERED) == 0)
				return null;
			uint crKey;
			byte alpha;
			uint flags;
			if (Win32WindowNative.GetLayeredWindowAttributes (hwnd, out crKey, out alpha, out flags))
			{
				if ((flags & (uint)Win32.LayeredWindowFlags.LWA_ALPHA) != 0)
					return alpha;
			}
			return null;
		}
		/// <summary>
		/// 获取光标位置（屏幕坐标，像素）
		/// </summary>
		public static Win32.POINT GetCursorPos ()
		{
			Win32.POINT pt;
			Win32WindowNative.GetCursorPos (out pt);
			return pt;
		}
		/// <summary>
		/// 查找子窗口
		/// </summary>
		public static IntPtr FindWindowEx (this HWND hwnd, IntPtr childAfter, string className, string windowName)
		{
			return Win32WindowNative.FindWindowEx (hwnd, childAfter, className, windowName);
		}
		/// <summary>
		/// 获取窗口位置信息（包括最小化/最大化状态）
		/// </summary>
		public static Win32.WINDOWPLACEMENT GetPlacement (this HWND hwnd)
		{
			var placement = new Win32.WINDOWPLACEMENT ();
			placement.length = Marshal.SizeOf (typeof (Win32.WINDOWPLACEMENT));
			if (!Win32WindowNative.GetWindowPlacement (hwnd, ref placement))
				throw new Win32Exception ();
			return placement;
		}
		/// <summary>
		/// 设置窗口区域（用于裁剪窗口形状）
		/// </summary>
		/// <param name="hwnd">窗口句柄</param>
		/// <param name="region">区域句柄（GDI 区域）</param>
		/// <param name="redraw">是否立即重绘</param>
		public static bool SetWindowRgn (this HWND hwnd, IntPtr region, bool redraw)
		{
			return Win32WindowNative.SetWindowRgn (hwnd, region, redraw) != 0;
		}
		/// <summary>
		/// 创建一个矩形区域（GDI 对象）
		/// </summary>
		/// <returns>区域句柄，使用完毕后需调用 DeleteObject 释放</returns>
		public static IntPtr CreateRectRgn (int left, int top, int right, int bottom)
		{
			return Win32WindowNative.CreateRectRgn (left, top, right, bottom);
		}
		/// <summary>
		/// 发送消息到窗口
		/// </summary>
		public static IntPtr SendMessage (this HWND hwnd, uint msg, IntPtr ?wParam = null, IntPtr ?lParam = null)
		{
			return Win32WindowNative.SendMessage (hwnd, msg, wParam ?? IntPtr.Zero, lParam ?? IntPtr.Zero);
		}
		public static IntPtr SendMessage (this HWND hwnd, Win32.WindowMessage msg, IntPtr ?wParam = null, IntPtr ?lParam = null)
			=> SendMessage (hwnd, (uint)msg, wParam, lParam);
		#region 窗口标题（Unicode）
		/// <summary>获取窗口标题。[Windows XP: 兼容]</summary>
		public static string GetWindowTitle (this HWND hwnd)
		{
			int len = Win32WindowNative.GetWindowTextLengthW (hwnd);
			if (len == 0) return string.Empty;
			StringBuilder sb = new StringBuilder (len + 1);
			Win32WindowNative.GetWindowTextW (hwnd, sb, sb.Capacity);
			return sb.ToString ();
		}
		/// <summary>设置窗口标题。[Windows XP: 兼容]</summary>
		public static bool SetWindowTitle (this HWND hwnd, string title)
		{
			return Win32WindowNative.SetWindowTextW (hwnd, title);
		}
		#endregion
		#region 窗口置顶
		/// <summary>获取或设置窗口是否置顶（TopMost）。[Windows XP: 兼容]</summary>
		public static bool GetTopMost (this HWND hwnd)
		{
			int exStyle = Win32WindowNative.GetWindowLong (hwnd, (int)Win32.WindowLongIndex.GWL_EXSTYLE);
			return (exStyle & (int)Win32.ExtendedWindowStyles.WS_EX_TOPMOST) != 0;
		}
		/// <summary>设置窗口是否置顶。[Windows XP: 兼容]</summary>
		public static bool SetTopMost (this HWND hwnd, bool topMost)
		{
			IntPtr insertAfter = topMost ? Win32.HWndForSetWindowPos.TOPMOST : Win32.HWndForSetWindowPos.NOTOPMOST;
			return Win32WindowNative.SetWindowPos (hwnd, insertAfter, 0, 0, 0, 0,
				(uint)(Win32.SetWindowPosFlags.SWP_NOMOVE | Win32.SetWindowPosFlags.SWP_NOSIZE | Win32.SetWindowPosFlags.SWP_NOACTIVATE));
		}
		#endregion
		#region 窗口状态（最小化/最大化/还原）
		/// <summary>最小化窗口。[Windows XP: 兼容]</summary>
		public static bool Minimize (this HWND hwnd)
		{
			return Win32WindowNative.ShowWindow (hwnd, (int)Win32.ShowWindowCommand.SW_MINIMIZE);
		}
		/// <summary>最大化窗口。[Windows XP: 兼容]</summary>
		public static bool Maximize (this HWND hwnd)
		{
			return Win32WindowNative.ShowWindow (hwnd, (int)Win32.ShowWindowCommand.SW_SHOWMAXIMIZED);
		}
		/// <summary>还原窗口（从最小化或最大化恢复）。[Windows XP: 兼容]</summary>
		public static bool Restore (this HWND hwnd)
		{
			return Win32WindowNative.ShowWindow (hwnd, (int)Win32.ShowWindowCommand.SW_RESTORE);
		}
		/// <summary>判断窗口是否最小化。[Windows XP: 兼容]</summary>
		public static bool IsMinimized (this HWND hwnd)
		{
			return Win32WindowNative.IsIconic (hwnd);
		}
		/// <summary>判断窗口是否最大化。[Windows XP: 兼容]</summary>
		public static bool IsMaximized (this HWND hwnd)
		{
			return Win32WindowNative.IsZoomed (hwnd);
		}
		#endregion
		#region 窗口激活与焦点
		/// <summary>激活窗口并将其带到前台。[Windows XP: 兼容]</summary>
		public static bool Activate (this HWND hwnd)
		{
			return Win32WindowNative.SetForegroundWindow (hwnd);
		}
		/// <summary>将窗口置于 Z 序顶部（不激活）。[Windows XP: 兼容]</summary>
		public static bool BringToTop (this HWND hwnd)
		{
			return Win32WindowNative.SetWindowPos (hwnd, Win32.HWndForSetWindowPos.TOP, 0, 0, 0, 0,
				(uint)(Win32.SetWindowPosFlags.SWP_NOMOVE | Win32.SetWindowPosFlags.SWP_NOSIZE | Win32.SetWindowPosFlags.SWP_NOACTIVATE));
		}
		#endregion
		#region 窗口闪烁（吸引用户注意）
		/// <summary>闪烁窗口一次。[Windows XP: 兼容]</summary>
		public static bool Flash (this HWND hwnd)
		{
			Win32.FLASHWINFO fi = Win32.FLASHWINFO.Create ();
			fi.hwnd = hwnd;
			fi.dwFlags = (uint)Win32.FlashFlags.FLASHW_CAPTION | (uint)Win32.FlashFlags.FLASHW_TRAY;
			fi.uCount = 1;
			fi.dwTimeout = 0;
			return Win32WindowNative.FlashWindowEx (ref fi);
		}
		/// <summary>闪烁窗口指定次数。[Windows XP: 兼容]</summary>
		/// <param name="count">闪烁次数，0 表示无限闪烁直到停止</param>
		public static bool Flash (this HWND hwnd, uint count)
		{
			Win32.FLASHWINFO fi = Win32.FLASHWINFO.Create ();
			fi.hwnd = hwnd;
			fi.dwFlags = (uint)Win32.FlashFlags.FLASHW_ALL;
			fi.uCount = count;
			fi.dwTimeout = 0;
			return Win32WindowNative.FlashWindowEx (ref fi);
		}
		/// <summary>停止闪烁。[Windows XP: 兼容]</summary>
		public static bool StopFlash (this HWND hwnd)
		{
			Win32.FLASHWINFO fi = Win32.FLASHWINFO.Create ();
			fi.hwnd = hwnd;
			fi.dwFlags = (uint)Win32.FlashFlags.FLASHW_STOP;
			return Win32WindowNative.FlashWindowEx (ref fi);
		}
		#endregion
		#region 窗口坐标辅助（获取工作区矩形）
		/// <summary>获取窗口所在显示器的工作区矩形（排除任务栏）。[Windows XP: 兼容]</summary>
		public static Win32.RECT GetWorkArea (this HWND hwnd)
		{
			IntPtr monitor = Win32WindowNative.MonitorFromWindow (hwnd, 0);
			Win32.MONITORINFO mi = Win32.MONITORINFO.Create ();
			Win32WindowNative.GetMonitorInfo (monitor, ref mi);
			return mi.rcWork;
		}
		/// <summary>获取窗口所在显示器的完整矩形。[Windows XP: 兼容]</summary>
		public static Win32.RECT GetMonitorRect (this HWND hwnd)
		{
			IntPtr monitor = Win32WindowNative.MonitorFromWindow (hwnd, 0);
			Win32.MONITORINFO mi = Win32.MONITORINFO.Create ();
			Win32WindowNative.GetMonitorInfo (monitor, ref mi);
			return mi.rcMonitor;
		}
		#endregion
		/// <summary>
		/// 获取窗口扩展样式
		/// </summary>
		public static int GetExStyle (this HWND hwnd)
		{
			return Win32WindowNative.GetWindowLong (hwnd, (int)Win32.WindowLongIndex.GWL_EXSTYLE);
		}
		/// <summary>
		/// 设置窗口扩展样式，返回旧的扩展样式值。
		/// </summary>
		public static int SetExStyle (this HWND hwnd, int exStyle)
		{
			return Win32WindowNative.SetWindowLong (hwnd, (int)Win32.WindowLongIndex.GWL_EXSTYLE, exStyle);
		}
		#region 窗口重绘
		/// <summary>
		/// 使窗口的整个客户区无效，并可选擦除背景。
		/// 无效区域会在消息空闲时被重绘（发送 WM_PAINT）。
		/// </summary>
		/// <param name="hwnd">窗口句柄</param>
		/// <param name="erase">是否擦除背景（true 会发送 WM_ERASEBKGND）</param>
		/// <returns>成功返回 true</returns>
		public static bool Invalidate (this HWND hwnd, bool erase = false)
		{
			return Win32WindowNative.InvalidateRect (hwnd, IntPtr.Zero, erase);
		}
		/// <summary>
		/// 强制立即重绘窗口（如果有无效区域）。
		/// 如果没有无效区域，则不做任何操作。
		/// </summary>
		/// <param name="hwnd">窗口句柄</param>
		/// <returns>成功返回 true</returns>
		public static bool Update (this HWND hwnd)
		{
			return Win32WindowNative.UpdateWindow (hwnd);
		}
		/// <summary>
		/// 强制立即重绘窗口的整个客户区（组合 Invalidate + Update）。
		/// 相当于调用 Invalidate(true) 然后 Update()。
		/// </summary>
		/// <param name="hwnd">窗口句柄</param>
		/// <returns>成功返回 true</returns>
		public static bool Refresh (this HWND hwnd)
		{
			if (!Win32WindowNative.InvalidateRect (hwnd, IntPtr.Zero, true))
				return false;
			return Win32WindowNative.UpdateWindow (hwnd);
		}
		/// <summary>
		/// 使用 RedrawWindow API 立即重绘窗口，支持更多标志。
		/// </summary>
		/// <param name="hwnd">窗口句柄</param>
		/// <param name="flags">重绘标志（RDW_* 组合，例如 RDW_INVALIDATE | RDW_UPDATENOW | RDW_ERASE）</param>
		/// <returns>成功返回 true</returns>
		public static bool Redraw (this HWND hwnd, uint flags)
		{
			return Win32WindowNative.RedrawWindow (hwnd, IntPtr.Zero, IntPtr.Zero, flags);
		}
		#endregion
	}
	public partial struct HWND
	{
		public Win32.RECT Rect
		{
			get
			{
				Win32.RECT r;
				Win32WindowNative.GetWindowRect (this, out r);
				return r;
			}
		}
		public Win32.RECT ClientRect
		{
			get
			{
				Win32.RECT r;
				Win32WindowNative.GetClientRect (this, out r);
				return r;
			}
		}
		public int Left
		{
			get { return Rect.Left; }
			set { this.SetPosition (value, null, null, null); }
		}
		public int Top
		{
			get { return Rect.Top; }
			set { this.SetPosition (null, value, null, null); }
		}
		public int Width
		{
			get { return Rect.Width; }
			set { this.SetPosition (null, null, value, null); }
		}
		public int Height
		{
			get { return Rect.Height; }
			set { this.SetPosition (null, null, null, value); }
		}
		public int ClientLeft => Rect.Left;
		public int ClientTop => Rect.Top;
		public int ClientWidth => ClientRect.Width;
		public int ClientHeight => ClientRect.Height;
		public System.Drawing.Size Size => new System.Drawing.Size (Width, Height);
		public System.Drawing.Size ClientSize => new System.Drawing.Size (ClientWidth, ClientHeight);
		public Win32.WindowStyles Styles
		{
			get { return (Win32.WindowStyles)Win32WindowNative.GetWindowLong (this, (int)Win32.WindowLongIndex.GWL_STYLE); }
			set { Win32WindowNative.SetWindowLong (this, (int)Win32.WindowLongIndex.GWL_STYLE, (int)value); }
		}
		/// <summary>
		/// 0 ~ 255，越大越不透明
		/// </summary>
		public byte Opacity
		{
			get { return this.GetOpacity () ?? 255; }
			set { this.SetOpacity (value); }
		}
		/// <summary>
		/// 获取或设置窗口是否可见
		/// </summary>
		public bool Visibility
		{
			get
			{
				return (Styles & Win32.WindowStyles.WS_VISIBLE) != 0;
			}
			set
			{
				var style = Styles;
				if (value)
					style |= Win32.WindowStyles.WS_VISIBLE;
				else
					style &= ~Win32.WindowStyles.WS_VISIBLE;
				Styles = style;
			}
		}
		/// <summary>
		/// 获取或设置窗口是否禁用
		/// </summary>
		public bool Disabled
		{
			get
			{
				return (Styles & Win32.WindowStyles.WS_ENABLED) != 0;
			}
			set
			{
				var style = Styles;
				if (value)
					style |= Win32.WindowStyles.WS_ENABLED;
				else
					style &= ~Win32.WindowStyles.WS_ENABLED;
				Styles = style;
			}
		}
		/// <summary>窗口标题（获取或设置）。[Windows XP: 兼容]</summary>
		public string Title
		{
			get { return this.GetWindowTitle (); }
			set { this.SetWindowTitle (value); }
		}
		/// <summary>窗口是否置顶（TopMost）。[Windows XP: 兼容]</summary>
		public bool TopMost
		{
			get { return this.GetTopMost (); }
			set { this.SetTopMost (value); }
		}
		/// <summary>窗口是否已最小化。[Windows XP: 兼容]</summary>
		public bool IsMinimized
		{
			get { return this.IsMinimized (); }
		}
		/// <summary>窗口是否已最大化。[Windows XP: 兼容]</summary>
		public bool IsMaximized
		{
			get { return this.IsMaximized (); }
		}
		/// <summary>窗口是否处于正常状态（非最小化且非最大化）。[Windows XP: 兼容]</summary>
		public bool IsNormal
		{
			get { return !IsMinimized && !IsMaximized; }
		}
		/// <summary>窗口所在显示器的工作区矩形。[Windows XP: 兼容]</summary>
		public Win32.RECT WorkArea
		{
			get { return this.GetWorkArea (); }
		}
		/// <summary>窗口所在显示器的完整矩形。[Windows XP: 兼容]</summary>
		public Win32.RECT MonitorRect
		{
			get { return this.GetMonitorRect (); }
		}
		/// <summary>
		/// 获取或设置窗口的扩展样式（WS_EX_* 组合）。
		/// </summary>
		public Win32.ExtendedWindowStyles StylesEx
		{
			get { return (Win32.ExtendedWindowStyles)this.GetExStyle (); }
			set { this.SetExStyle ((int)value); }
		}
	}
	public interface IWin32WindowInterop
	{
		HWND HWnd { get; }
	}
	public interface IWindowInterop
	{
		WindowInteropHelper InteropHelper { get; }
		IntPtr Handle { get; }
		IntPtr WndOwner { get; }
	}
}
