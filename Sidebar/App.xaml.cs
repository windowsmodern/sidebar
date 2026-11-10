using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms.VisualStyles;
using System.Windows.Threading;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Sidebar
{
	/// <summary>
	/// App.xaml 的交互逻辑
	/// </summary>
	public partial class App: Application
	{
		internal static ThemeManager ThemeMgr { get; private set; } 
		internal static ProgramFolder ProgramFolder { get; } = ProgramFolder.GlobalFolder;
		internal static ProgramFolder CurrentUserFolder { get; } = ProgramFolder.CurrentUserFolder;
		internal static SidebarConfig GlobalConfig { get; } = new SidebarConfig (ProgramFolder);
		internal static SidebarConfig CurrentUserConfig { get; } = new SidebarConfig (CurrentUserFolder);
		internal static TileManager TileMgr { get; } = new TileManager ();
		internal static Mutex GlobalMutex = null;
		internal const string AppIdentity = "WindowsModern.Sidebar!App";
		public App ()
		{
			DispatcherUnhandledException += App_DispatcherUnhandledException;
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
		}
		#region Unhandled Exception
		private void App_DispatcherUnhandledException (object sender, DispatcherUnhandledExceptionEventArgs e)
		{
			var sr = ProgramFolder.StringResources;
			try
			{
				MessageWindow.ShowException (e.Exception, sr.SuitableResource ("SIDEBAR_MESSAGE_ERROR_TITLE"), sr.SuitableResource ("SIDEBAR_MESSAGE_ERROR_DESC"), true);
			}
			catch
			{
				ShowFallbackException (e.Exception, sr);
			}
			finally
			{
				e.Handled = true;
			}
		}
		private static void ShowFallbackException (Exception error, ILocaleResources sr)
		{
			var title = sr?.SuitableResource ("SIDEBAR_MESSAGE_ERROR_TITLE") ?? error?.GetType ().ToString () ?? "Error";
			var description = sr?.SuitableResource ("SIDEBAR_MESSAGE_ERROR_DESC") ?? error?.Message ?? string.Empty;
			var details = GetFallbackExceptionText (error, description);
			try
			{
				var dialog = new TaskDialog ();
				dialog.Caption = title;
				dialog.InstructionText = description;
				dialog.Text = description;
				dialog.DetailsExpandedText = details;
				dialog.DetailsExpanded = true;
				dialog.StandardButtons = TaskDialogStandardButtons.Close;
				dialog.Show ();
			}
			catch
			{
				try { MessageBox.Show (details, title, MessageBoxButton.OK, MessageBoxImage.Error); } catch { }
			}
		}
		private static string GetFallbackExceptionText (Exception error, string description)
		{
			var sb = new StringBuilder ();
			sb.AppendLine (description ?? string.Empty);
			sb.AppendLine ();
			if (error != null && error.InnerException != null)
			{
				sb.AppendLine ();
				sb.AppendLine ("InnerException:");
				sb.AppendLine ($"\t{error.InnerException.GetType ()}: {error.InnerException.Message}");
			}
			return sb.ToString ();
		}
		#endregion
		#region Assembly Load
		private class AssemblyCacheItem
		{
			public string DllPath { get; set; }
			public DateTime CachedTime { get; set; }
		}
		private static readonly ConcurrentDictionary<string, AssemblyCacheItem> AssemblyPathCache = new ConcurrentDictionary<string, AssemblyCacheItem> (StringComparer.OrdinalIgnoreCase);
		private static readonly TimeSpan CacheTTL = TimeSpan.FromMinutes (10);
		private static int _isIndexing = 0;
		private static Timer _cacheRefreshTimer;
		private static void EnsureCacheTimerStarted ()
		{
			if (_cacheRefreshTimer != null) return;
			var timer = new Timer (_ => RebuildAssemblyIndex (), null, (int)CacheTTL.TotalMilliseconds, (int)CacheTTL.TotalMilliseconds);
			if (Interlocked.CompareExchange (ref _cacheRefreshTimer, timer, null) != null) timer.Dispose ();
		}
		private static void RebuildAssemblyIndex ()
		{
			if (Interlocked.Exchange (ref _isIndexing, 1) == 1) return;
			try
			{
				string baseDir = AppDomain.CurrentDomain.BaseDirectory;
				string [] searchRoots = { Path.Combine (baseDir, "Themes"), Path.Combine (baseDir, "Gadgets") };
				var newCache = new ConcurrentDictionary<string, AssemblyCacheItem> (StringComparer.OrdinalIgnoreCase);
				DateTime now = DateTime.Now;
				foreach (var root in searchRoots)
				{
					if (!Directory.Exists (root)) continue;
					try
					{
						foreach (var dllPath in Directory.EnumerateFiles (root, "*.dll", SearchOption.AllDirectories))
						{
							if (dllPath.IndexOf (@"\Deleted\", StringComparison.OrdinalIgnoreCase) >= 0) continue;
							string simpleName = Path.GetFileNameWithoutExtension (dllPath);
							newCache.TryAdd (simpleName, new AssemblyCacheItem { DllPath = dllPath, CachedTime = now });
						}
					}
					catch { }
				}
				AssemblyPathCache.Clear ();
				foreach (var kv in newCache) AssemblyPathCache.TryAdd (kv.Key, kv.Value);
			}
			catch { }
			finally
			{
				Interlocked.Exchange (ref _isIndexing, 0);
			}
		}
		private Assembly CurrentDomain_AssemblyResolve (object sender, ResolveEventArgs args)
		{
			EnsureCacheTimerStarted ();
			var assemblyName = new AssemblyName (args.Name).Name;
			if (assemblyName.EndsWith (".resources", StringComparison.OrdinalIgnoreCase)) return null;
			AssemblyCacheItem cacheItem;
			AssemblyCacheItem removedItem;
			if (AssemblyPathCache.TryGetValue (assemblyName, out cacheItem))
			{
				try
				{
					if (!File.Exists (cacheItem.DllPath))
					{
						AssemblyPathCache.TryRemove (assemblyName, out removedItem);
						return null;
					}
					var asm = Assembly.LoadFrom (cacheItem.DllPath);
					cacheItem.CachedTime = DateTime.Now;
					return asm;
				}
				catch
				{
					AssemblyPathCache.TryRemove (assemblyName, out removedItem);
				}
			}
			string fallbackPath = Path.Combine (AppDomain.CurrentDomain.BaseDirectory, assemblyName + ".dll");
			if (File.Exists (fallbackPath))
			{
				try { return Assembly.LoadFrom (fallbackPath); } catch { }
			}
			if (Thread.VolatileRead (ref _isIndexing) == 0) ThreadPool.QueueUserWorkItem (state => RebuildAssemblyIndex ());
			return null;
		}
		#endregion
		private void Application_Startup (object sender, StartupEventArgs e)
		{
			Directory.SetCurrentDirectory (AppDomain.CurrentDomain.BaseDirectory);
			bool createdNew = false;
			GlobalMutex = new Mutex (true, AppIdentity, out createdNew);
			if (!createdNew)
			{
				Shutdown ();
				return;
			}
			try
			{
				System.Windows.Forms.Application.EnableVisualStyles ();
				System.Windows.Forms.Application.SetCompatibleTextRenderingDefault (false);
			}
			catch { }
			OnlyRemainProgramResources (ProgramFolder?.StringResources);
			OnlyRemainProgramResources (CurrentUserFolder?.StringResources);
			ProgramFolder?.StringResources?.CleanRedundantValues ();
			ProgramFolder?.FileResources?.CleanRedundantValues ();
			CurrentUserFolder?.StringResources?.CleanRedundantValues ();
			CurrentUserFolder?.FileResources?.CleanRedundantValues ();
			Resources.Add ("GlobalConfig", GlobalConfig);
			Resources.Add ("CurrentUserConfig", CurrentUserConfig);
			//BrowserEmulation.SetWebBrowserEmulation ();
			ThemeMgr = new ThemeManager ();
			var theme = ThemeMgr.CurrentUserTheme;
			ThemeManager.Apply (theme);
			var mainwnd = new SidebarWindow ();
			this.MainWindow = mainwnd;
			mainwnd.Show ();
			SidebarPipe.StartServer ();
			UpdateTheme ();
			SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
			ReleaseLargeResourcesAsync ();
		}
		private void SystemEvents_UserPreferenceChanged (object sender, UserPreferenceChangedEventArgs e)
		{
			if (e.Category == UserPreferenceCategory.VisualStyle)
			{
				UpdateTheme ();
			}
		}
		private void Application_Exit (object sender, ExitEventArgs e)
		{
			SidebarPipe.StopServer ();
			SidebarPipe.Message = null;
			SidebarPipe.Mail = null;
		}
		private static void OnlyRemainProgramResources (ILocaleResources lr)
		{
			if (lr == null) return;
			var deletekeys = new List<string> ();
			foreach (var kv in lr)
			{
				var nord = kv.Key.Trim ().ToUpperInvariant ();
				if (nord.StartsWith ("SIDEBAR_") || nord.StartsWith ("TILE") || nord.StartsWith ("CONFIG_") || nord.StartsWith ("ABOUT_")) continue;
				else deletekeys.Add (kv.Key);
			}
			foreach (var i in deletekeys)
			{
				lr?.Remove (i);
			}
		}
		internal static void ReleaseLargeResourcesAsync () => Utilities.ReleaseLargeResourcesAsync ();
		private static void ReleaseLargeResources () => Utilities.ReleaseLargeResources ();
		#region Base Theme
		//private static void SetTheme (string themeName, string themeColor = "NormalColor")
		//{
		//	const BindingFlags staticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;
		//	var presentationFrameworkAsm = Assembly.GetAssembly (typeof (Window));
		//	var themeWrapper = presentationFrameworkAsm?.GetType ("MS.Win32.UxThemeWrapper");
		//	if (themeWrapper == null) return;
		//	var isActiveField = themeWrapper.GetField ("_isActive", staticNonPublic);
		//	var themeColorField = themeWrapper.GetField ("_themeColor", staticNonPublic);
		//	var themeNameField = themeWrapper.GetField ("_themeName", staticNonPublic);
		//	isActiveField?.SetValue (null, true);
		//	themeColorField?.SetValue (null, themeColor);
		//	themeNameField?.SetValue (null, themeName);
		//}
		private static void SetCurrentBaseTheme (WpfTheme wpfTheme, string color = "normalcolor", string themeUri = null)
		{
			return;
			var appResources = Application.Current.Resources;
			var mergedDictionaries = appResources.MergedDictionaries;
			if (mergedDictionaries.Count < 2)
			{
				if (mergedDictionaries.Count == 1) mergedDictionaries.Add (new ResourceDictionary ());
			}
			switch (wpfTheme)
			{
				default:
				case WpfTheme.Default:
					mergedDictionaries [1] = new ResourceDictionary ();
					break;
				case WpfTheme.Classic:
					{
						SetCurrentBaseTheme (WpfTheme.Default);
						var rd = new ResourceDictionary ();
						rd.Source = new Uri ("/PresentationFramework.Classic,Version=4.0.0.0,Culture=neutral,PublicKeyToken=31bf3856ad364e35;component/themes/classic.xaml", UriKind.RelativeOrAbsolute);
						mergedDictionaries [1] = rd;
					}
					break;
				case WpfTheme.Aero:
				case WpfTheme.Luna:
				case WpfTheme.Royale:
					{
						var themeStr = "Aero";
						switch (wpfTheme)
						{
							case WpfTheme.Aero: themeStr = "Aero"; break;
							case WpfTheme.Luna: themeStr = "Luna"; break;
							case WpfTheme.Royale: themeStr = "Royale"; break;
						}
						var rd = new ResourceDictionary ();
						rd.Source = new Uri ($"/PresentationFramework.{themeStr},Version=4.0.0.0,Culture=neutral,PublicKeyToken=31bf3856ad364e35;component/themes/{themeStr.ToLowerInvariant ()}.{color}.xaml", UriKind.RelativeOrAbsolute);
						mergedDictionaries [1] = rd;
					}
					break;
				case WpfTheme.Others:
					try
					{
						var rd = new ResourceDictionary ();
						rd.Source = new Uri (themeUri, UriKind.RelativeOrAbsolute);
						mergedDictionaries [1] = rd;
					}
					catch
					{
						mergedDictionaries [1] = new ResourceDictionary ();
					}
					break;
				case WpfTheme.XPTheme:
					mergedDictionaries [1] = new ResourceDictionary ();
					//try
					//{
					//	ApplyXpThemeToSystemBottom ();
					//}
					//catch
					//{
					//	ApplyThemeAsAppDictionary ();
					//}
					break;
			}
		}
		private void UpdateTheme ()
		{
			if (Environment.OSVersion.Version.Major != 5) return;
			bool isThemed = VisualStyleInformation.IsEnabledByUser;
			string dispName = VisualStyleInformation.DisplayName;
			string colorScheme = VisualStyleInformation.ColorScheme;
			//MessageBox.Show ($"{isThemed}\n{dispName}\n{colorScheme}");
			WindowsXPVisualStyle style = WindowsXPVisualStyle.Others;
			var tld = dispName?.Trim ()?.ToLowerInvariant () ?? "";
			var colors = colorScheme?.Trim ()?.ToLowerInvariant () ?? "";
			if (!isThemed) style = WindowsXPVisualStyle.Classic;
			else if (tld.StartsWith ("windows xp "))
			{
				if (!tld.EndsWith ("ultimate")) style = WindowsXPVisualStyle.Luna;
			}
			else if (tld.StartsWith ("media center") || tld == "royale")
			{
				style = WindowsXPVisualStyle.Royale;
			}
			else if (tld.StartsWith ("embedded"))
			{
				style = WindowsXPVisualStyle.Embedded;
			}
			else if (tld.StartsWith ("zune "))
			{
				style = WindowsXPVisualStyle.Zune;
			}
			else if (tld == "watercolor")
			{
				style = WindowsXPVisualStyle.Watercolor;
			}
			else if (tld.Contains ("slate"))
			{
				if (colors.Contains ("plex")) style = WindowsXPVisualStyle.Plex;
				else style = WindowsXPVisualStyle.Slate;
			}
			else if (tld.Contains ("plex")) style = WindowsXPVisualStyle.Plex;
			else if (tld.Contains ("jade"))
				style = WindowsXPVisualStyle.Jade;
			else if (tld.Contains ("aero")) style = WindowsXPVisualStyle.Aero;
			else style = WindowsXPVisualStyle.Others;
			switch (style)
			{
				default:
				case WindowsXPVisualStyle.Embedded:
				case WindowsXPVisualStyle.Others:
				case WindowsXPVisualStyle.Plex:
				case WindowsXPVisualStyle.Slate:
				case WindowsXPVisualStyle.Watercolor:
					SetCurrentBaseTheme (WpfTheme.XPTheme);
					SetTheme ("Classic");
					break;
				case WindowsXPVisualStyle.Luna:
					SetCurrentBaseTheme (WpfTheme.Luna, colorScheme);
					SetTheme ("Luna", colorScheme);
					break;
				case WindowsXPVisualStyle.Royale:
					SetCurrentBaseTheme (WpfTheme.Royale);
					SetTheme ("Royale");
					break;
				case WindowsXPVisualStyle.Classic:
					SetCurrentBaseTheme (WpfTheme.Default);
					SetTheme ("Classic");
					break;
				case WindowsXPVisualStyle.Jade:
				case WindowsXPVisualStyle.Aero:
					SetCurrentBaseTheme (WpfTheme.Aero);
					SetTheme ("Aero");
					break;
			}
		}
		private static void SetTheme (string themeName, string themeColor = "NormalColor")
		{
			const BindingFlags staticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;
			var presentationFrameworkAsm = Assembly.GetAssembly (typeof (Window));
			if (presentationFrameworkAsm == null) return;
			var systemResources = presentationFrameworkAsm.GetType ("System.Windows.SystemResources");
			systemResources?.GetMethod ("OnThemeChanged", staticNonPublic)?.Invoke (null, null);
			var themeWrapper = presentationFrameworkAsm.GetType ("MS.Win32.UxThemeWrapper");
			if (themeWrapper == null) return;
			var isActiveField = themeWrapper.GetField ("_isActive", staticNonPublic);
			var themeColorField = themeWrapper.GetField ("_themeColor", staticNonPublic);
			var themeNameField = themeWrapper.GetField ("_themeName", staticNonPublic);
			isActiveField?.SetValue (null, true);
			themeColorField?.SetValue (null, themeColor);
			themeNameField?.SetValue (null, themeName);
		}
		private static void ApplyXpThemeToSystemBottom ()
		{
			const BindingFlags staticNonPublic = BindingFlags.Static | BindingFlags.NonPublic;
			var presentationFrameworkAsm = Assembly.GetAssembly (typeof (Window));
			if (presentationFrameworkAsm == null) throw new InvalidOperationException ("PresentationFramework is not loaded.");
			var systemResources = presentationFrameworkAsm.GetType ("System.Windows.SystemResources");
			if (systemResources == null) throw new InvalidOperationException ("SystemResources was not found.");
			systemResources.GetMethod ("OnThemeChanged", staticNonPublic)?.Invoke (null, null);
			SetTheme ("Classic");
			systemResources.GetMethod ("FindThemeStyle", staticNonPublic)?.Invoke (null, new object [] { DependencyObjectType.FromSystemType (typeof (Button)) });
			var source = new ResourceDictionary { Source = new Uri ("pack://application:,,,/PresentationFramework.Theme;component/theme.withtransition.xaml", UriKind.RelativeOrAbsolute) };
			var themedProperty = systemResources.GetProperty ("ThemedResourceDictionaries", staticNonPublic);
			var themedDictionaries = themedProperty?.GetValue (null, null) as System.Collections.IEnumerable;
			if (themedDictionaries == null) throw new InvalidOperationException ("Themed dictionaries were not available.");
			foreach (var info in themedDictionaries)
			{
				if (info == null) continue;
				var dictionaryProperty = info.GetType ().GetProperty ("ResourceDictionary");
				var target = dictionaryProperty?.GetValue (info, null) as ResourceDictionary;
				if (target != null) CopyResources (source, target);
			}
			ClearStaticDictionary (systemResources, "_themeStyleCache", staticNonPublic);
			ClearStaticDictionary (systemResources, "_resourceCache", staticNonPublic);
		}
		private static void ApplyThemeAsAppDictionary ()
		{
			var mergedDictionaries = Application.Current.Resources.MergedDictionaries;
			if (mergedDictionaries.Count == 0) mergedDictionaries.Add (new ResourceDictionary ());
			while (mergedDictionaries.Count < 2) mergedDictionaries.Add (new ResourceDictionary ());
			var rd = new ResourceDictionary ();
			try
			{
				rd.Source = new Uri ("pack://application:,,,/PresentationFramework.Theme;component/theme.withtransition.xaml", UriKind.RelativeOrAbsolute);
			}
			catch
			{
				rd = new ResourceDictionary ();
				rd.Source = new Uri ("pack://application:,,,/PresentationFramework.Theme;component/theme.xaml", UriKind.RelativeOrAbsolute);
			}
			mergedDictionaries [1] = rd;
		}
		private static void CopyResources (ResourceDictionary source, ResourceDictionary target)
		{
			foreach (var key in source.Keys) target [key] = source [key];
			foreach (ResourceDictionary merged in source.MergedDictionaries) CopyResources (merged, target);
		}
		private static void ClearStaticDictionary (Type type, string fieldName, BindingFlags flags)
		{
			var field = type.GetField (fieldName, flags);
			var dictionary = field?.GetValue (null) as System.Collections.IDictionary;
			dictionary?.Clear ();
		}
		enum WindowsXPVisualStyle
		{
			Others, // 其他主题
			Luna,
			Classic,
			Royale, // Media Center 偏白云蓝天的蓝色，较浅
			Zune, // 黑色+橙色
			Embedded, // 嵌入式 Windows XP 的主题，深色，偏青
			Watercolor, // 测试版主题，浅蓝
			Plex, // 网友分享的个性化主题，主要模仿 Longhorn Plex 主题（蓝色)
			Slate, // 网友分享的个性化主题，主要模仿 Longhorn Slate 主题（灰色)
			Jade, // 网友分享的个性化主题，主要模仿 Longhorn Jade 主题（白色)
			Aero // 网友分享的个性化主题
		}
		enum WpfTheme
		{
			Default,
			Luna,
			Royale,
			Classic,
			Aero,
			XPTheme,
			Others
		}
		#endregion
	}
}
