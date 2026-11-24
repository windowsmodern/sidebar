using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Sidebar
{
	[ComVisible (true)]
	public static class StringNormalize
	{
		public static bool NEmpty (this string str)
		{
			return string.IsNullOrWhiteSpace (str);
		}
		public static string NNormalize (this string str, bool upper = false)
		{
			if (str == null) return "";
			int start = 0, end = str.Length - 1;
			while (start <= end && char.IsWhiteSpace (str [start])) start++;
			while (end >= start && char.IsWhiteSpace (str [end])) end--;
			if (start > end) return "";
			if (start == 0 && end == str.Length - 1)
				return upper ? str.ToUpperInvariant () : str.ToLowerInvariant ();
			string trimmed = str.Substring (start, end - start + 1);
			return upper ? trimmed.ToUpperInvariant () : trimmed.ToLowerInvariant ();
		}
		public static bool NEquals (this string self, string another)
		{
			return NormalizedStringComparer.Instance.Equals (self, another);
		}
		public static int NCompareTo (this string self, string another)
		{
			return NormalizedStringComparer.Instance.Compare (self, another);
		}
	}
	public class NormalizedStringComparer: IEqualityComparer<string>, IComparer<string>
	{
		public static readonly NormalizedStringComparer Instance = new NormalizedStringComparer ();
		private static char LowerChar (char c)
		{
			if (c >= 'A' && c <= 'Z') return (char)(c + 32);
			if (c < 128) return c;
			return char.ToLowerInvariant (c);
		}
		private static void GetTrimmedRange (string s, out int start, out int end)
		{
			int len = s.Length;
			start = 0;
			end = len - 1;
			while (start <= end && char.IsWhiteSpace (s [start])) start++;
			while (end >= start && char.IsWhiteSpace (s [end])) end--;
		}
		public bool Equals (string x, string y)
		{
			if (ReferenceEquals (x, y)) return true;
			if (x == null || y == null) return false;

			int xs, xe, ys, ye;
			GetTrimmedRange (x, out xs, out xe);
			GetTrimmedRange (y, out ys, out ye);

			int xLen = xe - xs + 1;
			int yLen = ye - ys + 1;
			if (xLen != yLen) return false;

			for (int i = 0; i < xLen; i++)
			{
				char a = x [xs + i];
				char b = y [ys + i];
				if (a == b) continue;
				if (LowerChar (a) != LowerChar (b)) return false;
			}
			return true;
		}
		public int GetHashCode (string obj)
		{
			if (obj == null) return 0;

			int start, end;
			GetTrimmedRange (obj, out start, out end);
			if (start > end) return 0;

			unchecked
			{
				uint hash = 2166136261;
				for (int i = start; i <= end; i++)
				{
					uint c = LowerChar (obj [i]);
					hash = (hash ^ c) * 16777619;
				}
				hash ^= hash >> 16;
				hash *= 0x85ebca6b;
				hash ^= hash >> 13;
				return (int)hash;
			}
		}
		public int Compare (string x, string y)
		{
			if (ReferenceEquals (x, y)) return 0;
			if (x == null) return -1;
			if (y == null) return 1;

			int xs, xe, ys, ye;
			GetTrimmedRange (x, out xs, out xe);
			GetTrimmedRange (y, out ys, out ye);

			int xLen = xe - xs + 1;
			int yLen = ye - ys + 1;
			int min = xLen < yLen ? xLen : yLen;

			for (int i = 0; i < min; i++)
			{
				char a = LowerChar (x [xs + i]);
				char b = LowerChar (y [ys + i]);
				if (a != b) return a < b ? -1 : 1;
			}
			if (xLen == yLen) return 0;
			return xLen < yLen ? -1 : 1;
		}
	}
}