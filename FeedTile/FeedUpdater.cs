using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace WindowsModern.FeedTile
{
	internal static class FeedUpdater
	{
		private const int HttpTimeoutMs = 15000;
		private const string UserAgent = "Mozilla/5.0 (compatible; WindowsModern.NewsFeedTile/1.0)";
		private static int _securityProtocolInited = 0;

		/// <summary>初始化安全协议。使用 try/catch，兼容 .NET 4.0 没有 Tls12/Tls13 枚举的情况。</summary>
		public static void EnsureSecurityProtocol ()
		{
			if (Interlocked.Exchange (ref _securityProtocolInited, 1) != 0) return;
			try { ServicePointManager.SecurityProtocol &= ~SecurityProtocolType.Ssl3; } catch { }
			try { ServicePointManager.SecurityProtocol |= (SecurityProtocolType)768; } catch { }
			try { ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072; } catch { }
			try { ServicePointManager.SecurityProtocol |= (SecurityProtocolType)12288; } catch { }
		}

		/// <summary>下载 feed 内容，带 ETag/Last-Modified 条件请求和本地 XML 缓存。失败时回退到本地缓存。</summary>
		public static string DownloadWithCache (string url, string cacheDir)
		{
			if (string.IsNullOrWhiteSpace (url)) return null;
			EnsureSecurityProtocol ();
			FeedHttpCacheEntry cache = LoadCache (cacheDir, url);
			try
			{
				var req = (HttpWebRequest)WebRequest.Create (url);
				req.Timeout = HttpTimeoutMs;
				req.ReadWriteTimeout = HttpTimeoutMs;
				req.UserAgent = UserAgent;
				req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
				req.AllowAutoRedirect = true;
				req.MaximumAutomaticRedirections = 5;
				if (cache != null)
				{
					if (!string.IsNullOrEmpty (cache.ETag))
					{
						try { req.Headers [HttpRequestHeader.IfNoneMatch] = cache.ETag; } catch { }
					}
					if (!string.IsNullOrEmpty (cache.LastModified))
					{
						try { req.Headers [HttpRequestHeader.IfModifiedSince] = cache.LastModified; } catch { }
					}
				}
				using (var resp = (HttpWebResponse)req.GetResponse ())
				{
					string content;
					using (var stream = resp.GetResponseStream ())
					using (var reader = new StreamReader (stream, Encoding.UTF8))
						content = reader.ReadToEnd ();
					var entry = new FeedHttpCacheEntry {
						Url = url,
						ETag = resp.Headers ["ETag"],
						LastModified = resp.Headers ["Last-Modified"],
						Content = content
					};
					SaveCache (cacheDir, url, entry);
					return content;
				}
			}
			catch (WebException we)
			{
				var resp = we.Response as HttpWebResponse;
				if (resp != null)
				{
					try
					{
						if (resp.StatusCode == HttpStatusCode.NotModified) return cache?.Content;
					}
					finally { resp.Close (); }
				}
				return cache?.Content;
			}
			catch
			{
				return cache?.Content;
			}
		}

		/// <summary>解析 RSS 2.0 / RSS 1.0(RDF) / Atom 内容。</summary>
		public static List<FeedItemData> ParseFeed (string xml, FeedData parent, int maxItems = 50)
		{
			var result = new List<FeedItemData> ();
			if (string.IsNullOrWhiteSpace (xml) || parent == null) return result;
			try
			{
				var doc = XDocument.Parse (xml, LoadOptions.None);
				var root = doc.Root;
				if (root == null) return result;
				string rootName = root.Name.LocalName ?? "";
				if (rootName.Equals ("rss", StringComparison.OrdinalIgnoreCase))
				{
					var channel = root.Element (root.Name.Namespace + "channel") ?? root.Element ("channel");
					if (channel != null)
					{
						foreach (var item in channel.Elements (root.Name.Namespace + "item"))
						{
							result.Add (ParseRssItem (item, parent));
							if (result.Count >= maxItems) break;
						}
					}
				}
				else if (rootName.Equals ("RDF", StringComparison.OrdinalIgnoreCase))
				{
					XNamespace rss = "http://purl.org/rss/1.0/";
					foreach (var item in root.Elements (rss + "item"))
					{
						result.Add (ParseRssItem (item, parent, rss));
						if (result.Count >= maxItems) break;
					}
				}
				else if (rootName.Equals ("feed", StringComparison.OrdinalIgnoreCase))
				{
					XNamespace atom = "http://www.w3.org/2005/Atom";
					foreach (var entry in root.Elements (atom + "entry"))
					{
						result.Add (ParseAtomEntry (entry, parent));
						if (result.Count >= maxItems) break;
					}
				}
			}
			catch { }
			return result;
		}

		private static FeedItemData ParseRssItem (XElement item, FeedData parent, XNamespace ns = null)
		{
			XNamespace n = ns ?? item.Name.Namespace;
			string title = (string)item.Element (n + "title") ?? "";
			string desc = (string)item.Element (n + "description") ?? "";
			if (string.IsNullOrEmpty (desc))
				desc = (string)item.Element (XName.Get ("encoded", "http://purl.org/rss/1.0/modules/content/")) ?? "";
			string link = (string)item.Element (n + "link") ?? "";
			string guid = (string)item.Element (n + "guid") ?? "";
			if (string.IsNullOrEmpty (link)) link = guid;
			DateTime pubDate = DateTime.MinValue;
			string pubStr = (string)item.Element (n + "pubDate") ?? "";
			if (!string.IsNullOrEmpty (pubStr))
				DateTime.TryParse (pubStr, out pubDate);
			if (pubDate == DateTime.MinValue)
			{
				string dc = (string)item.Element (XName.Get ("date", "http://purl.org/dc/elements/1.1/")) ?? "";
				if (!string.IsNullOrEmpty (dc)) DateTime.TryParse (dc, out pubDate);
			}
			var data = new FeedItemData {
				Title = StripHtml (title).Trim (),
				Description = desc ?? "",
				Link = link ?? "",
				IsRead = false,
				PublishDate = pubDate,
				Parent = parent
			};
			data.LocalId = MakeLocalId (parent.Path, data.Link, pubDate);
			return data;
		}

		private static FeedItemData ParseAtomEntry (XElement entry, FeedData parent)
		{
			XNamespace atom = "http://www.w3.org/2005/Atom";
			string title = (string)entry.Element (atom + "title") ?? "";
			string summary = (string)entry.Element (atom + "summary") ?? "";
			if (string.IsNullOrEmpty (summary))
				summary = (string)entry.Element (atom + "content") ?? "";
			string link = "";
			XElement linkEl = null;
			foreach (var l in entry.Elements (atom + "link"))
			{
				string rel = (string)l.Attribute ("rel");
				if (string.IsNullOrEmpty (rel) || string.Equals (rel, "alternate", StringComparison.OrdinalIgnoreCase))
				{
					linkEl = l;
					break;
				}
				if (linkEl == null) linkEl = l;
			}
			if (linkEl != null) link = (string)linkEl.Attribute ("href") ?? "";
			if (string.IsNullOrEmpty (link)) link = (string)entry.Element (atom + "id") ?? "";
			DateTime pubDate = DateTime.MinValue;
			string pubStr = (string)entry.Element (atom + "published") ?? (string)entry.Element (atom + "updated") ?? "";
			if (!string.IsNullOrEmpty (pubStr)) DateTime.TryParse (pubStr, out pubDate);
			var data = new FeedItemData {
				Title = StripHtml (title).Trim (),
				Description = summary ?? "",
				Link = link ?? "",
				IsRead = false,
				PublishDate = pubDate,
				Parent = parent
			};
			data.LocalId = MakeLocalId (parent.Path, data.Link, pubDate);
			return data;
		}

		/// <summary>用 feedPath + link + pubDate 生成稳定 LocalId（own: 前缀，区别于 Feeds 的数字 ID）。</summary>
		public static string MakeLocalId (string feedPath, string link, DateTime pubDate)
		{
			try
			{
				string raw = (feedPath ?? "") + "|" + (link ?? "") + "|" + pubDate.ToString ("o");
				using (var sha = SHA1.Create ())
				{
					var hash = sha.ComputeHash (Encoding.UTF8.GetBytes (raw));
					var sb = new StringBuilder (24);
					sb.Append ("own:");
					for (int i = 0; i < 8; i++) sb.Append (hash [i].ToString ("x2"));
					return sb.ToString ();
				}
			}
			catch { return "own:0"; }
		}

		private static string StripHtml (string html)
		{
			if (string.IsNullOrEmpty (html)) return "";
			try
			{
				var doc = new HtmlAgilityPack.HtmlDocument ();
				doc.LoadHtml (html);
				return doc.DocumentNode.InnerText ?? "";
			}
			catch { return html; }
		}

		#region HTTP Cache
		public class FeedHttpCacheEntry
		{
			public string Url { get; set; }
			public string ETag { get; set; }
			public string LastModified { get; set; }
			public string Content { get; set; }
		}

		private static string GetCacheFilePath (string cacheDir, string url)
		{
			if (string.IsNullOrEmpty (cacheDir)) return null;
			try
			{
				using (var sha = SHA1.Create ())
				{
					var hash = sha.ComputeHash (Encoding.UTF8.GetBytes (url ?? ""));
					var sb = new StringBuilder (40);
					for (int i = 0; i < hash.Length; i++) sb.Append (hash [i].ToString ("x2"));
					return Path.Combine (cacheDir, sb.ToString () + ".json");
				}
			}
			catch { return null; }
		}

		private static FeedHttpCacheEntry LoadCache (string cacheDir, string url)
		{
			var path = GetCacheFilePath (cacheDir, url);
			if (path == null || !File.Exists (path)) return null;
			try
			{
				var json = File.ReadAllText (path);
				return JsonConvert.DeserializeObject<FeedHttpCacheEntry> (json);
			}
			catch { return null; }
		}

		private static void SaveCache (string cacheDir, string url, FeedHttpCacheEntry entry)
		{
			if (entry == null) return;
			var path = GetCacheFilePath (cacheDir, url);
			if (path == null) return;
			try
			{
				var dir = Path.GetDirectoryName (path);
				if (!Directory.Exists (dir)) Directory.CreateDirectory (dir);
				File.WriteAllText (path, JsonConvert.SerializeObject (entry));
			}
			catch { }
		}
		#endregion
	}
}