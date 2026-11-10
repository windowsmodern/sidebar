using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using AnyDiff;

namespace Sidebar
{
	public class Theme: ITheme
	{
		private const string KeyDisplayName = "ThemeDisplayName";
		private const string KeyPublisherDisplayName = "ThemePublisherDisplayName";
		private const string KeyDescription = "ThemeDescription";
		private const string KeyLogo = "ThemeLogo";
		private const string LocaleFileName = "Locale.xml";
		private const string PathFileName = "Path.xml";
		public string FolderPath { get; private set; }
		public string ThemeMainFile { get { return Path.Combine (FolderPath, "Theme.xaml"); } }
		public string ThemeName { get; private set; }
		public ResourceDictionary ResourceDictionary { get; private set; }
		public string DisplayName { get; private set; }
		public string PublisherDisplayName { get; private set; }
		public string Description { get; private set; }
		public string Logo { get; private set; }
		public ILocaleResources StringResources { get; private set; }
		public IPathResources FileResources { get; private set; }
		private ImageSource _logoImage;
		private bool _logoImageLoaded;
		public ImageSource LogoImage
		{
			get
			{
				if (!_logoImageLoaded)
				{
					_logoImageLoaded = true;
					_logoImage = LoadLogoImage (Logo);
				}
				return _logoImage;
			}
		}
		public Theme (string fn)
		{
			if (string.IsNullOrWhiteSpace (fn) || !Directory.Exists (fn))
				throw new ArgumentException (string.Format ("Theme folder \"{0}\" is invalid", fn));
			FolderPath = fn;
			ThemeName = Path.GetFileName (
				fn.TrimEnd (Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
			if (!File.Exists (ThemeMainFile))
				throw new ArgumentException (string.Format ("Theme file \"{0}\" is invalid", ThemeMainFile));
			ResourceDictionary = new ResourceDictionary ();
			var uri = new Uri (ThemeMainFile, UriKind.RelativeOrAbsolute);
			ResourceDictionary.Source = uri;
			LoadOptionalResources ();
			LoadMetadata ();
		}
		private void LoadOptionalResources ()
		{
			StringResources = TryLoadLocale (Path.Combine (FolderPath, LocaleFileName))
							  ?? new LocaleResources ();
			FileResources = TryLoadPath (Path.Combine (FolderPath, PathFileName))
							?? new PathResources ();
		}
		private static ILocaleResources TryLoadLocale (string file)
		{
			if (!File.Exists (file)) return null;
			try { return LocaleResources.CreateFromFile (file); }
			catch { return null; }
		}
		private static IPathResources TryLoadPath (string file)
		{
			if (!File.Exists (file)) return null;
			try { return PathResources.CreateFromFile (file); }
			catch { return null; }
		}
		private void LoadMetadata ()
		{
			string rawDisplay = ReadResourceString (KeyDisplayName);
			string localizedDisplay = Localize (rawDisplay);
			if (string.IsNullOrWhiteSpace (localizedDisplay))
				localizedDisplay = ThemeName;
			DisplayName = localizedDisplay;
			PublisherDisplayName = Localize (ReadResourceString (KeyPublisherDisplayName));
			Description = Localize (ReadResourceString (KeyDescription));
			string rawLogo = ReadResourceString (KeyLogo);
			Logo = ResolveLogoPath (rawLogo);
		}
		private string ReadResourceString (string key)
		{
			try
			{
				object val = ResourceDictionary [key];
				return val as string;
			}
			catch { return null; }
		}
		private string Localize (string raw)
		{
			if (string.IsNullOrWhiteSpace (raw)) return raw;
			try
			{
				var localized = StringResources != null
					? StringResources.SuitableResource (raw, raw)
					: raw;
				return string.IsNullOrWhiteSpace (localized) ? raw : localized;
			}
			catch { return raw; }
		}
		private string ResolveLogoPath (string raw)
		{
			if (string.IsNullOrWhiteSpace (raw)) return null;
			try
			{
				var resolved = FileResources != null
					? FileResources.SuitableResource (raw, null)
					: null;
				if (!string.IsNullOrWhiteSpace (resolved))
				{
					var full = Path.IsPathRooted (resolved)
						? resolved
						: Path.Combine (FolderPath, resolved);
					if (File.Exists (full)) return full;
				}
			}
			catch { }
			try
			{
				var direct = Path.IsPathRooted (raw)
					? raw
					: Path.Combine (FolderPath, raw);
				if (File.Exists (direct)) return direct;
			}
			catch { }
			return null;
		}
		private static ImageSource LoadLogoImage (string path)
		{
			if (string.IsNullOrEmpty (path) || !File.Exists (path)) return null;
			try
			{
				var bmp = new BitmapImage ();
				bmp.BeginInit ();
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.UriSource = new Uri (path, UriKind.Absolute);
				bmp.EndInit ();
				if (bmp.CanFreeze) bmp.Freeze ();
				return bmp;
			}
			catch { return null; }
		}
		public override string ToString () { return DisplayName ?? ThemeName; }
	}
	public class ThemeManager: IDisposable
	{
		private readonly string [] _baseDirs;
		private readonly MultiRootWatcher _watcher;
		private readonly object _sync = new object ();
		private int _refreshing;
		private int _pending;
		private volatile bool _disposed;
		private List<Tuple<bool, string, Theme>> _allResults = new List<Tuple<bool, string, Theme>> ();
		public ObservableCollection<Theme> Themes { get; private set; }
		public string [] BaseDirs { get { return _baseDirs; } }
		public event EventHandler Changed;
		public ThemeManager (params string [] baseDirs)
		{
			Themes = new ObservableCollection<Theme> ();
			_baseDirs = baseDirs != null
				? Array.FindAll (baseDirs, d => !string.IsNullOrWhiteSpace (d))
				: new string [0];

			List<Tuple<bool, string, Theme>> all;
			List<Theme> valid;
			ScanAll (out all, out valid);
			ApplyResult (all, valid);

			_watcher = new MultiRootWatcher (_baseDirs, TimeSpan.FromMilliseconds (500), OnWatched);
			_watcher.Start ();
		}
		public ThemeManager () : this (DefaultBaseDirs ()) { }
		private static string [] DefaultBaseDirs ()
		{
			return new string [] {
				Path.Combine (ProgramFolder.GlobalFolder.FolderPath, "Themes"),
				Path.Combine (ProgramFolder.CurrentUserFolder.FolderPath, "Themes"),
			};
		}
		private void OnWatched () { TriggerRefresh (); }
		public void ForceRefresh () { TriggerRefresh (); }
		private void TriggerRefresh ()
		{
			if (_disposed) return;
			if (Interlocked.CompareExchange (ref _refreshing, 1, 0) != 0)
			{
				Interlocked.Exchange (ref _pending, 1);
				return;
			}
			Task.Factory.StartNew (RefreshLoop, CancellationToken.None,
				TaskCreationOptions.None, TaskScheduler.Default);
		}
		private void RefreshLoop ()
		{
			try
			{
				do
				{
					Interlocked.Exchange (ref _pending, 0);
					if (_disposed) return;
					try
					{
						List<Tuple<bool, string, Theme>> all;
						List<Theme> valid;
						ScanAll (out all, out valid);
						if (_disposed) return;
						UiDispatch.Run (() => ApplyResult (all, valid));
					}
					catch { }
				}
				while (!_disposed && Interlocked.CompareExchange (ref _pending, 0, 0) != 0);
			}
			finally
			{
				Interlocked.Exchange (ref _refreshing, 0);
				if (!_disposed && Interlocked.CompareExchange (ref _pending, 0, 0) != 0)
					TriggerRefresh ();
			}
		}
		private void ApplyResult (List<Tuple<bool, string, Theme>> all, List<Theme> valid)
		{
			if (_disposed) return;
			lock (_sync) _allResults = all;
			ObservableSync.Sync (
				Themes,
				valid,
				t => t.ThemeName,
				StringComparer.OrdinalIgnoreCase,
				ThemesContentEqual);
			Changed?.Invoke (this, EventArgs.Empty);
		}
		private static bool ThemesContentEqual (Theme a, Theme b)
		{
			if (ReferenceEquals (a, b)) return true;
			if (a == null || b == null) return false;
			if (!string.Equals (a.DisplayName, b.DisplayName, StringComparison.Ordinal)) return false;
			if (!string.Equals (a.PublisherDisplayName, b.PublisherDisplayName, StringComparison.Ordinal)) return false;
			if (!string.Equals (a.Description, b.Description, StringComparison.Ordinal)) return false;
			if (!string.Equals (a.Logo, b.Logo, StringComparison.Ordinal)) return false;
			return true;
		}
		private void ScanAll (out List<Tuple<bool, string, Theme>> all, out List<Theme> valid)
		{
			all = new List<Tuple<bool, string, Theme>> ();
			var byName = new Dictionary<string, Theme> (StringComparer.OrdinalIgnoreCase);
			foreach (var root in _baseDirs)
			{
				if (string.IsNullOrEmpty (root) || !Directory.Exists (root)) continue;
				IEnumerable<string> subdirs;
				try { subdirs = Directory.EnumerateDirectories (root); }
				catch { continue; }
				foreach (var dir in subdirs)
				{
					try
					{
						var t = new Theme (dir);
						all.Add (Tuple.Create (true, "", t));
						byName [t.ThemeName] = t;
					}
					catch (Exception ex)
					{
						all.Add (Tuple.Create (false, ex.Message, (Theme)null));
					}
				}
			}
			valid = byName.Values.ToList ();
			valid.Sort ((a, b) =>
				string.Compare (a.ThemeName, b.ThemeName, StringComparison.OrdinalIgnoreCase));
		}
		public List<Tuple<bool, string, Theme>> Get ()
		{
			lock (_sync) return new List<Tuple<bool, string, Theme>> (_allResults);
		}
		public List<Theme> ValidThemes
		{
			get { lock (_sync) return new List<Theme> (Themes); }
		}
		public Theme GetCurrentUserTheme (bool enableDefault = true)
		{
			var raw = ProgramFolder.CurrentUserFolder.InitConfig ["Theme"] ["ThemeName"] as string;
			return LookupTheme (raw, enableDefault);
		}
		public Theme GetPublicTheme (bool enableDefault = true)
		{
			var raw = ProgramFolder.GlobalFolder.InitConfig ["Theme"] ["ThemeName"] as string;
			return LookupTheme (raw, enableDefault);
		}
		private Theme LookupTheme (string name, bool enableDefault)
		{
			if (string.IsNullOrWhiteSpace (name) && enableDefault) name = "Slate";
			name = name != null ? name.Trim ().ToLowerInvariant () : null;
			if (string.IsNullOrEmpty (name)) return null;
			List<Theme> snapshot;
			lock (_sync) snapshot = new List<Theme> (Themes);
			foreach (var t in snapshot)
			{
				var tn = t.ThemeName != null ? t.ThemeName.Trim ().ToLowerInvariant () : null;
				if (tn == name) return t;
			}
			return null;
		}
		public void SetCurrentUserTheme (string themeName)
		{
			ProgramFolder.CurrentUserFolder.InitConfig ["Theme"] ["ThemeName"] = themeName;
		}
		public void SetCurrentUserTheme (Theme theme)
		{
			if (theme == null) return;
			SetCurrentUserTheme (theme.ThemeName);
		}
		public void SetPublicTheme (string themeName)
		{
			ProgramFolder.GlobalFolder.InitConfig ["Theme"] ["ThemeName"] = themeName;
		}
		public void SetPublicTheme (Theme theme)
		{
			if (theme == null) return;
			SetPublicTheme (theme.ThemeName);
		}
		public Theme CurrentUserTheme
		{
			get { return GetCurrentUserTheme (); }
			set { SetCurrentUserTheme (value); }
		}
		public Theme PublicTheme
		{
			get { return GetPublicTheme (); }
			set { SetPublicTheme (value); }
		}
		public static void Apply (Theme newTheme)
		{
			try { OnThemeChanged (newTheme); } catch { }
			var appResources = Application.Current != null ? Application.Current.Resources : null;
			if (appResources == null) return;
			var mergedDictionaries = appResources.MergedDictionaries;
			if (mergedDictionaries.Count < 3)
			{
				if (mergedDictionaries.Count == 0) mergedDictionaries.Add (new ResourceDictionary ());
				if (mergedDictionaries.Count == 1) mergedDictionaries.Add (new ResourceDictionary ());
				if (mergedDictionaries.Count == 2) mergedDictionaries.Add (new ResourceDictionary ());
			}
			var rd = newTheme != null ? newTheme.ResourceDictionary : null;
			if (rd != null && mergedDictionaries.Count > 2 && ReferenceEquals (mergedDictionaries [2], rd))
				return;
			mergedDictionaries [2] = rd ?? new ResourceDictionary ();
		}
		public static event Action<Theme> ThemeChanged;
		private static void OnThemeChanged (Theme t)
		{
			var h = ThemeChanged;
			if (h != null) h (t);
		}
		public void Dispose ()
		{
			if (_disposed) return;
			_disposed = true;
			_watcher.Dispose ();
		}
	}
}