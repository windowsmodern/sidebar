using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Win32.Init
{
	internal static class IniFile
	{
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern uint GetPrivateProfileStringA (string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint GetPrivateProfileStringW (string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern uint GetPrivateProfileSectionA (string lpAppName, byte [] lpReturnedString, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint GetPrivateProfileSectionW (string lpAppName, char [] lpReturnedString, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern uint GetPrivateProfileSectionNamesA (byte [] lpszReturnBuffer, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint GetPrivateProfileSectionNamesW (char [] lpszReturnBuffer, uint nSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern uint GetPrivateProfileIntA (string lpAppName, string lpKeyName, int nDefault, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern uint GetPrivateProfileIntW (string lpAppName, string lpKeyName, int nDefault, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern bool WritePrivateProfileStringA (string lpAppName, string lpKeyName, string lpString, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern bool WritePrivateProfileStringW (string lpAppName, string lpKeyName, string lpString, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern bool WritePrivateProfileSectionA (string lpAppName, string lpString, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern bool WritePrivateProfileSectionW (string lpAppName, string lpString, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern bool GetPrivateProfileStructA (string lpAppName, string lpKeyName, IntPtr lpStruct, uint uSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern bool GetPrivateProfileStructW (string lpAppName, string lpKeyName, IntPtr lpStruct, uint uSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Ansi)]
		private static extern bool WritePrivateProfileStructA (string lpAppName, string lpKeyName, IntPtr lpStruct, uint uSize, string lpFileName);
		[DllImport ("kernel32.dll", CharSet = CharSet.Unicode)]
		private static extern bool WritePrivateProfileStructW (string lpAppName, string lpKeyName, IntPtr lpStruct, uint uSize, string lpFileName);

		public static string GetPrivateProfileStringA (string filePath, string section, string key, string defaultValue = "")
		{
			StringBuilder buf = new StringBuilder (32768);
			GetPrivateProfileStringA (section, key, defaultValue, buf, (uint)buf.Capacity, filePath);
			return buf.ToString ();
		}
		public static string GetPrivateProfileStringW (string filePath, string section, string key, string defaultValue = "")
		{
			StringBuilder buf = new StringBuilder (32768);
			GetPrivateProfileStringW (section, key, defaultValue, buf, (uint)buf.Capacity, filePath);
			return buf.ToString ();
		}
		public static uint GetPrivateProfileIntA (string filePath, string section, string key, int defaultValue = 0)
		{
			return GetPrivateProfileIntA (section, key, defaultValue, filePath);
		}
		public static uint GetPrivateProfileIntW (string filePath, string section, string key, int defaultValue = 0)
		{
			return GetPrivateProfileIntW (section, key, defaultValue, filePath);
		}
		public static bool WritePrivateProfileString (string filePath, string section, string key, string value)
		{
			return WritePrivateProfileStringW (section, key, value, filePath);
		}
		public static int GetPrivateProfileSectionA (string filePath, string section, List<string> output)
		{
			byte [] buf = new byte [32768];
			uint len = GetPrivateProfileSectionA (section, buf, (uint)buf.Length, filePath);
			output.Clear ();
			if (len == 0) return 0;
			int i = 0;
			while (i < len)
			{
				int start = i;
				while (i < len && buf [i] != 0) i++;
				if (i > start) output.Add (Encoding.Default.GetString (buf, start, i - start));
				i++;
			}
			return output.Count;
		}
		public static int GetPrivateProfileSectionW (string filePath, string section, List<string> output)
		{
			char [] buf = new char [32768];
			uint len = GetPrivateProfileSectionW (section, buf, (uint)buf.Length, filePath);
			output.Clear ();
			if (len == 0) return 0;
			int i = 0;
			while (i < len)
			{
				int start = i;
				while (i < len && buf [i] != '\0') i++;
				if (i > start) output.Add (new string (buf, start, i - start));
				i++;
			}
			return output.Count;
		}
		public static int GetPrivateProfileSectionNamesA (string filePath, List<string> output)
		{
			byte [] buf = new byte [32768];
			uint len = GetPrivateProfileSectionNamesA (buf, (uint)buf.Length, filePath);
			output.Clear ();
			if (len == 0) return 0;
			int i = 0;
			while (i < len)
			{
				int start = i;
				while (i < len && buf [i] != 0) i++;
				if (i > start) output.Add (Encoding.Default.GetString (buf, start, i - start));
				i++;
			}
			return output.Count;
		}
		public static int GetPrivateProfileSectionNamesW (string filePath, List<string> output)
		{
			char [] buf = new char [32768];
			uint len = GetPrivateProfileSectionNamesW (buf, (uint)buf.Length, filePath);
			output.Clear ();
			if (len == 0) return 0;
			int i = 0;
			while (i < len)
			{
				int start = i;
				while (i < len && buf [i] != '\0') i++;
				if (i > start) output.Add (new string (buf, start, i - start));
				i++;
			}
			return output.Count;
		}
		public static bool WritePrivateProfileSectionA (string filePath, string section, List<string> lines)
		{
			string buf = string.Join ("\0", lines) + "\0\0";
			return WritePrivateProfileSectionA (section, buf, filePath);
		}
		public static bool WritePrivateProfileSectionW (string filePath, string section, List<string> lines)
		{
			string buf = string.Join ("\0", lines) + "\0\0";
			return WritePrivateProfileSectionW (section, buf, filePath);
		}
		public static bool GetPrivateProfileStructA (string filePath, string section, string key, IntPtr output, uint size)
		{
			return GetPrivateProfileStructA (section, key, output, size, filePath);
		}
		public static bool GetPrivateProfileStructW (string filePath, string section, string key, IntPtr output, uint size)
		{
			return GetPrivateProfileStructW (section, key, output, size, filePath);
		}
		public static bool WritePrivateProfileStructA (string filePath, string section, string key, IntPtr data, uint size)
		{
			return WritePrivateProfileStructA (section, key, data, size, filePath);
		}
		public static bool WritePrivateProfileStructW (string filePath, string section, string key, IntPtr data, uint size)
		{
			return WritePrivateProfileStructW (section, key, data, size, filePath);
		}
		public static int GetPrivateProfileKeysA (string filePath, string section, List<string> keys)
		{
			List<string> lines = new List<string> ();
			int count = GetPrivateProfileSectionA (filePath, section, lines);
			keys.Clear ();
			foreach (var line in lines)
			{
				int pos = line.IndexOf ('=');
				if (pos != -1) keys.Add (line.Substring (0, pos));
			}
			return keys.Count;
		}
		public static int GetPrivateProfileKeysW (string filePath, string section, List<string> keys)
		{
			List<string> lines = new List<string> ();
			int count = GetPrivateProfileSectionW (filePath, section, lines);
			keys.Clear ();
			foreach (var line in lines)
			{
				int pos = line.IndexOf ('=');
				if (pos != -1) keys.Add (line.Substring (0, pos));
			}
			return keys.Count;
		}
		public static bool DeletePrivateProfileKeyA (string filePath, string section, string key)
		{
			return WritePrivateProfileStringA (section, key, null, filePath);
		}
		public static bool DeletePrivateProfileKeyW (string filePath, string section, string key)
		{
			return WritePrivateProfileStringW (section, key, null, filePath);
		}
		public static bool DeletePrivateProfileSectionA (string filePath, string section)
		{
			return WritePrivateProfileStringA (section, null, null, filePath);
		}
		public static bool DeletePrivateProfileSectionW (string filePath, string section)
		{
			return WritePrivateProfileStringW (section, null, null, filePath);
		}
	}
	internal static class BoolHelper
	{
		public static readonly string [] trueValue =
		{
			"true", "zhen", "yes", "真", "1"
		};
		public static readonly string [] falseValue =
		{
			"false", "jia", "no", "假", "0"
		};
		public static bool ConvertToBool (string str)
		{
			if (str == null) throw new ArgumentNullException (nameof (str));
			str = str.Trim ().ToLowerInvariant ();
			if (trueValue.Any (s => s.ToLowerInvariant () == str)) return true;
			if (falseValue.Any (s => s.ToLowerInvariant () == str)) return false;
			throw new FormatException ($"Cannot convert '{str}' to bool.");
		}
		public static bool TryParseBool (string str, out bool result)
		{
			result = false;
			if (str == null) return false;
			str = str.Trim ().ToLowerInvariant ();
			if (trueValue.Any (s => s.ToLowerInvariant () == str))
			{
				result = true;
				return true;
			}
			if (falseValue.Any (s => s.ToLowerInvariant () == str))
			{
				result = false;
				return true;
			}
			return false;
		}
	}
	[ComVisible (true)]
	public class RefString
	{
		public string Value { get; set; }
		public RefString (string value) { Value = value; }
		public RefString (ref string value) { Value = value; }
		public static RefString operator + (RefString a, RefString b)
		{
			if (ReferenceEquals (a, null)) return b;
			if (ReferenceEquals (b, null)) return a;
			return new RefString (a.Value + b.Value);
		}

		public static RefString operator + (RefString a, string b)
		{
			if (ReferenceEquals (a, null)) return new RefString (b);
			return new RefString (a.Value + b);
		}
		public static RefString operator + (string a, RefString b)
		{
			if (ReferenceEquals (b, null)) return new RefString (a);
			return new RefString (a + b.Value);
		}
		public static implicit operator RefString (string v) => new RefString (v);
		public static implicit operator string (RefString r) => r?.Value;
		public static bool operator == (RefString a, RefString b)
		{
			if (ReferenceEquals (a, b)) return true;
			if (ReferenceEquals (a, null) || ReferenceEquals (b, null)) return false;
			return a.Value == b.Value;
		}
		public static bool operator != (RefString a, RefString b) => !(a == b);

		public static bool operator == (RefString a, string b)
		{
			if (ReferenceEquals (a, null)) return b == null;
			return a.Value == b;
		}

		public static bool operator != (RefString a, string b) => !(a == b);
		public override bool Equals (object obj)
		{
			if (obj is RefString) return this == (RefString)obj;
			if (obj is string) return this == (string)obj;
			return false;
		}
		public override int GetHashCode () => Value?.GetHashCode () ?? 0;
		public override string ToString () => Value;
	}
	[ComVisible (true)]
	public class InitKey
	{
		private RefString filepath = "";
		private RefString section = "";
		private RefString key = "";
		public string FilePath => filepath.Value;
		public string Section => section.Value;
		public string Key { get { return key; } set { key.Value = value; } }
		public InitKey (RefString _fp, RefString _sect, RefString _key)
		{
			filepath = _fp;
			section = _sect;
			key = _key;
		}
		public string ReadString (string dflt = "") { return IniFile.GetPrivateProfileStringW (filepath, section, key, dflt); }
		private T ReadTo<T> (T dflt, Func<string, T> convTo)
		{
			string str = ReadString (dflt?.ToString () ?? "");
			try { return convTo (str); }
			catch { return dflt; }
		}
		public short ReadShort (short dflt = 0) { return ReadTo (dflt, Convert.ToInt16); }
		public ushort ReadUShort (ushort dflt = 0) { return ReadTo (dflt, Convert.ToUInt16); }
		public int ReadInt (int dflt = 0) { return ReadTo (dflt, Convert.ToInt32); }
		public uint ReadUInt (uint dflt = 0) { return ReadTo (dflt, Convert.ToUInt32); }
		public long ReadLong (long dflt = 0) { return ReadTo (dflt, Convert.ToInt64); }
		public ulong ReadULong (ulong dflt = 0) { return ReadTo (dflt, Convert.ToUInt64); }
		public Int16 ReadInt16 (Int16 dflt = 0) { return ReadTo (dflt, Convert.ToInt16); }
		public UInt16 ReadUInt16 (UInt16 dflt = 0) { return ReadTo (dflt, Convert.ToUInt16); }
		public Int32 ReadInt32 (Int32 dflt = 0) { return ReadTo (dflt, Convert.ToInt32); }
		public UInt32 ReadUInt32 (UInt32 dflt = 0) { return ReadTo (dflt, Convert.ToUInt32); }
		public Int64 ReadInt64 (Int64 dflt = 0) { return ReadTo (dflt, Convert.ToInt64); }
		public UInt64 ReadUInt64 (UInt64 dflt = 0) { return ReadTo (dflt, Convert.ToUInt64); }
		public bool ReadBool (bool dflt = false) { return ReadTo (dflt, BoolHelper.ConvertToBool); }
		public float ReadFloat (float dflt = 0) { return ReadTo (dflt, Convert.ToSingle); }
		public double ReadDouble (double dflt = 0) { return ReadTo (dflt, Convert.ToDouble); }
		public decimal ReadDecimal (decimal dflt = 0) { return ReadTo (dflt, Convert.ToDecimal); }
		public sbyte ReadInt8 (sbyte dflt = 0) { return ReadTo (dflt, Convert.ToSByte); }
		public byte ReadUInt8 (byte dflt = 0) { return ReadTo (dflt, Convert.ToByte); }
		public byte ReadByte (byte dflt = 0) { return ReadTo (dflt, Convert.ToByte); }
		public sbyte ReadSByte (sbyte dflt = 0) { return ReadTo (dflt, Convert.ToSByte); }
		public DateTime ReadDateTime (DateTime dflt = default (DateTime)) { return ReadTo (dflt, Convert.ToDateTime); }
		public object Get (object dflt) { return ReadString (dflt?.ToString () ?? ""); }
		public object Get () { return ReadString (); }
		public bool WriteString (string value) { return IniFile.WritePrivateProfileString (filepath, section, key, value); }
		public bool Write (string value) { return WriteString (value); }
		private bool WriteTo<T> (T value) { return WriteString (value?.ToString ()); }
		public bool Write (byte value) { return WriteTo (value); }
		public bool Write (sbyte value) { return WriteTo (value); }
		public bool Write (short value) { return WriteTo (value); }
		public bool Write (ushort value) { return WriteTo (value); }
		public bool Write (int value) { return WriteTo (value); }
		public bool Write (uint value) { return WriteTo (value); }
		public bool Write (long value) { return WriteTo (value); }
		public bool Write (ulong value) { return WriteTo (value); }
		public bool Write (bool value) { return WriteTo (value); }
		public bool Write (float value) { return WriteTo (value); }
		public bool Write (double value) { return WriteTo (value); }
		public bool Write (decimal value) { return WriteTo (value); }
		public bool Write (DateTime value) { return WriteTo (value); }
		public bool Set (object value) { return Write (value?.ToString ()); }
		public object Value { get { return Get (); } set { Set (value); } }
		public bool KeyExists
		{
			get
			{
				string val = IniFile.GetPrivateProfileStringW (filepath, section, key, null);
				return val != null;
			}
		}
		public bool ValueExists
		{
			get
			{
				if (!KeyExists) return false;
				string val = IniFile.GetPrivateProfileStringW (filepath, section, key, "");
				return !string.IsNullOrEmpty (val);
			}
		}
		public bool DeleteKey () => IniFile.WritePrivateProfileString (filepath, section, key, null);
		public bool Clear () => DeleteKey ();
	}
	[ComVisible (true)]
	public class InitSection
	{
		private RefString filepath = "";
		private RefString section = "";
		public string FilePath => filepath.Value;
		public string Section { get { return section; } set { section.Value = value; } }
		public InitSection (RefString _fp, RefString _sect)
		{
			filepath = _fp;
			section = _sect;
		}
		public InitKey GetKey (string key) => new InitKey (filepath, section, key);
		public object Get (string key, object dflt = null) => GetKey (key).Get (dflt);
		public object Get (string key) => GetKey (key).Get ();
		public bool Set (string key, object value) => GetKey (key).Set (value);
		public object this [string key]
		{
			get { return Get (key); }
			set { Set (key, value); }
		}
		public string [] GetAllKeys ()
		{
			var keys = new System.Collections.Generic.List<string> ();
			IniFile.GetPrivateProfileKeysW (filepath, section, keys);
			return keys.ToArray ();
		}
		public InitKey [] Keys
		{
			get { return GetAllKeys ().Select (k => new InitKey (filepath, section, k)).ToArray (); }
		}
		public bool DeleteSection () => IniFile.WritePrivateProfileString (filepath, section, null, null);
		public bool Clear () => DeleteSection ();
	}
	[ComVisible (true)]
	public class InitConfig
	{
		private RefString filepath = "";
		public string FilePath { get { return filepath.Value; } set { filepath.Value = value; } }
		public InitConfig (string _fp = "") { FilePath = _fp; }
		public InitSection GetSection (string section) => new InitSection (filepath, section);
		public InitKey GetKey (string section, string key) => new InitKey (filepath, section, key);
		public object Get (string section, string key, object dflt) => GetKey (section, key).Get (dflt);
		public object Get (string section, string key) => GetKey (section, key).Get ();
		public bool Set (string section, string key, object value) => GetKey (section, key).Set (value);
		public InitSection this [string key] => GetSection (key);
		public string [] GetAllSections ()
		{
			var sections = new System.Collections.Generic.List<string> ();
			IniFile.GetPrivateProfileSectionNamesW (filepath, sections);
			return sections.ToArray ();
		}
		public InitSection [] Sections => GetAllSections ().Select (s => new InitSection (filepath, s)).ToArray ();
	}
}