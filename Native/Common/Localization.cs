using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace Sidebar
{
	internal static class NativeMethods
	{
		public const int LOCALE_SSHORTESTSCRIPT = 0x0000004F; // 获取四字母脚本代码
		public const uint KLF_ACTIVATE = 0x00000001; // 激活键盘布局
													 // GetLocaleInfoW for LCID-based queries
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetLocaleInfoW (int Locale, int LCType, [Out] StringBuilder lpLCData, int cchData);
		// GetLocaleInfoEx for locale name based queries
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int GetLocaleInfoEx (string lpLocaleName, int LCType, [Out] StringBuilder lpLCData, int cchData);
		// LocaleNameToLCID - available on Vista+; fallback is to use CultureInfo
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int LocaleNameToLCID (string lpName, uint dwFlags);
		// LCIDToLocaleName (Vista+)
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		public static extern int LCIDToLocaleName (int Locale, [Out] StringBuilder lpName, int cchName, uint dwFlags);
		[DllImport ("user32.dll")]
		public static extern IntPtr GetKeyboardLayout (uint dwLayout);
		[DllImport ("user32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr LoadKeyboardLayout (string pwszKLID, uint Flags);
	}
	[ComVisible (true)]
	public static class Locale
	{
		// Current locale name like "en-US"
		public static string CurrentLocale => GetComputerLocaleCode ();
		// Current LCID (int)
		public static int CurrentLCID => new CultureInfo (CurrentLocale).LCID;
		// Convert LCID -> locale name (e.g. 1033 -> "en-US")
		public static string ToLocaleName (int lcid)
		{
			try
			{
				// Try managed first
				var ci = new CultureInfo (lcid);
				if (!string.IsNullOrEmpty (ci.Name))
					return ci.Name;
			}
			catch
			{
				// try Win32 LCIDToLocaleName (Vista+)
				try
				{
					StringBuilder sb = new StringBuilder (LOCALE_NAME_MAX_LENGTH);
					int res = NativeMethods.LCIDToLocaleName (lcid, sb, sb.Capacity, 0);
					if (res > 0) return sb.ToString ();
				}
				catch { }
			}
			return string.Empty;
		}
		// Convert locale name -> LCID
		public static int ToLCID (string localeName)
		{
			if (string.IsNullOrEmpty (localeName)) return CultureInfo.InvariantCulture.LCID;
			try
			{
				// prefer managed creation
				var ci = new CultureInfo (localeName);
				return ci.LCID;
			}
			catch
			{
				// try Win32 LocaleNameToLCID (Vista+)
				try
				{
					int lcid = NativeMethods.LocaleNameToLCID (localeName, 0);
					if (lcid != 0) return lcid;
				}
				catch { }
			}
			// fallback: invariant culture
			return CultureInfo.InvariantCulture.LCID;
		}
		// Return a locale info string for given LCID and LCTYPE. LCTYPE is the Win32 LOCALE_* constant.
		// Returns a string (or empty string on failure).
		public static object LocaleInfo (int lcid, int lctype)
		{
			try
			{
				// First try mapping common LCTYPE values to managed properties for better correctness
				// Some common LCTYPE values:
				// LOCALE_SISO639LANGNAME = 0x59 (89)  -> Two-letter ISO language name
				// LOCALE_SISO3166CTRYNAME = 0x5A (90) -> Two-letter country/region name
				// LOCALE_SNAME = 0x5c (92) -> locale name like "en-US" (Vista+)
				// But we cannot rely on all values, so we fallback to native GetLocaleInfoW.
				if (lctype == 0x59) // LOCALE_SISO639LANGNAME
				{
					try
					{
						var ci = new CultureInfo (lcid);
						return ci.TwoLetterISOLanguageName ?? string.Empty;
					}
					catch { }
				}
				else if (lctype == 0x5A) // LOCALE_SISO3166CTRYNAME
				{
					try
					{
						var ci = new CultureInfo (lcid);
						try
						{
							var ri = new RegionInfo (ci.Name);
							return ri.TwoLetterISORegionName ?? string.Empty;
						}
						catch
						{
							// some cultures have no region; fallback to parsing name
							var name = ci.Name;
							if (!string.IsNullOrEmpty (name) && name.IndexOf ('-') >= 0)
							{
								return name.Split ('-') [1];
							}
						}
					}
					catch { }
				}
				else if (lctype == 0x5c) // LOCALE_SNAME
				{
					try
					{
						var ci = new CultureInfo (lcid);
						return ci.Name ?? string.Empty;
					}
					catch { }
				}

				// Fallback to native GetLocaleInfoW
				StringBuilder sb = new StringBuilder (256);
				int ret = NativeMethods.GetLocaleInfoW (lcid, lctype, sb, sb.Capacity);
				if (ret > 0)
					return sb.ToString ();
				return string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}
		// LocaleInfoEx: query by locale name string and LCTYPE
		// Returns string if available; otherwise returns the integer result code (as int) if string empty (mimic C++ behavior).
		public static object LocaleInfoEx (string localeName, int lctype)
		{
			if (string.IsNullOrEmpty (localeName))
			{
				// fall back to current culture name
				localeName = CurrentLocale;
				if (string.IsNullOrEmpty (localeName)) return 0;
			}

			try
			{
				// Try managed shortcuts for common types
				if (lctype == 0x59) // LOCALE_SISO639LANGNAME
				{
					try
					{
						var ci = new CultureInfo (localeName);
						return ci.TwoLetterISOLanguageName ?? string.Empty;
					}
					catch { }
				}
				else if (lctype == 0x5A) // LOCALE_SISO3166CTRYNAME
				{
					try
					{
						var ci = new CultureInfo (localeName);
						var ri = new RegionInfo (ci.Name);
						return ri.TwoLetterISORegionName ?? string.Empty;
					}
					catch
					{
						// try to split
						var parts = localeName.Split (new char [] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
						if (parts.Length >= 2) return parts [1];
					}
				}
				else if (lctype == 0x5c) // LOCALE_SNAME
				{
					// localeName is probably already the name
					return localeName;
				}

				// Fallback to GetLocaleInfoEx
				StringBuilder sb = new StringBuilder (LOCALE_NAME_MAX_LENGTH);
				int res = NativeMethods.GetLocaleInfoEx (localeName, lctype, sb, sb.Capacity);
				if (res > 0)
				{
					string outStr = sb.ToString ();
					if (!string.IsNullOrEmpty (outStr))
						return outStr;
				}
				// if nothing returned, return the result code
				return res;
			}
			catch
			{
				return 0;
			}
		}
		// Helpers similar to the C++: restricted (language) and elaborated (region) codes
		public static string GetLocaleRestrictedCode (string localeName)
		{
			if (string.IsNullOrEmpty (localeName)) localeName = CurrentLocale;
			try
			{
				var ci = new CultureInfo (localeName);
				return ci.TwoLetterISOLanguageName ?? string.Empty;
			}
			catch
			{
				// fallback: parse name
				var parts = localeName.Split (new char [] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 1) return parts [0];
				return string.Empty;
			}
		}
		public static string GetLocaleElaboratedCode (string localeName)
		{
			if (string.IsNullOrEmpty (localeName)) localeName = CurrentLocale;
			try
			{
				var ci = new CultureInfo (localeName);
				// Region part from RegionInfo
				try
				{
					var ri = new RegionInfo (ci.Name);
					return ri.TwoLetterISORegionName ?? string.Empty;
				}
				catch
				{
					// fallback: parse
					var parts = localeName.Split (new char [] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
					if (parts.Length >= 2) return parts [1];
				}
			}
			catch
			{
				var parts = localeName.Split (new char [] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
				if (parts.Length >= 2) return parts [1];
			}
			return string.Empty;
		}
		// LCID -> combined code like "en-US" (with configurable separator)
		public static string LcidToLocaleCode (int lcid)
		{
			try
			{
				var ci = new CultureInfo (lcid);
				if (!string.IsNullOrEmpty (ci.Name)) return ci.Name;
			}
			catch
			{
				try
				{
					var name = ToLocaleName (lcid);
					if (!string.IsNullOrEmpty (name)) return name;
				}
				catch { }
			}
			return string.Empty;
		}
		// Get the user default locale name
		public static string GetUserDefaultLocaleName ()
		{
			try
			{
				// In .NET, CurrentCulture corresponds to user default
				string name = CultureInfo.CurrentCulture.Name;
				if (!string.IsNullOrEmpty (name)) return name;
			}
			catch { }
			return LcidToLocaleCode (CultureInfo.CurrentCulture.LCID);
		}
		// Get system default locale name (machine)
		public static string GetSystemDefaultLocaleName ()
		{
			try
			{
				// InstalledUICulture / Invariant fallback
				string name = CultureInfo.InstalledUICulture.Name;
				if (!string.IsNullOrEmpty (name)) return name;
			}
			catch { }
			return LcidToLocaleCode (CultureInfo.InstalledUICulture.LCID);
		}
		// Get computer locale code similar to C++ approach
		public static string GetComputerLocaleCode ()
		{
			try
			{
				// Thread culture -> user -> system
				string threadName = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
				if (!string.IsNullOrEmpty (threadName)) return threadName;

				string user = GetUserDefaultLocaleName ();
				if (!string.IsNullOrEmpty (user)) return user;

				string system = GetSystemDefaultLocaleName ();
				if (!string.IsNullOrEmpty (system)) return system;
			}
			catch { }
			// fallback to invariant
			return CultureInfo.InvariantCulture.Name ?? string.Empty;
		}
		public static List<string> RecommendLocaleNames
		{
			get
			{
				var arr = new string [] {
					System.Threading.Thread.CurrentThread.CurrentCulture.Name,
					GetUserDefaultLocaleName (),
					GetSystemDefaultLocaleName (),
					LcidToLocaleCode (CurrentLCID),
					GetLocaleRestrictedCode (System.Threading.Thread.CurrentThread.CurrentCulture.Name),
					GetLocaleRestrictedCode (GetUserDefaultLocaleName ()),
					GetLocaleRestrictedCode (GetSystemDefaultLocaleName ()),
					"en-US",
					"en"
				};
				var list = new List<string> ();
				foreach (var loc in arr)
				{
					var lloc = loc.Trim ().ToLowerInvariant ();
					var isfind = false;
					foreach (var item in list)
					{
						var str = item as string;
						if (string.IsNullOrWhiteSpace (str)) isfind = true;
						if (isfind) break;
						isfind = str.Trim ().ToLowerInvariant () == lloc;
						if (isfind) break;
					}
					if (!isfind) list.Add (loc);
				}
				return list;
			}
		}
		// Compare two locale names; returns true if equal by name or LCID
		public static bool LocaleNameCompare (string left, string right)
		{
			if (string.Equals (left, right, StringComparison.OrdinalIgnoreCase)) return true;
			try
			{
				int l = ToLCID (left);
				int r = ToLCID (right);
				return l == r && l != CultureInfo.InvariantCulture.LCID;
			}
			catch
			{
				return false;
			}
		}
		// Constants
		private const int LOCALE_NAME_MAX_LENGTH = 85; // defined by Windows
		public static string CurrentInputMethodLanguageTag
		{
			get
			{
				IntPtr hkl = NativeMethods.GetKeyboardLayout (0);
				int lcid = hkl.ToInt32 () & 0xFFFF;

				StringBuilder sb = new StringBuilder (85);
				int result = NativeMethods.LCIDToLocaleName (lcid, sb, sb.Capacity, 0);
				if (result > 0)
					return sb.ToString ();

				return null;
			}
		}
		public static bool IsWellFormed (string languageTag)
		{
			if (string.IsNullOrEmpty (languageTag))
				return false;

			try
			{
				var _ = new CultureInfo (languageTag);
				return true;
			}
			catch
			{
				return false;
			}
		}
		public static List<string> GetMuiCompatibleLanguageListFromLanguageTags (IEnumerable<string> languageTags)
		{
			var result = new List<string> ();
			foreach (string tag in languageTags)
			{
				if (string.IsNullOrEmpty (tag))
					continue;
				result.Add (tag);
				try
				{
					var ci = new CultureInfo (tag);
					string parent = ci.Parent.Name;
					if (!string.IsNullOrEmpty (parent) && parent != tag && !result.Contains (parent))
						result.Add (parent);
				}
				catch { }
				string neutral = Regex.Replace (tag, @"-.*$", "");
				if (neutral != tag && !result.Contains (neutral))
					result.Add (neutral);
			}
			if (!result.Contains ("neutral"))
				result.Add ("neutral");
			return result;
		}
	}
}
