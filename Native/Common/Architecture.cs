using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Sidebar
{
	/// <summary>
	/// 处理器架构枚举，值与 Win32 API 宏 PROCESSOR_ARCHITECTURE_* 保持一致。
	/// </summary>
	public enum ProcessorArchitecture: ushort
	{
		/// <summary>Intel x86</summary>
		X86 = 0,            // PROCESSOR_ARCHITECTURE_INTEL
							/// <summary>ARM</summary>
		ARM = 5,            // PROCESSOR_ARCHITECTURE_ARM
							/// <summary>Intel Itanium (IA-64)</summary>
		IA64 = 6,           // PROCESSOR_ARCHITECTURE_IA64
							/// <summary>x64 (AMD or Intel 64-bit)</summary>
		X64 = 9,            // PROCESSOR_ARCHITECTURE_AMD64
							/// <summary>ARM64</summary>
		ARM64 = 12,         // PROCESSOR_ARCHITECTURE_ARM64 (Windows 10+)
							/// <summary>未知架构</summary>
		Unknown = 0xFFFF,   // PROCESSOR_ARCHITECTURE_UNKNOWN
		/// <summary>中性（Any CPU），仅用于表示通用组件，不作为运行时检测返回值。</summary>
		Neutral = 0x11    // 自定义值，不与任何实际架构冲突
	}
	/// <summary>
	/// 提供检测当前运行环境处理器架构的功能。
	/// </summary>
	public static class ProcessorDetector
	{
		[StructLayout (LayoutKind.Sequential)]
		private struct SYSTEM_INFO
		{
			public ushort wProcessorArchitecture;
			public ushort wReserved;
			public uint dwPageSize;
			public IntPtr lpMinimumApplicationAddress;
			public IntPtr lpMaximumApplicationAddress;
			public IntPtr dwActiveProcessorMask;
			public uint dwNumberOfProcessors;
			public uint dwProcessorType;
			public uint dwAllocationGranularity;
			public ushort wProcessorLevel;
			public ushort wProcessorRevision;
		}
		[DllImport ("kernel32.dll", SetLastError = true)]
		private static extern void GetNativeSystemInfo (out SYSTEM_INFO lpSystemInfo);

		/// <summary>
		/// 获取当前运行环境的原生处理器架构。
		/// </summary>
		/// <returns>对应的 <see cref="ProcessorArchitecture"/> 枚举值。</returns>
		public static ProcessorArchitecture GetCurrentArchitecture ()
		{
			SYSTEM_INFO si;
			GetNativeSystemInfo (out si);
			return (ProcessorArchitecture)si.wProcessorArchitecture;
		}
	}
}
