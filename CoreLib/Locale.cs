using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Linq;
namespace Sidebar
{
	public class LocaleResource: ConcurrentDictionary<string, string>, ILocaleResource
	{
		public LocaleResource () : base (StringComparer.OrdinalIgnoreCase) { }
		internal string GetRequiredValue (IDictionary<string, string> dict, string localeName)
		{
			var ln = localeName?.Trim ()?.ToLowerInvariant () ?? "";
			var lid = Locale.ToLCID (ln);
			var ret = "";
			if (dict.TryGetValue (ln, out ret)) return ret;
			foreach (var kv in dict)
			{
				if ((kv.Key?.Trim ()?.ToLowerInvariant () ?? "") == ln) return kv.Value;
			}
			foreach (var kv in dict)
			{
				if (Locale.ToLCID (kv.Key) == lid) return kv.Value;
			}
			var restrict = Locale.GetLocaleRestrictedCode (ln)?.Trim ()?.ToLowerInvariant () ?? "";
			foreach (var kv in dict)
			{
				var kr = Locale.GetLocaleRestrictedCode (kv.Key)?.Trim ()?.ToLowerInvariant () ?? "";
				if (kr == restrict) return kv.Value;
			}
			var rid = Locale.ToLCID (restrict);
			foreach (var kv in dict)
			{
				var kr = Locale.GetLocaleRestrictedCode (kv.Key)?.Trim ()?.ToLowerInvariant () ?? "";
				var krid = Locale.ToLCID (kr);
				if (krid == rid) return kv.Value;
			}
			return "";
		}
		public string SuitableValue (string fallback = null, string localeName = null)
		{
			if (string.IsNullOrWhiteSpace (localeName)) localeName = Locale.GetComputerLocaleCode ();
			var ret = "";
			ret = GetRequiredValue (this, localeName);
			if (string.IsNullOrEmpty (ret))
			{
				if (localeName?.Trim ()?.ToLowerInvariant () != Locale.GetComputerLocaleCode ()?.Trim ()?.ToLowerInvariant ())
				{
					ret = GetRequiredValue (this, Locale.GetComputerLocaleCode ());
				}
			}
			if (string.IsNullOrEmpty (ret)) ret = GetRequiredValue (this, "en-US");
			if (string.IsNullOrEmpty (ret))
			{
				foreach (var kv in this)
				{
					ret = kv.Value;
					break;
				}
			}
			if (string.IsNullOrEmpty (ret)) ret = null;
			return ret ?? fallback;
		}
		public override string ToString ()
		{
			try { return SuitableValue (); } catch { return ""; }
		}
		public void CleanRedundantValues (string localeName = null)
		{
			if (Count <= 2) return;
			var list = LocaleResources.RecommendRemainLanguages;
			if (!string.IsNullOrWhiteSpace (localeName) && !list.Contains (localeName))
			{
				var formatted = localeName.Trim ();
				var isfind = false;
				foreach (var i in list)
				{
					if (i.NEquals (formatted))
					{
						isfind = true;
						break;
					}
				}
				if (isfind == true)
				{
					var newer = new List<string> (list);
					newer.Add (formatted);
					list = newer;
				}
			}
			var remainlist = new List<string> ();
			foreach (var kv in this)
			{
				foreach (var i in list)
				{
					if (kv.Key.NEquals (i))
					{
						remainlist.Add (kv.Key);
						break;
					}
					if (Locale.ToLCID (kv.Key) == Locale.ToLCID (i))
					{
						remainlist.Add (kv.Key);
						break;
					}
					if (Locale.GetLocaleRestrictedCode (kv.Key).NEquals (Locale.GetLocaleRestrictedCode (i)))
					{
						remainlist.Add (kv.Key);
						break;
					}
					if (Locale.ToLCID (Locale.GetLocaleRestrictedCode (kv.Key)) == Locale.ToLCID (Locale.GetLocaleRestrictedCode (i)))
					{
						remainlist.Add (kv.Key);
						break;
					}
				}
			}
			var deletelist = new List<string> ();
			foreach (var kv in this)
			{
				if (!remainlist.Contains (kv.Key)) deletelist.Add (kv.Key);
			}
			foreach (var i in deletelist)
			{
				string _ = null;
				if (ContainsKey (i)) TryRemove (i, out _);
				if (Count <= 2) return;
			}
		}
	}
	public class LocaleResources: ConcurrentDictionary<string, ILocaleResource>, ILocaleResources
	{
		public LocaleResources () : base (StringComparer.OrdinalIgnoreCase) { }
		public IDictionary<string, ILocaleResource> AllResources => this;
		public ILocaleResource AllValues (string resourceName) => this [resourceName];
		public string SuitableResource (string resName, string fallback = null, string localeName = null)
		{
			try { return this [resName]?.SuitableValue (fallback, localeName) ?? fallback; }
			catch { return fallback; }
		}
		public static LocaleResources CreateFromXml (XmlDocument doc)
		{
			var ret = new LocaleResources ();
			var root = doc.DocumentElement;
			var children = root.ChildNodes;
			foreach (XmlNode xn in children)
			{
				var lr = new LocaleResource ();
				var locales = xn.ChildNodes;
				foreach (XmlNode xnl in locales)
				{
					lr [xnl.Attributes ["name"].Value] = xnl.InnerText;
				}
				ret [xn.Attributes ["id"].Value] = lr;
			}
			return ret;
		}
		public static LocaleResources CreateFromFile (string filepath)
		{
			using (var f = File.Open (filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				var doc = new XmlDocument ();
				doc.Load (f);
				return CreateFromXml (doc);
			}
		}
		public static LocaleResources CreateFromUrl (string filename)
		{
			var doc = new XmlDocument ();
			doc.Load (filename);
			return CreateFromXml (doc);
		}
		public void CleanRedundantValues (string localeName = null)
		{
			foreach (var kv in this)
			{
				kv.Value?.CleanRedundantValues ();
			}
		}
		private static List<string> rrl = null;
		internal static IEnumerable<string> RecommendRemainLanguages
		{
			get
			{
				if (rrl == null)
				{
					rrl = Locale.RecommendLocaleNames;
				}
				return rrl;
			}
		}
	}
}
