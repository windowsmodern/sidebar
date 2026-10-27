using System;
using System.IO;
using Microsoft.Win32.Init;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Sidebar;

namespace Sidebar
{
	public class ProgramFolder: IProgramFolder
	{
		public string FolderPath { get; }
		public bool Exists => !string.IsNullOrWhiteSpace (FolderPath) && Directory.Exists (FolderPath);
		public bool EnsureExists ()
		{
			if (!Exists) return Directory.CreateDirectory (FolderPath).Exists;
			else return true;
		}
		private ILocaleResources lr = null;
		private IPathResources pr = null;
		public ILocaleResources StringResources => lr;
		public IPathResources FileResources => pr;
		internal ProgramFolder (string folderPath, bool autoCreate = true)
		{
			if (!Directory.Exists (folderPath) && autoCreate)
				Directory.CreateDirectory (folderPath);
			FolderPath = folderPath;
			InitConfig = new InitConfig (Path.Combine (FolderPath, "config.ini"));
			XmlConfig = new XmlConfig (FolderPath);
			try { lr = LocaleResources.CreateFromFile (Path.Combine (FolderPath, "Locale.xml")); } catch { lr = new LocaleResources (); }
			try { pr = PathResources.CreateFromFile (Path.Combine (FolderPath, "Path.xml")); } catch { pr = new PathResources (); }
		}
		public InitConfig InitConfig { get; set; }
		public XmlConfig XmlConfig { get; set; }
		public static ProgramFolder GlobalFolder { get; } = new ProgramFolder (AppDomain.CurrentDomain.BaseDirectory);
		public static ProgramFolder CurrentUserFolder { get; } = new ProgramFolder (Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData), "Windows Modern\\Sidebar"));
		public static ProgramFolder CreateFromPath (string fp) => new ProgramFolder (fp);
	}
	public class SidebarConfig: INotifyPropertyChanged, ISidebarConfig
	{
		InitConfig initConfig = null;
		XmlConfig xmlConfig = null;
		public event PropertyChangedEventHandler PropertyChanged;
		public InitConfig Ini => initConfig;
		public XmlConfig Xml => xmlConfig;
		protected virtual void OnPropertyChanged (string propertyName = null)
		{
			PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));
		}
		private SidebarDirection _direction;
		private double _width;
		private bool _overlapTaskbar;
		private string _screen;
		private Screen _screenInst;
		private bool _locked;
		private string _themeName;
		private bool _topmost;
		private bool _occupyWorkingArea;
		private bool _autorun;
		private bool _canScroll;
		private ObservableCollection<string> _pinnedTiles;
		private ObservableCollection<string> _pinnedBottomTiles;
		private bool _isAdjustingPinned = false;
		private bool _isMinimum = false;
		private bool _forceTransparency = false;
		private bool _firstRun = true;
		public SidebarDirection Direction
		{
			get { return _direction; }
			set
			{
				var str = "";
				_direction = value;
				switch (value)
				{
					case SidebarDirection.Left: str = "Left"; break;
					case SidebarDirection.Right: str = "Right"; break;
				}
				initConfig ["Settings"] ["Direction"] = str;
				OnPropertyChanged (nameof (Direction));
			}
		}
		public double Width
		{
			get { return _width; }
			set
			{
				_width = value;
				initConfig ["Settings"] ["Width"] = value;
				OnPropertyChanged (nameof (Width));
			}
		}
		public bool OverlapTaskbar
		{
			get { return _overlapTaskbar; }
			set
			{
				_overlapTaskbar = value;
				initConfig ["Settings"] ["OverlapTaskbar"] = value;
				OnPropertyChanged (nameof (OverlapTaskbar));
			}
		}
		public string Screen
		{
			get { return _screen; }
			set
			{
				_screen = value;
				_screenInst = null;
				if (string.IsNullOrWhiteSpace (value))
					value = "Primary";
				initConfig ["Settings"] ["CurrentScreen"] = value;
				OnPropertyChanged (nameof (Screen));
				_screenInst = ScreenExtraMethods.GetScreen (value);
			}
		}
		public Screen CurrentScreen
		{
			get { return _screenInst; }
			set { Screen = value.DeviceName; OnPropertyChanged (nameof (CurrentScreen)); }
		}
		public bool Locked
		{
			get { return _locked; }
			set
			{
				_locked = value;
				initConfig ["Settings"] ["Locked"] = _locked;
				OnPropertyChanged (nameof (Locked));
			}
		}
		public string ThemeName
		{
			get { return _themeName; }
			set
			{
				_themeName = value;
				initConfig ["Theme"] ["ThemeName"] = value;
				OnPropertyChanged (nameof (ThemeName));
			}
		}
		/// <summary>
		/// 固定在边栏顶部的磁贴 FamilyName 列表（有序）。
		/// 与 <see cref="PinnedBottomTiles"/> 合并后无重复（忽略大小写和首尾空白）。
		/// </summary>
		public ObservableCollection<string> PinnedTiles
		{
			get
			{
				EnsurePinnedLoaded ();
				return _pinnedTiles;
			}
		}
		/// <summary>
		/// 固定在边栏底部的磁贴 FamilyName 列表（有序）。
		/// 与 <see cref="PinnedTiles"/> 合并后无重复（忽略大小写和首尾空白）。
		/// </summary>
		public ObservableCollection<string> PinnedBottomTiles
		{
			get
			{
				EnsurePinnedLoaded ();
				return _pinnedBottomTiles;
			}
		}
		public bool Topmost
		{
			get { return _topmost; }
			set
			{
				_topmost = value;
				initConfig ["Settings"] ["Topmost"] = value;
				OnPropertyChanged (nameof (Topmost));
			}
		}
		public bool OccupyWorkingArea
		{
			get { return _occupyWorkingArea; }
			set
			{
				_occupyWorkingArea = value;
				initConfig ["Settings"] ["OccupyWorkingArea"] = value;
				OnPropertyChanged (nameof (OccupyWorkingArea));
			}
		}
		public bool AutoRun
		{
			get { return _autorun; }
			set
			{
				_autorun = value;
				initConfig ["Settings"] ["AutoRun"] = value;
				OnPropertyChanged (nameof (AutoRun));
			}
		}
		public bool CanScroll
		{
			get { return _canScroll; }
			set
			{
				_canScroll = value;
				initConfig ["Settings"] ["CanScroll"] = value;
				OnPropertyChanged (nameof (CanScroll));
			}
		}
		public bool IsMinimum
		{
			get { return _isMinimum; }
			set
			{
				_isMinimum = value;
				initConfig ["Settings"] ["IsMinimum"] = value;
				OnPropertyChanged (nameof (IsMinimum));
			}
		}
		public bool ForceTransparency
		{
			get { return _forceTransparency; }
			set
			{
				_forceTransparency = value;
				initConfig ["Settings"] ["ForceTransparency"] = value;
				OnPropertyChanged (nameof (ForceTransparency));
			}
		}
		public bool FirstRun
		{
			get { return _firstRun; }
			set
			{
				_firstRun = value;
				initConfig ["Settings"] ["FirstRun"] = value;
				OnPropertyChanged (nameof (FirstRun));
			}
		}
		#region Pinned Tiles
		/// <summary>
		/// 查询指定 FamilyName 是否处于已固定状态（顶部或底部）。
		/// </summary>
		public bool IsPinned (string familyName)
		{
			if (string.IsNullOrWhiteSpace (familyName)) return false;
			EnsurePinnedLoaded ();
			foreach (var t in _pinnedTiles)
				if (t.NEquals (familyName)) return true;
			foreach (var t in _pinnedBottomTiles)
				if (t.NEquals (familyName)) return true;
			return false;
		}
		/// <summary>
		/// 查询指定 FamilyName 是否固定在顶部。
		/// </summary>
		public bool IsPinnedTop (string familyName)
		{
			if (string.IsNullOrWhiteSpace (familyName)) return false;
			EnsurePinnedLoaded ();
			foreach (var t in _pinnedTiles)
				if (t.NEquals (familyName)) return true;
			return false;
		}
		/// <summary>
		/// 查询指定 FamilyName 是否固定在底部。
		/// </summary>
		public bool IsPinnedBottom (string familyName)
		{
			if (string.IsNullOrWhiteSpace (familyName)) return false;
			EnsurePinnedLoaded ();
			foreach (var t in _pinnedBottomTiles)
				if (t.NEquals (familyName)) return true;
			return false;
		}
		public void PinTop (string familyName)
		{
			if (string.IsNullOrWhiteSpace (familyName)) return;
			EnsurePinnedLoaded ();
			_pinnedBottomTiles.RemoveAll (familyName);
			if (!ContainsNormalized (_pinnedTiles, familyName))
				_pinnedTiles.Insert (0, familyName);
		}
		public void PinBottom (string familyName)
		{
			if (string.IsNullOrWhiteSpace (familyName)) return;
			EnsurePinnedLoaded ();
			_pinnedTiles.RemoveAll (familyName);
			if (!ContainsNormalized (_pinnedBottomTiles, familyName))
				_pinnedBottomTiles.Insert (0, familyName);
		}
		/// <summary>
		/// 取消固定。会从两个表中删除。
		/// </summary>
		public void Unpin (string familyName)
		{
			if (string.IsNullOrWhiteSpace (familyName)) return;
			EnsurePinnedLoaded ();
			_pinnedTiles.RemoveAll (familyName);
			_pinnedBottomTiles.RemoveAll (familyName);
		}
		/// <summary>
		/// 在顶部表内移动。
		/// </summary>
		public void MoveTop (string familyName, int newIndex)
		{
			EnsurePinnedLoaded ();
			MoveIn (_pinnedTiles, familyName, newIndex);
		}
		/// <summary>
		/// 在底部表内移动。
		/// </summary>
		public void MoveBottom (string familyName, int newIndex)
		{
			EnsurePinnedLoaded ();
			MoveIn (_pinnedBottomTiles, familyName, newIndex);
		}
		private void EnsurePinnedLoaded ()
		{
			if (_pinnedTiles != null) return;
			LoadPinnedTiles ();
			_pinnedTiles.CollectionChanged += OnPinnedTilesChanged;
			_pinnedBottomTiles.CollectionChanged += OnPinnedBottomTilesChanged;
		}
		private void LoadPinnedTiles ()
		{
			var topList = new List<string> ();
			var bottomList = new List<string> ();
			if (xmlConfig?.Global != null)
			{
				topList = xmlConfig.Global.Get<List<string>> ("PinnedTiles", new List<string> ()) ?? new List<string> ();
				bottomList = xmlConfig.Global.Get<List<string>> ("PinnedBottomTiles", new List<string> ()) ?? new List<string> ();
			}
			// 去重：先加载顶部表，底部表与之冲突的项丢弃
			var seen = new HashSet<string> (NormalizedStringComparer.Instance);
			var dedupedTop = new List<string> ();
			foreach (var t in topList)
			{
				if (string.IsNullOrWhiteSpace (t)) continue;
				if (seen.Add (t)) dedupedTop.Add (t);
			}
			var dedupedBottom = new List<string> ();
			foreach (var b in bottomList)
			{
				if (string.IsNullOrWhiteSpace (b)) continue;
				if (seen.Add (b)) dedupedBottom.Add (b);
			}
			_pinnedTiles = new ObservableCollection<string> (dedupedTop);
			_pinnedBottomTiles = new ObservableCollection<string> (dedupedBottom);
		}
		private void OnPinnedTilesChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_isAdjustingPinned) return;
			_isAdjustingPinned = true;
			var dispatcher = System.Windows.Application.Current != null
				? System.Windows.Application.Current.Dispatcher
				: null;
			if (dispatcher == null)
			{
				try
				{
					HandlePinnedChanged (_pinnedTiles, _pinnedBottomTiles, e);
					SavePinnedTiles ();
				}
				finally { _isAdjustingPinned = false; }
				OnPropertyChanged (nameof (PinnedTiles));
			}
			else
			{
				dispatcher.BeginInvoke (
					System.Windows.Threading.DispatcherPriority.Background,
					new Action (() => {
						try
						{
							HandlePinnedChanged (_pinnedTiles, _pinnedBottomTiles, e);
							SavePinnedTiles ();
						}
						finally { _isAdjustingPinned = false; }
						OnPropertyChanged (nameof (PinnedTiles));
					}));
			}
		}
		private void OnPinnedBottomTilesChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_isAdjustingPinned) return;
			_isAdjustingPinned = true;
			var dispatcher = System.Windows.Application.Current != null
				? System.Windows.Application.Current.Dispatcher
				: null;
			if (dispatcher == null)
			{
				try
				{
					HandlePinnedChanged (_pinnedBottomTiles, _pinnedTiles, e);
					SavePinnedTiles ();
				}
				finally { _isAdjustingPinned = false; }
				OnPropertyChanged (nameof (PinnedBottomTiles));
			}
			else
			{
				dispatcher.BeginInvoke (
					System.Windows.Threading.DispatcherPriority.Background,
					new Action (() => {
						try
						{
							HandlePinnedChanged (_pinnedBottomTiles, _pinnedTiles, e);
							SavePinnedTiles ();
						}
						finally { _isAdjustingPinned = false; }
						OnPropertyChanged (nameof (PinnedBottomTiles));
					}));
			}
		}
		/// <summary>
		/// 处理一次集合变更：
		/// 1. 从另一个表移除本次新增/替换进来的项（跨表唯一性，新加入为准）；
		/// 2. 当前表内部去重（保留第一次出现）。
		/// </summary>
		private static void HandlePinnedChanged (ObservableCollection<string> self, ObservableCollection<string> other, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
				case NotifyCollectionChangedAction.Replace:
					if (e.NewItems != null)
					{
						foreach (string item in e.NewItems)
						{
							if (string.IsNullOrWhiteSpace (item)) continue;
							other.RemoveAll (item);
						}
					}
					break;
			}
			DeduplicateSelf (self);
		}
		/// <summary>
		/// 表内去重，保留第一次出现。使用规范化比较（忽略大小写与首尾空白）。
		/// </summary>
		private static void DeduplicateSelf (ObservableCollection<string> self)
		{
			if (self.Count <= 1) return;
			var seen = new HashSet<string> (NormalizedStringComparer.Instance);
			var toRemove = new List<int> ();
			for (int i = 0; i < self.Count; i++)
			{
				if (!seen.Add (self [i]))
					toRemove.Add (i);
			}
			for (int i = toRemove.Count - 1; i >= 0; i--)
				self.RemoveAt (toRemove [i]);
		}
		private void SavePinnedTiles ()
		{
			if (xmlConfig?.Global == null) return;
			xmlConfig.Global ["PinnedTiles"] = _pinnedTiles.ToList ();
			xmlConfig.Global ["PinnedBottomTiles"] = _pinnedBottomTiles.ToList ();
		}
		private static bool ContainsNormalized (ObservableCollection<string> coll, string familyName)
		{
			foreach (var t in coll)
				if (t.NEquals (familyName)) return true;
			return false;
		}
		private static void MoveIn (ObservableCollection<string> coll, string familyName, int newIndex)
		{
			int oldIndex = -1;
			for (int i = 0; i < coll.Count; i++)
			{
				if (coll [i].NEquals (familyName))
				{
					oldIndex = i;
					break;
				}
			}
			if (oldIndex < 0) return;
			if (newIndex < 0) newIndex = 0;
			if (newIndex >= coll.Count) newIndex = coll.Count - 1;
			if (oldIndex == newIndex) return;
			coll.Move (oldIndex, newIndex);
		}
		#endregion
		private void InitConfigValues ()
		{
			var settings = initConfig? ["Settings"];
			{
				var gotted = settings?.GetKey ("Direction")?.ReadString ("right")?.NNormalize ();
				switch (gotted)
				{
					case "left": _direction = SidebarDirection.Left; break;
					default:
					case "right": _direction = SidebarDirection.Right; break;
				}
			}
			_width = settings?.GetKey ("Width")?.ReadDouble (150) ?? 150;
			_overlapTaskbar = settings?.GetKey ("OverlapTaskbar")?.ReadBool (false) ?? false;
			_screen = settings?.GetKey ("CurrentScreen")?.ReadString ("Primary") ?? "Primary";
			_screenInst = ScreenExtraMethods.GetScreen (Screen);
			_locked = settings?.GetKey ("Locked")?.ReadBool (false) ?? false;
			_themeName = initConfig? ["Theme"]?.GetKey ("ThemeName").ReadString () ?? "";
			_topmost = settings?.GetKey ("Topmost").ReadBool (false) ?? false;
			_occupyWorkingArea = settings?.GetKey ("OccupyWorkingArea").ReadBool (false) ?? false;
			_autorun = settings?.GetKey ("AutoRun").ReadBool (false) ?? false;
			_canScroll = settings?.GetKey ("CanScroll").ReadBool (false) ?? false;
			_isMinimum = settings?.GetKey ("IsMinimum").ReadBool (false) ?? false;
			_forceTransparency = settings?.GetKey ("ForceTransparency").ReadBool (false) ?? false;
			_firstRun = settings?.GetKey ("FirstRun").ReadBool (true) ?? true;
		}
		public SidebarConfig (InitConfig ini, XmlConfig xml)
		{
			initConfig = ini;
			xmlConfig = xml;
			InitConfigValues ();
		}
		public SidebarConfig (IProgramFolder pf) : this (pf.InitConfig, pf.XmlConfig) { }
	}
	internal static class ObservableCollectionStringExtensions
	{
		/// <summary>
		/// 按规范化比较（忽略大小写与首尾空白）删除所有匹配项。
		/// </summary>
		public static int RemoveAll (this ObservableCollection<string> coll, string familyName)
		{
			if (coll == null || string.IsNullOrWhiteSpace (familyName)) return 0;
			int removed = 0;
			for (int i = coll.Count - 1; i >= 0; i--)
			{
				if (coll [i].NEquals (familyName))
				{
					coll.RemoveAt (i);
					removed++;
				}
			}
			return removed;
		}
	}
}
