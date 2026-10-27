using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using NotifyIcon = System.Windows.Forms.NotifyIcon;
using FormContextMenu = System.Windows.Forms.ContextMenu;
using FormMenuItem = System.Windows.Forms.MenuItem;
using Sidebar;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Collections;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Controls.Primitives;
using System.Security.Cryptography;
using System.ComponentModel;

namespace Sidebar
{
	/// <summary>
	/// Sidebar.xaml 的交互逻辑
	/// </summary>
	public partial class SidebarWindow: Window, ISidebarFeatures, IWin32WindowInterop, IWindowInterop, ITileReorderHost
	{
		public SidebarWindow ()
		{
			Current = this;
			InitializeComponent ();
			if (DWMAPI.IsElderWindows () && !App.CurrentUserConfig.ForceTransparency) AllowsTransparency = false;
			InitTilesList ();
		}
		#region Instance
		public static SidebarWindow Current { get; private set; }
		public SidebarContext sidebarContext = new SidebarContext ();
		#endregion
		#region Tile to Create/Remove/Move
		private Dictionary<string, TileControl> tileCache = new Dictionary<string, TileControl> (NormalizedStringComparer.Instance);
		private ObservableCollection<TileControl> tileOverflowList = new ObservableCollection<TileControl> ();
		private ProjectedView<TileControl, ITileElement> tileOverflowVisualList;
		private bool _rebuildScheduled;
		private readonly HashSet<string> _pendingRemovals = new HashSet<string> (NormalizedStringComparer.Instance);
		private void InitTilesList ()
		{
			var cc = App.CurrentUserConfig;
			tileOverflowVisualList = new ProjectedView<TileControl, ITileElement> (tileOverflowList, tc => tc.VisualView);
			RebuildFromConfig (true);
			cc.PinnedTiles.CollectionChanged += PinnedTiles_CollectionChanged;
			cc.PinnedBottomTiles.CollectionChanged += PinnedBottomTiles_CollectionChanged;
			App.TileMgr.Applications.CollectionChanged += Applications_CollectionChanged;
		}
		private void UninitTilesList ()
		{
			var cc = App.CurrentUserConfig;
			cc.PinnedTiles.CollectionChanged -= PinnedTiles_CollectionChanged;
			cc.PinnedBottomTiles.CollectionChanged -= PinnedBottomTiles_CollectionChanged;
			App.TileMgr.Applications.CollectionChanged -= Applications_CollectionChanged;
			foreach (var tc in tileCache)
			{
				try
				{
					(tc.Value?.Parent as Panel)?.Children?.Remove (tc.Value);
					tc.Value?.Instance?.Instance?.OnDestroy ();
					tc.Value?.TileContext?.Dispose ();
				}
				catch { }
			}
		}
		private void PinnedTiles_CollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			ScheduleRefresh ();
		}
		private void PinnedBottomTiles_CollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			ScheduleRefresh ();
		}
		private void Applications_CollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Replace || e.Action == NotifyCollectionChangedAction.Reset)
			{
				PurgeDeadCache ();
			}
			ScheduleRefresh ();
		}
		private void ScheduleRefresh ()
		{
			if (_rebuildScheduled) return;
			_rebuildScheduled = true;
			Dispatcher.BeginInvoke (DispatcherPriority.Background, new Action (() => {
				_rebuildScheduled = false;
				RebuildFromConfig (IsSidebarViewFirstLoad);
			}));
		}
		private void RebuildFromConfig (bool ?sidebarFirstLoad = false)
		{
			var cc = App.CurrentUserConfig;
			if (cc == null) return;
			var topList = cc.PinnedTiles.ToList ();
			var bottomList = cc.PinnedBottomTiles.ToList ();
			ArrangePanel (TilesControl, topList, false, sidebarFirstLoad);
			ArrangePanel (PinnedTilesControl, bottomList, true, sidebarFirstLoad);
			RemoveOrphans (topList, bottomList);
		}
		private void ArrangePanel (StackPanel panel, List<string> orderedUids, bool pinnedBottom, bool ?sidebarFirstLoad = false)
		{
			if (panel == null) return;
			int insertIndex = 0;
			for (int i = 0; i < orderedUids.Count; i++)
			{
				string umid = orderedUids [i];
				if (_pendingRemovals.Contains (umid)) continue;
				var tile = GetOrCreateTile (umid, sidebarFirstLoad);
				if (tile == null) continue;
				tile.IsPinned = pinnedBottom;
				if (tile?.Instance?.Config != null)
					(tile.Instance.Config as TileConfig)?.NotifyPinned ();
				while (insertIndex < panel.Children.Count &&
					   IsObsoleteChild (panel.Children [insertIndex], orderedUids))
				{
					insertIndex++;
				}
				if (insertIndex < panel.Children.Count &&
					ReferenceEquals (panel.Children [insertIndex], tile))
				{
					insertIndex++;
					continue;
				}
				SafeInsert (panel, insertIndex, tile);
				insertIndex++;
			}
		}
		private static bool IsObsoleteChild (UIElement child, List<string> orderedUids)
		{
			var tile = child as TileControl;
			if (tile == null) return false;
			string umid = tile.UserModelId;
			if (string.IsNullOrEmpty (umid)) return false;
			return !ContainsNormalized (orderedUids, umid);
		}
		private void RemoveOrphans (List<string> topList, List<string> bottomList)
		{
			if (tileCache.Count == 0) return;
			var toRemove = new List<string> ();
			foreach (var kv in tileCache)
			{
				string umid = kv.Key;
				if (ContainsNormalized (topList, umid)) continue;
				if (ContainsNormalized (bottomList, umid)) continue;
				toRemove.Add (umid);
			}
			foreach (var umid in toRemove)
			{
				BeginRemoveTileWithAnimation (umid);
			}
		}
		private void BeginRemoveTileWithAnimation (string umid)
		{
			if (string.IsNullOrWhiteSpace (umid)) return;
			if (!_pendingRemovals.Add (umid)) return;
			TileControl tc;
			if (!tileCache.TryGetValue (umid, out tc) || tc == null)
			{
				_pendingRemovals.Remove (umid);
				return;
			}
			tc.SizeChanged -= TileControl_SizeChanged;
			var task = tc.DestroyTileControl ();
			task.ContinueWith (_ => {
				var d = Dispatcher;
				if (d == null || d.HasShutdownStarted || d.HasShutdownFinished) return;
				try { d.BeginInvoke (new Action (() => FinishRemoveTile (umid))); }
				catch { }
			}, TaskScheduler.Default);
		}
		private void FinishRemoveTile (string umid)
		{
			_pendingRemovals.Remove (umid);
			RemoveFromPanel (TilesControl, umid);
			RemoveFromPanel (PinnedTilesControl, umid);
			tileCache.Remove (umid);
			ScheduleRefresh ();
		}
		private void RemoveFromPanel (StackPanel panel, string umid)
		{
			if (panel == null) return;
			int idx = IndexOfUserModelId (panel, umid);
			if (idx >= 0 && idx < panel.Children.Count) panel.Children.RemoveAt (idx);
		}
		private void FlushPendingRemovals ()
		{
			if (_pendingRemovals.Count == 0) return;
			var keys = new List<string> (_pendingRemovals);
			_pendingRemovals.Clear ();
			foreach (var umid in keys)
			{
				RemoveFromPanel (TilesControl, umid);
				RemoveFromPanel (PinnedTilesControl, umid);
				tileCache.Remove (umid);
			}
		}
		private TileControl GetOrCreateTile (string userModelId, bool ?sidebarFirstLoad = false)
		{
			TileControl tc = null;
			if (tileCache.TryGetValue (userModelId, out tc)) return tc;
			var tas = App.TileMgr.GetApplicationByUserModelId (userModelId);
			if (tas == null) return null;
			if (sidebarFirstLoad != null)
				tc = TileControl.CreateTileControl (tas, this, sidebarFirstLoad.Value);
			else
				tc = TileControl.CreateTileControl (tas, this);
			if (tc == null) return null;
			tc.SizeChanged -= TileControl_SizeChanged;
			tc.SizeChanged += TileControl_SizeChanged;
			tileCache [userModelId] = tc;
			try
			{
				taskbarTopTiles?.Refresh ();
				taskbarBottomTiles?.Refresh ();
			}
			catch { }
			return tc;
		}
		private TileControl GetOrCreateTile (TileApplicationStorage tas, bool ?sidebarFirstLoad = false)
		{
			if (tas == null) return null;
			return GetOrCreateTile (tas.UserModelId, sidebarFirstLoad);
		}
		private void PurgeDeadCache ()
		{
			var alive = new HashSet<string> (App.TileMgr.Applications.Select (a => a.UserModelId), NormalizedStringComparer.Instance);
			var dead = new List<string> ();
			foreach (var key in tileCache.Keys)
				if (!alive.Contains (key)) dead.Add (key);
			foreach (var key in dead)
				tileCache.Remove (key);
		}
		private static void DetachFromParent (UIElement element)
		{
			if (element == null) return;
			var panel = (element as FrameworkElement)?.Parent as Panel;
			if (panel != null) panel.Children.Remove (element);
		}
		private static void SafeInsert (Panel panel, int index, UIElement child)
		{
			if (panel == null || child == null) return;
			DetachFromParent (child);
			int count = panel.Children.Count;
			if (index < 0) index = 0;
			else if (index > count) index = count;
			panel.Children.Insert (index, child);
		}
		private static int IndexOfUserModelId (StackPanel panel, string userModelId)
		{
			if (panel == null || string.IsNullOrWhiteSpace (userModelId)) return -1;
			for (int i = 0; i < panel.Children.Count; i++)
			{
				var tc = panel.Children [i] as TileControl;
				if (tc != null && tc.UserModelId != null && tc.UserModelId.NEquals (userModelId)) return i;
			}
			return -1;
		}
		private static bool ContainsNormalized (IList<string> list, string userModelId)
		{
			if (list == null || string.IsNullOrWhiteSpace (userModelId)) return false;
			for (int i = 0; i < list.Count; i++)
			{
				if (list [i].NEquals (userModelId)) return true;
			}
			return false;
		}
		private static int IndexOfFamilyName (IList<string> list, string userModelId)
		{
			if (list == null || string.IsNullOrWhiteSpace (userModelId)) return -1;
			for (int i = 0; i < list.Count; i++)
			{
				if (list [i].NEquals (userModelId)) return i;
			}
			return -1;
		}
		private static ObservableCollection<string> FindPinnedList (SidebarConfig cc, string userModelId)
		{
			if (cc == null || string.IsNullOrWhiteSpace (userModelId)) return null;
			if (ContainsNormalized (cc.PinnedTiles, userModelId)) return cc.PinnedTiles;
			if (ContainsNormalized (cc.PinnedBottomTiles, userModelId)) return cc.PinnedBottomTiles;
			return null;
		}
		private double TilesRegionHeightLimit
		{
			get
			{
				var p = TilesContainer.Parent as Panel;
				var totalHeight = p.ActualHeight;
				var tilesContainerHeight = totalHeight - TilesContainer.Margin.Top - TilesContainer.Margin.Bottom;
				var nopinTilesHeight = tilesContainerHeight
					- PinnedTilesControl.ActualHeight - PinnedTilesControl.Margin.Top - PinnedTilesControl.Margin.Bottom
					- OverflowTilesControl.ActualHeight - OverflowTilesControl.Margin.Top - OverflowTilesControl.Margin.Bottom;
				return nopinTilesHeight < 0 ? 0 : nopinTilesHeight;
			}
		}
		public bool MoveTileUp (TileControl tile) => MoveTileUp (tile?.UserModelId);
		public bool MoveTileUp (string userModelId) => MoveTileCore (userModelId, -1);
		public bool MoveTileDown (TileControl tile) => MoveTileDown (tile?.UserModelId);
		public bool MoveTileDown (string userModelId) => MoveTileCore (userModelId, +1);
		public bool CanMoveTileUp (TileControl tile) => CanMoveTileUp (tile?.UserModelId);
		public bool CanMoveTileUp (string userModelId) => CanMoveTileCore (userModelId, -1);
		public bool CanMoveTileDown (TileControl tile) => CanMoveTileDown (tile?.UserModelId);
		public bool CanMoveTileDown (string userModelId) => CanMoveTileCore (userModelId, +1);
		public bool RemoveTile (TileControl tile) => RemoveTile (tile?.UserModelId);
		public bool RemoveTile (string userModelId)
		{
			if (string.IsNullOrWhiteSpace (userModelId)) return false;
			var cc = App.CurrentUserConfig;
			if (cc == null) return false;
			if (!cc.IsPinned (userModelId)) return false;
			cc.Unpin (userModelId);
			TileControl overflowWillRemove = null;
			foreach (var vi in tileOverflowList)
			{
				if (vi.UserModelId.NEquals (userModelId))
				{
					overflowWillRemove = vi;
					break;
				}
			}
			tileOverflowList.Remove (overflowWillRemove);
			return true;
		}
		public bool PinTileToBottom (TileControl tile) => PinTileToBottom (tile?.UserModelId);
		public bool PinTileToBottom (string userModelId)
		{
			if (string.IsNullOrWhiteSpace (userModelId)) return false;
			var cc = App.CurrentUserConfig;
			if (cc == null) return false;
			cc.PinBottom (userModelId);
			RebuildOverflowList ();
			return true;
		}
		public bool PinTileToTop (TileControl tile) => PinTileToTop (tile?.UserModelId);
		public bool PinTileToTop (string userModelId)
		{
			if (string.IsNullOrWhiteSpace (userModelId)) return false;
			var cc = App.CurrentUserConfig;
			if (cc == null) return false;
			cc.PinTop (userModelId);
			RebuildOverflowList ();
			return true;
		}
		private bool MoveTileCore (string userModelId, int delta)
		{
			if (string.IsNullOrWhiteSpace (userModelId)) return false;
			var cc = App.CurrentUserConfig;
			if (cc == null) return false;
			var list = FindPinnedList (cc, userModelId);
			if (list == null) return false;
			int idx = IndexOfFamilyName (list, userModelId);
			if (idx < 0) return false;
			int target = idx + delta;
			if (target < 0 || target >= list.Count) return false;
			list.Move (idx, target);
			RebuildOverflowList ();
			return true;
		}
		private bool CanMoveTileCore (string userModelId, int delta)
		{
			if (string.IsNullOrWhiteSpace (userModelId)) return false;
			var cc = App.CurrentUserConfig;
			if (cc == null) return false;
			var list = FindPinnedList (cc, userModelId);
			if (list == null) return false;
			int idx = IndexOfFamilyName (list, userModelId);
			if (idx < 0) return false;
			int target = idx + delta;
			return target >= 0 && target < list.Count;
		}
		#endregion
		#region Tile Overflow
		private HideTilesContainWindow hideTilesWindow;
		private void InitOverflowArea ()
		{
			hideTilesWindow?.Close ();
			hideTilesWindow = new HideTilesContainWindow (this);
			OverflowTilesControl.ItemsSource = tileOverflowVisualList;
		}
		private void DestroyOverflowArea ()
		{
			OverflowTilesControl.ItemsSource = null;
			hideTilesWindow?.Close ();
			hideTilesWindow = null;
		}
		private void TileControl_SizeChanged (object sender, System.Windows.SizeChangedEventArgs e)
		{
			if ((sender as TileControl)?.IsOverflowLock ?? false) return;
			RebuildOverflowList ();
		}
		private DispatcherTimer overflowUpdateTimer;
		private void RebuildOverflowList ()
		{
			if (overflowUpdateTimer == null)
			{
				overflowUpdateTimer = new DispatcherTimer ();
				overflowUpdateTimer.Interval = TimeSpan.FromMilliseconds (10);
			}
			overflowUpdateTimer.Stop ();
			overflowUpdateTimer.Tick -= OverflowUpdateTimer_Tick;
			overflowUpdateTimer.Tick += OverflowUpdateTimer_Tick;
			overflowUpdateTimer.Start ();
		}
		private void OverflowUpdateTimer_Tick (object sender, EventArgs e)
		{
			overflowUpdateTimer.Stop ();
			overflowUpdateTimer.Tick -= OverflowUpdateTimer_Tick;
			OnTileHeightChanged ();
		}
		private void OnTileHeightChanged ()
		{
			if (App.CurrentUserConfig.CanScroll)
			{
				foreach (var tc in tileOverflowList)
				{
					AddToTargetPanel (tc, TilesControl);
				}
				tileOverflowList.Clear ();
				return;
			}
			if (IsTileDragging) return;
			var totalHeight = TilesRegionHeightLimit;
			if (totalHeight <= 0) return;
			List<TileControl> willadd = new List<TileControl> ();
			foreach (FrameworkElement element in TilesControl.Children)
			{
				TileControl tc = null;
				if (element is TileControl) tc = element as TileControl;
				if (tc == null)
				{
					totalHeight -= element.ActualHeight;
					continue;
				}
				else
				{
					totalHeight -= tc.ActualHeight;
					if (totalHeight < 0)
					{
						willadd.Add (tc);
					}
				}
			}
			foreach (var tc in willadd)
			{
				AddToOverflowTilesList (tc);
				AddToTargetPanel (tc, hideTilesWindow?.OverflowTilesRegion);
			}
			SortOverflowTiles ();
			HashSet<TileControl> willremove = new HashSet<TileControl> ();
			var remain = totalHeight;
			if (remain > 0)
			{
				foreach (var tc in tileOverflowList)
				{
					remain -= tc.ActualHeight;
					if (remain >= 0)
					{
						AddToTargetPanel (tc, TilesControl);
						willremove.Add (tc);
					}
					else break;
				}
			}
			foreach (var tc in willremove)
			{
				tileOverflowList.Remove (tc);
			}
			List<TileControl> willclear = new List<TileControl> ();
			foreach (var tc in tileOverflowList)
			{
				if (tc?.Parent == TilesControl) willclear.Add (tc);
			}
			foreach (var tc in willclear)
			{
				tileOverflowList.Remove (tc);
			}
		}
		public bool IsTileOverflow (TileControl tc) => tileOverflowList?.Contains (tc) ?? false;
		private void SortOverflowTiles ()
		{
			if (tileOverflowList == null || tileOverflowList.Count <= 1) return;
			var cc = App.CurrentUserConfig;
			if (cc == null) return;
			var pinned = cc.PinnedTiles;
			var snapshot = tileOverflowList.ToList ();
			snapshot.Sort ((a, b) => {
				int ia = IndexOfPinned (pinned, a?.UserModelId);
				int ib = IndexOfPinned (pinned, b?.UserModelId);
				if (ia != ib) return ia.CompareTo (ib);
				return string.Compare (a?.UserModelId ?? "", b?.UserModelId ?? "", StringComparison.OrdinalIgnoreCase);
			});
			for (int i = 0; i < snapshot.Count; i++)
			{
				int current = tileOverflowList.IndexOf (snapshot [i]);
				if (current >= 0 && current != i) tileOverflowList.Move (current, i);
			}
		}
		private static int IndexOfPinned (IList<string> pinned, string umid)
		{
			if (pinned == null || string.IsNullOrWhiteSpace (umid)) return int.MaxValue;
			for (int i = 0; i < pinned.Count; i++)
			{
				if (pinned [i].NEquals (umid)) return i;
			}
			return int.MaxValue;
		}
		private void AddToOverflowTilesList (TileControl tc)
		{
			if (tc == null) return;
			if (tileOverflowList.Contains (tc)) return;
			tc.IsOverflowLock = true;
			tileOverflowList.Add (tc);
		}
		private void AddToTargetPanel (FrameworkElement ue, Panel p)
		{
			if (ue?.Parent as Panel == p) return;
			else
			{
				(ue?.Parent as Panel)?.Children?.Remove (ue);
				if (p == null) return;
				p.Children.Add (ue);
			}
		}
		public bool MoveTileOutOfOverflow (TileControl tile) => MoveTileOutOfOverflow (tile?.UserModelId);
		public bool MoveTileOutOfOverflow (string userModelId)
		{
			if (string.IsNullOrWhiteSpace (userModelId)) return false;
			var cc = App.CurrentUserConfig;
			if (cc == null) return false;
			var list = FindPinnedList (cc, userModelId);
			if (list == null) return false;
			int idx = IndexOfFamilyName (list, userModelId);
			if (idx < 0) return false;
			TileControl tile = null;
			tileCache.TryGetValue (userModelId, out tile);
			if (tile != null) tileOverflowList.Remove (tile);
			if (idx > 0)
			{
				list.Move (idx, 0);
			}
			else
			{
				if (tile != null) AddToTargetPanel (tile, TilesControl);
				RebuildOverflowList ();
			}
			return true;
		}
		public void EnsureTileRemoveFromOverflow (string umid)
		{
			if (string.IsNullOrWhiteSpace (umid)) return;
			if (App.CurrentUserConfig.IsPinned (umid)) return;
			TileControl tile = null;
			foreach (var tc in tileOverflowList)
			{
				if (tc.UserModelId.NEquals (umid))
				{
					tile = tc;
					break;
				}
			}
			if (tile == null) return;
			tileOverflowList.Remove (tile);
		}
		public void EnsureTileRemoveFromOverflow (TileControl tc) => EnsureTileRemoveFromOverflow (tc?.UserModelId);
		#endregion
		#region Features
		public ISidebarConfig Config => App.CurrentUserConfig;
		public bool Request (ISidebarRequest request) => Communicate (request);
		public bool Communicate (ITileRequest request)
		{
			if (request.RequestTarget.NEquals ("Sidebar"))
			{
				if (request.RequestName.NEquals ("Notification"))
				{
					EventHandler handler = null;
					handler = (s, e) => {
						notifyIcon.BalloonTipClicked -= handler;
						foreach (var i in tileCache)
						{
							if (i.Key.NEquals (request.RequestSource))
							{
								var resp = new TileResponse (request);
								resp.Success = true;
								resp.ResponseName = "NotificationClick";
								Response (resp);
								break;
							}
						}
					};
					EventHandler closeHandler = null;
					closeHandler = (s, e) => {
						notifyIcon.BalloonTipClicked -= handler;
						notifyIcon.BalloonTipClosed -= closeHandler;
						try
						{
							if (notifyIcon.Icon != Properties.Resources.AppIcon)
							{
								var icon = notifyIcon.Icon;
								notifyIcon.Icon = null;
								icon?.Dispose ();
							}
						}
						catch { }
						finally
						{
							notifyIcon.Icon = Properties.Resources.AppIcon;
						}
						notifyIcon.Text = App.ProgramFolder.StringResources.SuitableResource ("SIDEBAR_TITLE");
					};
					notifyIcon.BalloonTipClicked -= handler;
					notifyIcon.BalloonTipClosed -= closeHandler;
					notifyIcon.BalloonTipClicked += handler;
					notifyIcon.BalloonTipClosed += closeHandler;
					if (request.RequestDatas is string)
					{
						var ts = App.TileMgr.GetApplicationByUserModelId (request.RequestSource);
						var tc = tileCache [ts.UserModelId];
						var iconimg = tc.VisualView.Icon;
						var name = ts.Storage.TileFolder.StringResources.SuitableResource (ts.DisplayName, ts.DisplayName) ?? "Tile";
						var content = request.RequestDatas as string;
						if (string.IsNullOrEmpty (content)) content = " ";
						var nin = new NotifyIconNotification2 {
							Timeout = 5000,
							Title = String.Format (
								App.ProgramFolder.StringResources.SuitableResource ("SIDEBAR_NOTIFY_TITLE", "Notification from {0}"),
								name
							),
							Content = content,
							Icon = System.Windows.Forms.ToolTipIcon.None,
							IconImage = iconimg
						};
						var result = Communicate (new SidebarRequestInternal {
							RequestTarget = "WindowsModern.NotificationHistoryTile_6jwq91e3ytb32!App",
							RequestName = "InternalNotificationAddHistoryInternal",
							RequestDatas = nin,
						});
						if (result)
						{
							Notification wnd = null;
							EventHandler clickhandler = null, closeclickhandler = null;
							clickhandler = (s, e) => {
								foreach (var i in tileCache)
								{
									if (i.Key.NEquals (request.RequestSource))
									{
										var resp = new TileResponse (request);
										resp.Success = true;
										resp.ResponseName = "NotificationClick";
										Response (resp);
										break;
									}
								}
								Communicate (new SidebarRequestInternal {
									RequestTarget = "WindowsModern.NotificationHistoryTile_6jwq91e3ytb32!App",
									RequestName = "InternalNotificationSetRead",
									RequestDatas = nin,
								});
							};
							closeclickhandler = (s, e) => {
								Communicate (new SidebarRequestInternal {
									RequestTarget = "WindowsModern.NotificationHistoryTile_6jwq91e3ytb32!App",
									RequestName = "InternalNotificationSetRead",
									RequestDatas = nin,
								});
							};
							wnd = Notification.ShowNotification (nin);
							wnd.BalloonTipClicked += clickhandler;
							wnd.BalloonTipCloseClicked += closeclickhandler;
							return true;
						}
						notifyIcon.ShowBalloonTip (
							5000,
							String.Format (
								App.ProgramFolder.StringResources.SuitableResource ("SIDEBAR_NOTIFY_TITLE", "Notification from {0}"),
								name
							),
							content,
							System.Windows.Forms.ToolTipIcon.None
						);
						return true;
					}
					else if (request.RequestDatas is NotifyIconNotification)
					{
						var ts = App.TileMgr.GetApplicationByUserModelId (request.RequestSource);
						var tc = tileCache [ts.UserModelId];
						var iconimg = tc.VisualView.Icon;
						var name = ts.Storage.TileFolder.StringResources.SuitableResource (ts.DisplayName, ts.DisplayName) ?? "Tile";
						var nin = request.RequestDatas as NotifyIconNotification;
						var timeout = nin.Timeout;
						var content = nin.Content;
						if (string.IsNullOrEmpty (content)) content = " ";
						var title = nin.Title;
						if (string.IsNullOrEmpty (title))
						{
							title = String.Format (App.ProgramFolder.StringResources.SuitableResource ("SIDEBAR_NOTIFY_TITLE", "Notification from {0}"), name ?? " ");
						}
						var nin2 = new NotifyIconNotification2 (nin) {
							Title = title,
							Content = content,
							IconImage = ((nin is NotifyIconNotification2) ? (nin as NotifyIconNotification2).IconImage : null) ?? iconimg
						};
						var result = Communicate (new SidebarRequestInternal {
							RequestTarget = "WindowsModern.NotificationHistoryTile_6jwq91e3ytb32!App",
							RequestName = "InternalNotificationAddHistoryInternal",
							RequestDatas = nin2,
						});
						if (result)
						{
							Notification wnd = null;
							EventHandler clickhandler = null, closeclickhandler = null;
							clickhandler = (s, e) => {
								foreach (var i in tileCache)
								{
									if (i.Key.NEquals (request.RequestSource))
									{
										var resp = new TileResponse (request);
										resp.Success = true;
										resp.ResponseName = "NotificationClick";
										Response (resp);
										break;
									}
								}
								Communicate (new SidebarRequestInternal {
									RequestTarget = "WindowsModern.NotificationHistoryTile_6jwq91e3ytb32!App",
									RequestName = "InternalNotificationSetRead",
									RequestDatas = nin2,
								});
							};
							closeclickhandler = (s, e) => {
								Communicate (new SidebarRequestInternal {
									RequestTarget = "WindowsModern.NotificationHistoryTile_6jwq91e3ytb32!App",
									RequestName = "InternalNotificationSetRead",
									RequestDatas = nin2,
								});
							};
							wnd = Notification.ShowNotification (nin2);
							wnd.BalloonTipClicked += clickhandler;
							wnd.BalloonTipCloseClicked += closeclickhandler;
							return true;
						}
						notifyIcon.ShowBalloonTip (timeout, title, content, nin.Icon);
						return true;
					}
					else
					{
						notifyIcon.BalloonTipClicked -= handler;
						notifyIcon.BalloonTipClosed -= closeHandler;
					}
				}
				else if (request.RequestName.NEquals ("Resize"))
				{
					foreach (var t in tileCache)
					{
						if (t.Key.NEquals (request.RequestSource))
						{
							if (request.RequestDatas is double)
							{
								double height = (double)request.RequestDatas;
								var tile = t.Value;
								if (tile != null)
								{
									tile.Dispatcher.Invoke (new Action (() => tile.RequireSetTileContentHeight (height)));
								}
							}
							return true;
						}
					}
				}
				else if (request.RequestName.NEquals ("ResizeIgnoreAutoSize"))
				{
					foreach (var t in tileCache)
					{
						if (t.Key.NEquals (request.RequestSource))
						{
							if (request.RequestDatas is double)
							{
								double height = (double)request.RequestDatas;
								var tile = t.Value;
								if (tile != null)
								{
									tile.Dispatcher.Invoke (new Action (() => tile.RequireSetTileContentHeightIgnoreAutoSize (height)));
								}
							}
							return true;
						}
					}
				}
				else if (request.RequestName.NEquals ("ResizeForever"))
				{
					foreach (var t in tileCache)
					{
						if (t.Key.NEquals (request.RequestSource))
						{
							if (request.RequestDatas is double)
							{
								double height = (double)request.RequestDatas;
								var tile = t.Value;
								if (tile != null)
								{
									tile.Dispatcher.Invoke (new Action (() => tile.RequireSetTileContentHeightAndSave (height)));
								}
							}
							return true;
						}
					}
				}
				else if (request.RequestName.NEquals ("OpacityAnime"))
				{
					foreach (var t in tileCache)
					{
						if (t.Key.NEquals (request.RequestSource))
						{
							var tile = t.Value;
							if (tile != null)
							{
								if (request.RequestDatas is double)
								{
									double opacity = (double)request.RequestDatas;
									tile.Dispatcher.Invoke (new Action (() => tile.RequireLoadTileOpacityAnime (opacity)));
								}
								else
								{
									tile.Dispatcher.Invoke (new Action (() => tile.RequireLoadTileOpacityAnime ()));
								}
							}
							return true;
						}
					}
				}
				else if (request.RequestName.NEquals ("FlyoutSizeToContent"))
				{
					foreach (var t in tileCache)
					{
						if (t.Key.NEquals (request.RequestSource))
						{
							if (request.RequestDatas is SizeToContent)
							{
								SizeToContent sizeMode = (SizeToContent)request.RequestDatas;
								var f = t.Value?.CurrentFlyout;
								if (f != null)
								{
									f.Dispatcher.Invoke (new Action (() => f.SizeToContent = sizeMode));
								}
							}
						}
					}
				}
				else if (request.RequestName.NEquals ("FlyoutUpdatePosition"))
				{
					foreach (var t in tileCache)
					{
						if (t.Key.NEquals (request.RequestSource))
						{
							var f = t.Value.CurrentFlyout;
							if (f != null)
							{
								f.Dispatcher.BeginInvoke (DispatcherPriority.Render, new Action (() => f.FixPosition ()));
								return true;
							}
						}
					}
				}
				else if (request.RequestName.NEquals ("RequireExtraFlyout"))
				{
					foreach (var t in tileCache)
					{
						if (t.Key.NEquals (request.RequestSource))
						{
							var tile = t.Value;
							if (tile != null)
							{
								var flyoutWnd = new FlyoutWindow (tile);
								var flyoutContent = flyoutWnd.FlyoutContentContainer;
								Response (new TileResponse (request) {
									ResponseName = "ExtraFlyoutWindow",
									ResponseData = new FlyoutAboutEventArgs (flyoutContent, flyoutWnd)
								});
							}
							return true;
						}
					}
				}
				else if (request.RequestName.NEquals ("GetTitle"))
				{
					foreach (var t in tileCache)
					{
						if (t.Key.NEquals (request.RequestSource))
						{
							var tile = t.Value;
							if (tile != null)
							{
								Response (new TileResponse (request) {
									ResponseName = "ReturnGetTitle",
									ResponseData = tile.DisplayName
								});
							}
							return true;
						}
					}
				}
				else if (request.RequestName.NEquals ("SetTitle"))
				{
					foreach (var t in tileCache)
					{
						if (t.Key.NEquals (request.RequestSource))
						{
							var tile = t.Value;
							tile.DisplayName = request.RequestDatas as string;
							return true;
						}
					}
				}
				else if (request.RequestName.NEquals ("GetIcon"))
				{
					foreach (var t in tileCache)
					{
						if (t.Key.NEquals (request.RequestSource))
						{
							var tile = t.Value;
							if (tile != null)
							{
								Response (new TileResponse (request) {
									ResponseName = "ReturnGetIcon",
									ResponseData = tile.Icon
								});
							}
							return true;
						}
					}
				}
				else if (request.RequestName.NEquals ("SetIcon"))
				{
					foreach (var t in tileCache)
					{
						if (t.Key.NEquals (request.RequestSource))
						{
							var tile = t.Value;
							tile.Icon = request.RequestDatas as ImageSource;
							return true;
						}
					}
				}
				else if (request.RequestName.NEquals ("SidebarNotification"))
				{
					Notification ntwnd = null;
					if (request.RequestDatas is string)
					{
						var ts = App.TileMgr.GetApplicationByUserModelId (request.RequestSource);
						var name = ts.Storage.TileFolder.StringResources.SuitableResource (ts.DisplayName, ts.DisplayName) ?? "Tile";
						var content = request.RequestDatas as string;
						if (string.IsNullOrEmpty (content)) content = "";
						var tc = tileCache [ts.UserModelId];
						ntwnd = Notification.ShowNotification (new NotifyIconNotification {
							Timeout = 5000,
							Title = String.Format (
								App.ProgramFolder.StringResources.SuitableResource ("SIDEBAR_NOTIFY_TITLE", "Notification from {0}"),
								name
							),
							Content = content,
							Icon = System.Windows.Forms.ToolTipIcon.None
						}, tc.VisualView.Icon);
					}
					else if (request.RequestDatas is NotifyIconNotification)
					{
						var ts = App.TileMgr.GetApplicationByUserModelId (request.RequestSource);
						var name = ts.Storage.TileFolder.StringResources.SuitableResource (ts.DisplayName, ts.DisplayName) ?? "Tile";
						var nin = request.RequestDatas as NotifyIconNotification;
						var timeout = nin.Timeout;
						var content = nin.Content;
						if (string.IsNullOrEmpty (content)) content = " ";
						var title = nin.Title;
						if (string.IsNullOrEmpty (title))
						{
							title = String.Format (App.ProgramFolder.StringResources.SuitableResource ("SIDEBAR_NOTIFY_TITLE", "Notification from {0}"), name ?? " ");
						}
						var tc = tileCache [ts.UserModelId];
						ImageSource img = null;
						try
						{
							img = tc?.VisualView.Icon;
						}
						catch { }
						if (nin is NotifyIconNotification2)
						{
							var nin2 = nin as NotifyIconNotification2;
							img = nin2.IconImage ?? img;
						}
						ntwnd = Notification.ShowNotification (new NotifyIconNotification {
							Timeout = timeout,
							Title = title,
							Content = content,
							Icon = nin.Icon
						}, img);
					}
					if (ntwnd != null)
					{
						ntwnd.BalloonTipClicked += (s, e) => {
							foreach (var i in tileCache)
							{
								if (i.Key.NEquals (request.RequestSource))
								{
									var resp = new TileResponse (request);
									resp.Success = true;
									resp.ResponseName = "SidebarNotificationClick";
									Response (resp);
									break;
								}
							}
						};
						ntwnd.BalloonTipCloseClicked += (s, e) => {
							foreach (var i in tileCache)
							{
								if (i.Key.NEquals (request.RequestSource))
								{
									var resp = new TileResponse (request);
									resp.Success = true;
									resp.ResponseName = "SidebarNotificationCloseClick";
									Response (resp);
									break;
								}
							}
						};
						ntwnd.BalloonTipClosed += (s, e) => {
							foreach (var i in tileCache)
							{
								if (i.Key.NEquals (request.RequestSource))
								{
									var resp = new TileResponse (request);
									resp.Success = true;
									resp.ResponseName = "SidebarNotificationClose";
									Response (resp);
									break;
								}
							}
						};
						return true;
					}
				}
				return false;
			}
			else
			{
				foreach (var t in tileCache)
				{
					if (t.Key.NEquals (request.RequestTarget))
					{
						return t.Value.Instance.Instance.OnRequest (request);
					}
				}
			}
			return false;
		}
		public bool Response (ITileResponse resp)
		{
			if (resp.ResponseTarget.NEquals ("Sidebar"))
			{
				return false;
			}
			else
			{
				foreach (var t in tileCache)
				{
					if (t.Key.NEquals (resp.ResponseTarget))
					{
						return t.Value.Instance.Instance.OnResponse (resp);
					}
				}
			}
			return false;
		}
		#endregion
		#region Menu Item
		private void MenuItemMinBar_Click (object sender, RoutedEventArgs e)
		{
			MinimumSidebar ();
		}
		private ConfigWindow sidebarConfWnd = null;
		private void MenuItemProperties_Click (object sender, RoutedEventArgs e)
		{
			if (sidebarConfWnd != null) sidebarConfWnd.Activate ();
			else
			{
				sidebarConfWnd = new ConfigWindow ();
				sidebarConfWnd.Closed += SidebarConfWnd_Closed;
				sidebarConfWnd.Show ();
			}
		}
		private void SidebarConfWnd_Closed (object sender, EventArgs e)
		{
			sidebarConfWnd.Closed -= SidebarConfWnd_Closed;
			sidebarConfWnd = null;
		}
		private void MenuItemCancel_Click (object sender, RoutedEventArgs e)
		{
			try { Close (); } catch { }
		}
		private void MenuItemGetMoreTiles_Click (object sender, RoutedEventArgs e)
		{
			Process.Start (System.IO.Path.Combine (App.ProgramFolder.FolderPath, "SGStore.exe"));
		}
		private void TopOptionsButton_Click (object sender, RoutedEventArgs e)
		{
			var menu = ContextMenu;
			if (menu == null) return;
			menu.PlacementTarget = (UIElement)sender ?? this;
			menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
			menu.IsOpen = true;
		}
		private void MenuItemAddTiles_SubmenuOpened (object sender, RoutedEventArgs e)
		{
			var menuItem = sender as MenuItem;
			if (menuItem == null) return;
			menuItem.Items.Clear ();
			var cc = App.CurrentUserConfig;
			var apps = App.TileMgr.Applications;
			if (apps == null || apps.Count == 0)
			{
				menuItem.Items.Add (new MenuItem {
					Header = App.ProgramFolder.StringResources.SuitableResource ("SIDEBAR_CONTEXTMENU_NOTILE", "(No tiles installed)"),
					IsEnabled = false
				});
				return;
			}
			foreach (var app in apps)
			{
				string umid = app.UserModelId;
				if (string.IsNullOrWhiteSpace (umid)) continue;
				bool isPinned = cc.IsPinned (umid);
				var subItem = new MenuItem {
					Header = app.DisplayName,
					IsCheckable = true,
					IsChecked = isPinned,
					Tag = umid
				};
				subItem.Click += MenuSubItemApp_Click;
				menuItem.Items.Add (subItem);
			}
		}
		private void MenuSubItemApp_Click (object sender, RoutedEventArgs e)
		{
			var item = sender as MenuItem;
			if (item == null) return;
			string umid = item.Tag as string;
			if (string.IsNullOrWhiteSpace (umid)) return;
			var cc = App.CurrentUserConfig;
			if (item.IsChecked) cc.PinTop (umid);
			else cc.Unpin (umid);
		}
		#endregion
		#region Handle & Aero
		public HWND HWnd => InteropHelper?.Handle ?? IntPtr.Zero;
		public WindowInteropHelper InteropHelper { get; private set; }
		public IntPtr Handle => InteropHelper?.Handle ?? IntPtr.Zero;
		public IntPtr WndOwner => InteropHelper?.Owner ?? IntPtr.Zero;
		private void InitInteropHelper ()
		{
			InteropHelper = null;
			InteropHelper = new WindowInteropHelper (this);
		}
		private bool? _lastAeroState = null;
		private void ApplyAero (bool value)
		{
			if (!DWMAPI.IsDwmAvailable ()) return;
			if (_lastAeroState.HasValue && _lastAeroState.Value == value) return;
			_lastAeroState = value;
			IntPtr hwnd = Handle;
			if (value)
			{
				DWMAPI.EnableBlur (ref hwnd, IntPtr.Zero);
				try { if (!AllowsTransparency) AllowsTransparency = true; } catch { }
			}
			else DWMAPI.DisableBlur (ref hwnd);
			try { if (Environment.OSVersion.Version.Major > 6) WindowAccent.SetAccentPolicy (hwnd, value); } catch { }
		}
		private bool isSourceInitialized = false;
		private HwndSource hwndSource = null;
		private void InitHwndSource ()
		{
			hwndSource?.Dispose ();
			hwndSource = HwndSource.FromHwnd (HWnd);
		}
		private void DestoryHwndSource ()
		{
			hwndSource?.Dispose ();
			hwndSource = null;
		}
		private void Window_SourceInitialized (object sender, EventArgs e)
		{
			isSourceInitialized = true;
			InitInteropHelper ();
			InitHwndSource ();
			InitAppBar ();
			bool enableBlur = false;
			if (Application.Current.Resources.Contains ("EnableBlur"))
				enableBlur = (bool)Application.Current.Resources ["EnableBlur"];
			ApplyAero (enableBlur);
			var h = HWnd;
			Presentation.DwmThemeProvider.Instance.StartListening (hwndSource);
			h.StylesEx |= Win32.ExtendedWindowStyles.WS_EX_TOOLWINDOW;
			h.StylesEx |= Win32.ExtendedWindowStyles.WS_EX_LAYERED;
			//h.StylesEx |= Win32.ExtendedWindowStyles.WS_EX_NOACTIVATE;
			#region Preview Desktop
			IntPtr desktopHwnd = Win32WindowNative.FindWindow ("Progman", null);
			if (desktopHwnd == IntPtr.Zero)
			{
				IntPtr workerW = IntPtr.Zero;
				Win32WindowNative.EnumWindows (delegate (IntPtr hWnd, IntPtr lParam)
				{
					if (Win32WindowNative.FindWindowEx (hWnd, IntPtr.Zero, "SHELLDLL_DefView", null) != IntPtr.Zero)
					{
						workerW = Win32WindowNative.FindWindowEx (IntPtr.Zero, hWnd, "WorkerW", null);
						return false;
					}
					return true;
				}, IntPtr.Zero);
				desktopHwnd = workerW != IntPtr.Zero ? workerW : IntPtr.Zero;
			}
			if (desktopHwnd != IntPtr.Zero)
			{
				// GWL_HWNDPARENT = -8
				Win32WindowNative.SetWindowLong (Handle, -8, desktopHwnd.ToInt32 ());
			}
			if (DWMAPI.IsDwmAvailable ())
			{
				try { DWMAPI.RemoveFromFlip3D (Handle); } catch { }
				try
				{
					if (Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Major != 0)
						DWMAPI.RemoveFromAeroPeek (Handle);
				}
				catch { }
			}
			#endregion
		}
		#endregion
		#region Appbar & Overlap Taskbar
		private SidebarAppBar appBar;
		private TaskbarOverlapHelper taskbarHelper;
		private MultiView<ITileElement> taskbarTilesList;
		private ProjectedView<string, ITileElement> taskbarTopTiles;
		private ProjectedView<string, ITileElement> taskbarBottomTiles;
		private View<ITileElement> taskbarTopTilesListNoNull;
		private View<ITileElement> taskbarBottomTilesListNoNull;
		private DispatcherTimer taskbarPosTimer = new DispatcherTimer () {
			Interval = TimeSpan.FromMilliseconds (50)
		};
		private void InitAppBar ()
		{
			var cc = App.CurrentUserConfig;
			appBar?.Dispose ();
			appBar = new SidebarAppBar (this);
			appBar.Direction = cc.Direction;
			taskbarHelper?.Dispose ();
			taskbarHelper = new TaskbarOverlapHelper (this);
			taskbarHelper.Direction = cc.Direction;
			taskbarHelper.CurrentScreen = cc.CurrentScreen;
		}
		private ITileElement ResolveTileVisual (string userModelId)
		{
			if (string.IsNullOrEmpty (userModelId)) return null;
			TileControl tc;
			if (tileCache.TryGetValue (userModelId, out tc) && tc != null) return tc.VisualView;
			return null;
		}
		private void InitTaskbarTilesList ()
		{
			if (taskbarTilesList != null) return;
			var cc = App.CurrentUserConfig;
			if (cc == null) return;
			taskbarTilesList = new MultiView<ITileElement> ();
			taskbarTopTiles = new ProjectedView<string, ITileElement> (cc.PinnedTiles, ResolveTileVisual);
			taskbarBottomTiles = new ProjectedView<string, ITileElement> (cc.PinnedBottomTiles, ResolveTileVisual);
			taskbarTopTilesListNoNull = taskbarTopTiles.Where (ite => ite != null);
			taskbarBottomTilesListNoNull = taskbarBottomTiles.Where (ite => ite != null);
			taskbarTilesList.AddSource (taskbarTopTilesListNoNull);
			taskbarTilesList.AddSource (taskbarBottomTilesListNoNull);
			if (cc.IsMinimum)
			{
				if (TaskbarTiles.ItemsSource == null)
					TaskbarTiles.ItemsSource = taskbarTilesList;
			}
		}
		private void DestroyTaskbarTilesList ()
		{
			if (TaskbarTiles != null) TaskbarTiles.ItemsSource = null;
			if (taskbarTopTilesListNoNull != null)
			{
				taskbarTopTilesListNoNull?.Dispose ();
				taskbarTopTilesListNoNull = null;
			}
			if (taskbarBottomTilesListNoNull != null)
			{
				taskbarBottomTilesListNoNull?.Dispose ();
				taskbarBottomTilesListNoNull = null;
			}
			if (taskbarTilesList != null)
			{
				taskbarTilesList.Dispose ();
				taskbarTilesList = null;
			}
			if (taskbarTopTiles != null)
			{
				taskbarTopTiles.Dispose ();
				taskbarTopTiles = null;
			}
			if (taskbarBottomTiles != null)
			{
				taskbarBottomTiles.Dispose ();
				taskbarBottomTiles = null;
			}
		}
		private void MinimumSidebar ()
		{
			var cc = App.CurrentUserConfig;
			cc.IsMinimum = true;
			if (cc.OccupyWorkingArea) appBar.Unregister ();
			if (cc.OverlapTaskbar)
			{
				SidebarView.Visibility = Visibility.Collapsed;
				SizeToContent = SizeToContent.Width;
				if (TaskbarView.Visibility != Visibility.Visible)
				{
					TaskbarView.Visibility = Visibility.Visible;
					var sb = TryFindResource ("SidebarShowTaskbarTiles") as Storyboard;
					sb?.Begin (this);
				}
				Visibility = Visibility.Visible;
				if (TaskbarTiles.ItemsSource == null)
					TaskbarTiles.ItemsSource = taskbarTilesList;
				RefreshWndPos ();
			}
			else
			{
				ClearValue (SizeToContentProperty);
				Width = cc.Width;
				SidebarView.Visibility = Visibility.Visible;
				TaskbarView.Visibility = Visibility.Collapsed;
				Visibility = Visibility.Hidden;
				TaskbarTiles.ItemsSource = null;
			}
		}
		private void RestoreSidebar ()
		{
			var cc = App.CurrentUserConfig;
			cc.IsMinimum = false;
			ClearValue (SizeToContentProperty);
			SidebarView.Visibility = Visibility.Visible;
			TaskbarView.Visibility = Visibility.Collapsed;
			Visibility = Visibility.Visible;
			Width = cc.Width;
			TaskbarTiles.ItemsSource = null;
			if (cc.OccupyWorkingArea) appBar.Register ();
			RefreshWndPos ();
			ReshowSidebarView ();
		}
		private void RefreshTaskbarTileItems ()
		{
			if (!App.CurrentUserConfig.IsMinimum) return;
			taskbarTilesList?.Refresh ();
		}
		#endregion
		#region Position
		private void RefreshWndPos ()
		{
			if (!isSourceInitialized) return;
			var cc = App.CurrentUserConfig;
			if (cc.IsMinimum)
			{
				var tbrect = ExplorerShell.GetTaskbarRect ();
				var tbdirect = ExplorerShell.GetTaskbarEdge ();
				switch (tbdirect)
				{
					case Win32.AppBarEdge.Bottom:
					case Win32.AppBarEdge.Top:
						var l = tbrect.Right - this.GetPixelWidth ();
						var t = tbrect.Top;
						HWnd.SetPosition (l, t, null, tbrect.Height);
						break;
				}
			}
			else
			{
				if (cc.OccupyWorkingArea && appBar.IsRegistered) appBar.SizeAppBar ();
				else appBar.SetPos ();
			}
		}
		private void RestoreButton_Click (object sender, RoutedEventArgs e)
		{
			RestoreSidebar ();
		}
		#endregion
		#region Scroll Tiles Container
		private void ApplyScrollMode (bool canscroll)
		{
			var PinnedTilesScrollRegion = ScrollPinnedTilesControl;
			var PinnedTilesRegion = PinnedTilesControl;
			var TilesScrollRegion = ScrollTilesControl;
			var TilesRegion = TilesControl;
			if (canscroll)
			{
				if (PinnedTilesScrollRegion.Content != PinnedTilesRegion)
				{
					var parent = PinnedTilesScrollRegion.Parent as Panel;
					parent.Children.Remove (PinnedTilesRegion);
					PinnedTilesScrollRegion.Content = PinnedTilesRegion;
				}
				if (TilesScrollRegion.Content != TilesRegion)
				{
					var parent = TilesRegion.Parent as Panel;
					parent.Children.Remove (TilesRegion);
					TilesScrollRegion.Content = TilesRegion;
				}
				PinnedTilesScrollRegion.Visibility = TilesScrollRegion.Visibility = Visibility.Visible;
			}
			else
			{
				PinnedTilesScrollRegion.Visibility = TilesScrollRegion.Visibility = Visibility.Collapsed;
				UpdateLayout ();
				if (PinnedTilesScrollRegion.Content == PinnedTilesRegion)
				{
					PinnedTilesScrollRegion.Content = null;
					var parent = PinnedTilesScrollRegion.Parent as DockPanel;
					parent.Children.Insert (parent.Children.IndexOf (PinnedTilesScrollRegion), PinnedTilesRegion);
				}
				if (TilesScrollRegion.Content == TilesRegion)
				{
					TilesScrollRegion.Content = null;
					var parent = TilesScrollRegion.Parent as DockPanel;
					parent.Children.Insert (parent.Children.IndexOf (TilesScrollRegion), TilesRegion);
				}
			}
		}
		#endregion
		#region Settings
		internal SidebarContext Context = new SidebarContext ();
		private void CurrentUserConfig_PropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var cuc = App.CurrentUserConfig;
			switch (e.PropertyName)
			{
				case nameof (cuc.Direction):
					Context.Direction = cuc.Direction;
					foreach (var kv in tileCache)
						kv.Value?.Instance?.Instance?.OnHostChanged (TileHostEvent.SidebarDirectionChanged, Context);
					if (appBar != null && appBar.IsRegistered) appBar.Direction = cuc.Direction;
					if (taskbarHelper != null) taskbarHelper.Direction = cuc.Direction;
					RefreshWndPos ();
					break;
				case nameof (cuc.Width):
					Width = Math.Max (MinWidth, Math.Min (cuc.Width, MaxWidth));
					RefreshWndPos ();
					break;
				case nameof (cuc.AutoRun):
					if (StartupManager.IsEnabled () != cuc.AutoRun)
					{
						if (cuc.AutoRun) StartupManager.Enable ();
						else StartupManager.Disable ();
					}
					break;
				case nameof (cuc.CanScroll):
					ApplyScrollMode (cuc.CanScroll);
					RebuildOverflowList ();
					break;
				case nameof (cuc.Screen):
					RefreshWndPos ();
					break;
				case nameof (cuc.OccupyWorkingArea):
					if (!isSourceInitialized) return;
					if (cuc.OccupyWorkingArea) appBar.Register ();
					else appBar.Unregister ();
					RefreshWndPos ();
					break;
				case nameof (cuc.OverlapTaskbar):
					if (!isSourceInitialized) return;
					if (cuc.OverlapTaskbar) taskbarHelper.Register ();
					else taskbarHelper.Unregister ();
					RefreshWndPos ();
					if (cuc.IsMinimum) MinimumSidebar ();
					break;
				case nameof (cuc.Topmost):
					Topmost = cuc.Topmost;
					break;
			}
		}
		private void InitSettings ()
		{
			var cc = App.CurrentUserConfig;
			Width = cc.Width;
			ApplyScrollMode (cc.CanScroll);
			UpdateLayout ();
			RefreshWndPos ();
			if (cc.OccupyWorkingArea & !cc.IsMinimum) appBar.Register ();
			RefreshWndPos ();
			if (cc.OverlapTaskbar) taskbarHelper.Register ();
			RefreshWndPos ();
			Topmost = cc.Topmost;
			if (cc.IsMinimum) MinimumSidebar ();
		}
		#endregion
		#region Notify Icon
		private NotifyIcon notifyIcon = null;
		private FormContextMenu trayMenu = null;
		private void InitNotifyIcon ()
		{
			trayMenu = new FormContextMenu ();
			notifyIcon = new NotifyIcon ();
			notifyIcon.ContextMenu = trayMenu;
			#region Init Tray Menu
			var displayItem = new FormMenuItem ();
			var propItem = new FormMenuItem ();
			var exitItem = new FormMenuItem ();
			displayItem.Text = App.ProgramFolder.StringResources.SuitableResource ("SIDEBAR_CONTEXTMENU_SHOW");
			displayItem.Checked = !App.CurrentUserConfig.IsMinimum;
			displayItem.Click += TrayMenuItemDisplay_Click;
			propItem.Text = App.ProgramFolder.StringResources.SuitableResource ("SIDEBAR_CONTEXTMENU_PROP");
			propItem.Click += TrayMenuItemProperties_Click;
			exitItem.Text = App.ProgramFolder.StringResources.SuitableResource ("SIDEBAR_CONTEXTMENU_CANCEL");
			exitItem.Click += TrayMenuItemExit_Click;
			trayMenu.MenuItems.Add (displayItem);
			trayMenu.MenuItems.Add (propItem);
			trayMenu.MenuItems.Add ("-");
			trayMenu.MenuItems.Add (exitItem);
			#endregion
			notifyIcon.Icon = Properties.Resources.AppIcon;
			notifyIcon.Text = Title;
			notifyIcon.Visible = true;
			notifyIcon.MouseDoubleClick += NotifyIcon_MouseDoubleClick;
			notifyIcon.ContextMenu.Popup += TrayMenu_Popup;
		}
		private void TrayMenu_Popup (object sender, EventArgs e)
		{
			var displayItem = (sender as FormContextMenu).MenuItems [0];
			displayItem.Checked = !App.CurrentUserConfig.IsMinimum;
		}
		private void TrayMenuItemExit_Click (object sender, EventArgs e)
		{
			Close ();
		}
		private void TrayMenuItemProperties_Click (object sender, EventArgs e)
		{
			if (sidebarConfWnd != null) sidebarConfWnd.Activate ();
			else
			{
				sidebarConfWnd = new ConfigWindow ();
				sidebarConfWnd.Closed += SidebarConfWnd_Closed;
				sidebarConfWnd.Show ();
			}
		}
		private void TrayMenuItemDisplay_Click (object sender, EventArgs e)
		{
			if (App.CurrentUserConfig.IsMinimum) RestoreSidebar ();
			else MinimumSidebar ();
		}
		private void NotifyIcon_MouseDoubleClick (object sender, System.Windows.Forms.MouseEventArgs e)
		{
			RestoreSidebar ();
			Activate ();
		}
		#endregion
		#region Resize
		private const int WM_SYSCOMMAND = 0x0112;
		private const int SC_SIZE_LEFT = 0xF001;
		private const int SC_SIZE_RIGHT = 0xF002;
		private const double ResizeHotZone = 5.0;
		private const double DefaultWidth = 150;
		private void Window_MouseMove (object sender, MouseEventArgs e)
		{
			var cc = App.CurrentUserConfig;
			if (cc.IsMinimum) return;
			if (cc.Locked) return;
			double x = e.GetPosition (this).X;
			switch (cc.Direction)
			{
				case SidebarDirection.Right:
					if (x <= ResizeHotZone)
					{
						base.Cursor = Cursors.SizeWE;
						if (e.LeftButton == MouseButtonState.Pressed)
						{
							Win32WindowNative.SendMessageW (Handle, WM_SYSCOMMAND, (IntPtr)SC_SIZE_LEFT, IntPtr.Zero);
							cc.Width = (int)Math.Round (Width);
						}
					}
					else if (base.Cursor != Cursors.Arrow)
						base.Cursor = Cursors.Arrow;
					break;
				case SidebarDirection.Left:
					if (x >= this.Width - ResizeHotZone)
					{
						base.Cursor = Cursors.SizeWE;
						if (e.LeftButton == MouseButtonState.Pressed)
						{
							Win32WindowNative.SendMessageW (Handle, WM_SYSCOMMAND, (IntPtr)SC_SIZE_RIGHT, IntPtr.Zero);
							cc.Width = (int)Math.Round (Width);
						}
					}
					else if (base.Cursor != Cursors.Arrow)
						base.Cursor = Cursors.Arrow;
					break;
			}
		}
		private void Window_MouseDoubleClick (object sender, MouseButtonEventArgs e)
		{
			var cc = App.CurrentUserConfig;
			if (cc.IsMinimum || cc.Locked) return;
			double x = e.GetPosition (this).X;
			switch (cc.Direction)
			{
				case SidebarDirection.Right:
					if (x <= ResizeHotZone) cc.Width = (int)DefaultWidth;
					break;
				case SidebarDirection.Left:
					if (x >= this.Width - ResizeHotZone) cc.Width = (int)DefaultWidth;
					break;
			}
		}
		private void UpdateMinMaxWidth ()
		{
			if (App.CurrentUserConfig == null) return;
			var screen = App.CurrentUserConfig.CurrentScreen;
			if (screen == null) return;
			int screenWidthPx = screen.WorkingArea.Width;
			int maxWidthPx = Math.Max (200, (int)(screenWidthPx * 0.45));
			var source = PresentationSource.FromVisual (this);
			double dpiScale = 1.0;
			if (source?.CompositionTarget != null) dpiScale = source.CompositionTarget.TransformToDevice.M11;
			double minWidthDip = 100;
			double maxWidthDip = maxWidthPx / dpiScale;
			this.MinWidth = minWidthDip;
			this.MaxWidth = maxWidthDip;
		}
		#endregion
		#region System Events
		private void InitSystemEventHandlers ()
		{
			ScreenChangeNotifier.ScaleChanged += ScreenChangeNotifier_ScaleChanged;
			ScreenChangeNotifier.SizeChanged += ScreenChangeNotifier_SizeChanged;
			ScreenChangeNotifier.WorkingAreaHeightChanged += ScreenChangeNotifier_WorkingAreaHeightChanged;
			ScreenChangeNotifier.WorkingAreaChanged += ScreenChangeNotifier_WorkingAreaChanged;
			ScreenChangeNotifier.StartListening (this);
			ExplorerShell.TaskbarPositionChanged += ExplorerShell_TaskbarPositionChanged;
			ExplorerShell.StartTaskbarMonitoring ();
		}
		private void ExplorerShell_TaskbarPositionChanged (object sender, EventArgs e)
		{
			var cc = App.CurrentUserConfig;
			if (cc.OverlapTaskbar && cc.IsMinimum)
			{
				RefreshWndPos ();
				taskbarPosTimer.Tick -= TaskbarPosTimer_Tick;
				taskbarPosTimer.Tick += TaskbarPosTimer_Tick;
				taskbarPosTimer.Stop ();
				taskbarPosTimer.Start ();
			}
		}
		private void TaskbarPosTimer_Tick (object sender, EventArgs e)
		{
			try
			{
				taskbarPosTimer.Stop ();
				taskbarPosTimer.Tick -= TaskbarPosTimer_Tick;
				RefreshWndPos ();
			}
			catch { }
		}
		private void ScreenChangeNotifier_WorkingAreaChanged (object sender, EventArgs e)
		{
			var cc = App.CurrentUserConfig;
			if (!cc.OccupyWorkingArea) RefreshWndPos ();
		}
		private void ScreenChangeNotifier_WorkingAreaHeightChanged (object sender, EventArgs e)
		{
			var cc = App.CurrentUserConfig;
			if (cc.OccupyWorkingArea && !cc.OverlapTaskbar) RefreshWndPos ();
		}
		private void ScreenChangeNotifier_SizeChanged (object sender, EventArgs e)
		{
			UpdateMinMaxWidth ();
			RefreshWndPos ();
		}
		private void ScreenChangeNotifier_ScaleChanged (object sender, EventArgs e)
		{
			RefreshWndPos ();
		}
		private void DestroySystemEventHandlers ()
		{
			ScreenChangeNotifier.StopListening ();
			ScreenChangeNotifier.ScaleChanged -= ScreenChangeNotifier_ScaleChanged;
			ScreenChangeNotifier.SizeChanged -= ScreenChangeNotifier_SizeChanged;
			ScreenChangeNotifier.WorkingAreaHeightChanged -= ScreenChangeNotifier_WorkingAreaHeightChanged;
			ScreenChangeNotifier.WorkingAreaChanged -= ScreenChangeNotifier_WorkingAreaChanged;
			ExplorerShell.StopTaskbarMonitoring ();
			ExplorerShell.TaskbarPositionChanged -= ExplorerShell_TaskbarPositionChanged;
		}
		#endregion
		#region Tile Drag/Drop
		private TileDragManager _tileDragManager;
		/// <summary>是否正在处理磁贴拖拽（含待拖状态）；拖拽期间会抑制 TileOverflow 相关逻辑。</summary>
		public bool IsTileDragging { get { return _tileDragManager != null && (_tileDragManager.IsDragging || _tileDragManager.IsPending); } }
		/// <summary>磁贴拖拽管理器，供 TileControl 在手势中访问。</summary>
		internal TileDragManager DragManager { get { return _tileDragManager; } }
		/// <summary>初始化磁贴拖拽重排。</summary>
		private void InitTileDragDrop ()
		{
			if (_tileDragManager != null) return;
			_tileDragManager = new TileDragManager (this);
			PreviewMouseMove += TileDragDrop_PreviewMouseMove;
			PreviewMouseLeftButtonUp += TileDragDrop_PreviewMouseLeftButtonUp;
			PreviewStylusMove += TileDragDrop_PreviewStylusMove;
			PreviewStylusUp += TileDragDrop_PreviewStylusUp;
			PreviewTouchMove += TileDragDrop_PreviewTouchMove;
			PreviewTouchUp += TileDragDrop_PreviewTouchUp;
			PreviewKeyDown += TileDragDrop_PreviewKeyDown;
		}
		/// <summary>卸载磁贴拖拽重排。</summary>
		private void DestroyTileDragDrop ()
		{
			if (_tileDragManager == null) return;
			_tileDragManager.Cancel ();
			PreviewMouseMove -= TileDragDrop_PreviewMouseMove;
			PreviewMouseLeftButtonUp -= TileDragDrop_PreviewMouseLeftButtonUp;
			PreviewStylusMove -= TileDragDrop_PreviewStylusMove;
			PreviewStylusUp -= TileDragDrop_PreviewStylusUp;
			PreviewTouchMove -= TileDragDrop_PreviewTouchMove;
			PreviewTouchUp -= TileDragDrop_PreviewTouchUp;
			PreviewKeyDown -= TileDragDrop_PreviewKeyDown;
			_tileDragManager = null;
		}
		private void TileDragDrop_PreviewMouseMove (object sender, MouseEventArgs e)
		{
			if (IsTileDragging) _tileDragManager.OnMouseMove (e);
		}
		private void TileDragDrop_PreviewMouseLeftButtonUp (object sender, MouseButtonEventArgs e)
		{
			if (IsTileDragging) _tileDragManager.OnMouseUp (e);
		}
		private void TileDragDrop_PreviewStylusMove (object sender, StylusEventArgs e)
		{
			if (IsTileDragging) _tileDragManager.OnStylusMove (e);
		}
		private void TileDragDrop_PreviewStylusUp (object sender, StylusEventArgs e)
		{
			if (IsTileDragging) _tileDragManager.OnStylusUp (e);
		}
		private void TileDragDrop_PreviewTouchMove (object sender, TouchEventArgs e)
		{
			if (IsTileDragging) _tileDragManager.OnTouchMove (e);
		}
		private void TileDragDrop_PreviewTouchUp (object sender, TouchEventArgs e)
		{
			if (IsTileDragging) _tileDragManager.OnTouchUp (e);
		}
		private void TileDragDrop_PreviewKeyDown (object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape && IsTileDragging)
			{
				_tileDragManager.Cancel ();
				e.Handled = true;
			}
		}
		private bool _suppressTileRefresh;
		/// <summary>把磁贴移动到指定面板的指定索引处；内部走 list.Move，由原有重建流程负责后续布局。</summary>
		public bool MoveTileToPanelIndex (string userModelId, Panel panel, int panelIndex)
		{
			if (string.IsNullOrWhiteSpace (userModelId)) return false;
			SidebarConfig cc = App.CurrentUserConfig;
			if (cc == null) return false;
			ObservableCollection<string> list;
			if (ReferenceEquals (panel, TilesControl)) list = cc.PinnedTiles;
			else if (ReferenceEquals (panel, PinnedTilesControl)) list = cc.PinnedBottomTiles;
			else return false;
			int srcList = IndexOfFamilyName (list, userModelId);
			if (srcList < 0) return false;
			if (panelIndex < 0) panelIndex = 0;
			if (panelIndex > list.Count - 1) panelIndex = list.Count - 1;
			if (panelIndex == srcList) return false;
			list.Move (srcList, panelIndex);
			return true;
		}
		bool ITileReorderHost.MoveTileToPanelIndex (string userModelId, Panel panel, int panelIndex)
		{
			return MoveTileToPanelIndex (userModelId, panel, panelIndex);
		}
		#endregion
		#region Flyout Position
		private static double Range (double value, double min, double max) => Math.Min (Math.Max (value, min), max);
		private static int Range (int value, int min, int max) => Math.Min (Math.Max (value, min), max);
		public void FixFlyoutPosition (FlyoutWindow fwnd)
		{
			try
			{
				if (fwnd == null) return;
				var tc = fwnd.Tile;
				var umid = tc.UserModelId;
				var cs = App.CurrentUserConfig.CurrentScreen;
				var isminimum = App.CurrentUserConfig.IsMinimum;
				var isoverflow = IsTileOverflow (fwnd.Tile);
				var isoverlap = App.CurrentUserConfig.OverlapTaskbar;
				if (isminimum && isoverlap)
				{
					var btnindex = 0;
					var taskbartiles = TaskbarTiles.ItemsSource as IList<ITileElement>;
					for (; btnindex < (taskbartiles?.Count ?? 0); btnindex++)
					{
						var item = taskbartiles [btnindex];
						if (item.UserModelId == umid) break;
					}
					if (btnindex + 1 > (taskbartiles?.Count ?? 0)) btnindex = (taskbartiles?.Count ?? 0) - 1;
					var container = TaskbarTiles.ItemContainerGenerator.ContainerFromIndex (btnindex) as FrameworkElement;
					var pixelpos = container.GetTopLeftInPhysicalPixels ();
					var taskbarpos = ExplorerShell.GetTaskbarEdge ();
					switch (taskbarpos)
					{
						case Win32.AppBarEdge.Top:
							pixelpos.Y += container.GetPhysicalSize ().Height;
							break;
						case Win32.AppBarEdge.Bottom:
							pixelpos.Y -= fwnd.GetPixelHeight ();
							break;
					}
					pixelpos.X = Range (pixelpos.X, cs.Bounds.Left, cs.Bounds.Right - fwnd.GetPixelWidth ());
					var fHandle = fwnd.HWnd;
					fHandle.Move ((int)pixelpos.X, (int)pixelpos.Y);
					return;
				}
				else if (isoverflow)
				{
					var umidLocal = tc?.UserModelId;
					if (string.IsNullOrWhiteSpace (umidLocal)) return;
					if (System.Windows.PresentationSource.FromVisual (fwnd) == null) return;
					var overflowtiles = OverflowTilesControl.Items;
					var btnIndex = 0;
					for (; btnIndex < (overflowtiles?.Count ?? 0); btnIndex++)
					{
						var item = overflowtiles [btnIndex] as ITileElement;
						if (item != null && item.UserModelId != null && item.UserModelId.NEquals (umidLocal)) break;
					}
					if (btnIndex + 1 > (overflowtiles?.Count ?? 0)) btnIndex = (overflowtiles?.Count ?? 0) - 1;
					if (btnIndex < 0) return;
					var container = OverflowTilesControl.ItemContainerGenerator.ContainerFromIndex (btnIndex) as FrameworkElement;
					if (container == null) return;
					var pixelpos = container.GetTopLeftInPhysicalPixels ();
					var pixelsize = container.GetPhysicalSize ();
					if (double.IsNaN (pixelpos.X) || double.IsNaN (pixelpos.Y)) return;
					if (double.IsNaN (pixelsize.Height) || pixelsize.Height <= 0) return;
					double flyoutHeight = fwnd.GetPixelHeight ();
					double flyoutWidth = fwnd.GetPixelWidth ();
					if (double.IsNaN (flyoutHeight) || flyoutHeight <= 0) return;
					if (double.IsNaN (flyoutWidth) || flyoutWidth <= 0) return;
					var wa = cs.WorkingArea;
					double btnTop = pixelpos.Y;
					double btnBottom = pixelpos.Y + pixelsize.Height;
					double topSpace = btnTop - (wa.Top + 4);
					double bottomSpace = (wa.Bottom - 4) - btnBottom;
					if (bottomSpace >= flyoutHeight)
						pixelpos.Y = btnBottom;
					else if (topSpace >= flyoutHeight)
						pixelpos.Y = btnTop - flyoutHeight;
					else if (bottomSpace >= topSpace)
					{
						pixelpos.Y = btnBottom;
						if (pixelpos.Y + flyoutHeight > wa.Bottom) pixelpos.Y = wa.Bottom - flyoutHeight;
					}
					else
					{
						pixelpos.Y = btnTop - flyoutHeight;
						if (pixelpos.Y < wa.Top) pixelpos.Y = wa.Top;
					}
					pixelpos.X = Range (pixelpos.X, wa.Left, wa.Right - flyoutWidth);
					var fHandle = fwnd.HWnd;
					fHandle.Move ((int)pixelpos.X, (int)pixelpos.Y);
				}
				else
				{
					var cc = App.CurrentUserConfig;
					var btnPos = tc.GetTopLeftInPhysicalPixels ();
					double flyoutWidth = fwnd.GetPixelWidth ();
					double flyoutHeight = fwnd.GetPixelHeight ();
					const int H_MARGIN = 9;
					const int V_MARGIN = 5;
					var hArea = cc.OccupyWorkingArea ? cs.Bounds : cs.WorkingArea;
					var vArea = cc.OverlapTaskbar ? cs.Bounds : cs.WorkingArea;
					double flyoutLeft = this.GetPixelLeft () + H_MARGIN;
					if (flyoutLeft + flyoutWidth > hArea.Right - H_MARGIN)
						flyoutLeft = hArea.Right - H_MARGIN - flyoutWidth;
					if (flyoutLeft < hArea.Left + H_MARGIN)
						flyoutLeft = hArea.Left + H_MARGIN;
					double flyoutTop = btnPos.Y;
					if (flyoutTop + flyoutHeight > vArea.Bottom - V_MARGIN)
						flyoutTop = vArea.Bottom - V_MARGIN - flyoutHeight;
					if (flyoutTop < vArea.Top + V_MARGIN)
						flyoutTop = vArea.Top + V_MARGIN;
					var fHandle = fwnd.HWnd;
					fHandle.Move ((int)flyoutLeft, (int)flyoutTop);
				}
			}
			catch { }
		}
		#endregion
		#region Taskbar View Context Menu
		private const int WM_CONTEXTMENU = 0x007B;
		private void InitTaskbarViewContextMenu ()
		{
			TaskbarView.PreviewMouseRightButtonDown -= TaskbarView_PreviewMouseRightButtonDown;
			TaskbarView.PreviewMouseRightButtonDown += TaskbarView_PreviewMouseRightButtonDown;
			TaskbarView.PreviewMouseRightButtonUp -= TaskbarView_PreviewMouseRightButtonUp;
			TaskbarView.PreviewMouseRightButtonUp += TaskbarView_PreviewMouseRightButtonUp;
			TaskbarView.PreviewStylusButtonDown -= TaskbarView_PreviewStylusButtonDown;
			TaskbarView.PreviewStylusButtonDown += TaskbarView_PreviewStylusButtonDown;
			TaskbarView.PreviewStylusButtonUp -= TaskbarView_PreviewStylusButtonUp;
			TaskbarView.PreviewStylusButtonUp += TaskbarView_PreviewStylusButtonUp;
			TaskbarView.PreviewStylusSystemGesture -= TaskbarView_PreviewStylusSystemGesture;
			TaskbarView.PreviewStylusSystemGesture += TaskbarView_PreviewStylusSystemGesture;
			Stylus.SetIsPressAndHoldEnabled (TaskbarView, true);
		}
		private void DestroyTaskbarViewContextMenu ()
		{
			TaskbarView.PreviewMouseRightButtonDown -= TaskbarView_PreviewMouseRightButtonDown;
			TaskbarView.PreviewMouseRightButtonUp -= TaskbarView_PreviewMouseRightButtonUp;
			TaskbarView.PreviewStylusButtonDown -= TaskbarView_PreviewStylusButtonDown;
			TaskbarView.PreviewStylusButtonUp -= TaskbarView_PreviewStylusButtonUp;
			TaskbarView.PreviewStylusSystemGesture -= TaskbarView_PreviewStylusSystemGesture;
		}
		private void TaskbarView_PreviewMouseRightButtonDown (object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Right) return;
			if (IsInsideElement (e.OriginalSource as DependencyObject, TaskbarTiles)) return;
			e.Handled = true;
			if (e.StylusDevice != null) return;
			ShowTaskbarShellContextMenu (e.GetPosition (TaskbarView));
		}
		private void TaskbarView_PreviewMouseRightButtonUp (object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Right) return;
			if (IsInsideElement (e.OriginalSource as DependencyObject, TaskbarTiles)) return;
			e.Handled = true;
		}
		private void TaskbarView_PreviewStylusButtonDown (object sender, StylusButtonEventArgs e)
		{
			if (!IsBarrelButton (e.StylusButton)) return;
			if (IsInsideElement (e.OriginalSource as DependencyObject, TaskbarTiles)) return;
			e.Handled = true;
			ShowTaskbarShellContextMenu (e.GetPosition (TaskbarView));
		}
		private void TaskbarView_PreviewStylusButtonUp (object sender, StylusButtonEventArgs e)
		{
			if (!IsBarrelButton (e.StylusButton)) return;
			if (IsInsideElement (e.OriginalSource as DependencyObject, TaskbarTiles)) return;
			e.Handled = true;
		}
		private void TaskbarView_PreviewStylusSystemGesture (object sender, StylusSystemGestureEventArgs e)
		{
			if (e.SystemGesture != SystemGesture.RightTap) return;
			if (IsInsideElement (e.OriginalSource as DependencyObject, TaskbarTiles)) return;
			e.Handled = true;
			ShowTaskbarShellContextMenu (e.GetPosition (TaskbarView));
		}
		private static bool IsInsideElement (DependencyObject src, DependencyObject target)
		{
			DependencyObject cur = src;
			while (cur != null)
			{
				if (ReferenceEquals (cur, target)) return true;
				cur = GetParentSafe (cur);
			}
			return false;
		}
		private static DependencyObject GetParentSafe (DependencyObject cur)
		{
			if (cur == null) return null;
			if (cur is Visual) return VisualTreeHelper.GetParent (cur);
			if (cur is System.Windows.Media.Media3D.Visual3D) return VisualTreeHelper.GetParent (cur);
			return LogicalTreeHelper.GetParent (cur);
		}
		private void ShowTaskbarShellContextMenu (Point ptInTaskbarView)
		{
			IntPtr trayWnd = Win32WindowNative.FindWindow ("Shell_TrayWnd", null);
			if (trayWnd == IntPtr.Zero) return;
			Point screenPos = TaskbarView.PointToScreen (ptInTaskbarView);
			if (double.IsNaN (screenPos.X) || double.IsNaN (screenPos.Y)) return;
			int sx = (int)Math.Round (screenPos.X);
			int sy = (int)Math.Round (screenPos.Y);
			var tbrect = ExplorerShell.GetTaskbarRect ();
			if (sx < tbrect.Left || sx >= tbrect.Right || sy < tbrect.Top || sy >= tbrect.Bottom) return;
			IntPtr lParam = (IntPtr)(((sy & 0xFFFF) << 16) | (sx & 0xFFFF));
			Win32WindowNative.SendMessageW (trayWnd, WM_CONTEXTMENU, trayWnd, lParam);
		}
		#endregion
		#region ItemsControl Tile Interaction
		private bool _stylusBarrelMenuShown = false;
		private void AttachTileInteraction (ItemsControl ic)
		{
			if (ic == null) return;
			DetachTileInteraction (ic);
			Stylus.SetIsPressAndHoldEnabled (ic, true);
			ic.AddHandler (System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler (ItemsControl_TileButtonClick), true);
			ic.PreviewMouseRightButtonDown += ItemsControl_TilePreviewMouseRightDown;
			ic.PreviewStylusButtonDown += ItemsControl_TilePreviewStylusButtonDown;
			ic.PreviewStylusButtonUp += ItemsControl_TilePreviewStylusButtonUp;
			ic.PreviewStylusSystemGesture += ItemsControl_TilePreviewStylusSystemGesture;
		}
		private void DetachTileInteraction (ItemsControl ic)
		{
			if (ic == null) return;
			ic.RemoveHandler (System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler (ItemsControl_TileButtonClick));
			ic.PreviewMouseRightButtonDown -= ItemsControl_TilePreviewMouseRightDown;
			ic.PreviewStylusButtonDown -= ItemsControl_TilePreviewStylusButtonDown;
			ic.PreviewStylusButtonUp -= ItemsControl_TilePreviewStylusButtonUp;
			ic.PreviewStylusSystemGesture -= ItemsControl_TilePreviewStylusSystemGesture;
		}
		private static TileControl ResolveTileFromItemsControl (ItemsControl ic, DependencyObject src)
		{
			if (ic == null || src == null) return null;
			var container = ic.ContainerFromElement (src) as FrameworkElement;
			if (container == null) return null;
			object item;
			try { item = ic.ItemContainerGenerator.ItemFromContainer (container); }
			catch { return null; }
			if (item == null || ReferenceEquals (item, DependencyProperty.UnsetValue)) return null;
			if (!(item is TileVisualView)) return null;
			return ((TileVisualView)item).Element;
		}
		private static bool IsBarrelButton (StylusButton btn)
		{
			if (btn == null) return false;
			try { return btn.Guid == StylusPointProperties.BarrelButton.Id; }
			catch { return false; }
		}
		private static void OpenFlyoutOf (TileControl tc)
		{
			tc?.RequireOpenFlyout ();
		}
		private static void ShowContextMenuOf (TileControl tc, FrameworkElement target, System.Windows.Controls.Primitives.PlacementMode placementMode, TileContextMenuSource source)
		{
			if (tc == null || target == null) return;
			var cm = tc.TileContextMenu;
			if (cm == null) return;
			cm.PlacementTarget = target;
			cm.Placement = placementMode;
			cm.FlowDirection = System.Windows.FlowDirection.LeftToRight;
			tc.ContextMenuSource = source;
			cm.IsOpen = true;
		}
		private TileContextMenuSource ResolveContextMenuSource (ItemsControl ic)
		{
			if (ReferenceEquals (ic, TaskbarTiles)) return TileContextMenuSource.Taskbar;
			if (ReferenceEquals (ic, OverflowTilesControl)) return TileContextMenuSource.Overflow;
			return TileContextMenuSource.Normal;
		}
		private void ItemsControl_TileButtonClick (object sender, RoutedEventArgs e)
		{
			var ic = sender as ItemsControl;
			if (ic == null) return;
			var tc = ResolveTileFromItemsControl (ic, e.OriginalSource as DependencyObject);
			if (tc == null) return;
			OpenFlyoutOf (tc);
			e.Handled = true;
		}
		private void ItemsControl_TilePreviewMouseRightDown (object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Right) return;
			if (e.StylusDevice != null) return;
			var ic = sender as ItemsControl;
			var src = e.OriginalSource as DependencyObject;
			var tc = ResolveTileFromItemsControl (ic, src);
			if (tc == null) return;
			var container = ic.ContainerFromElement (src) as FrameworkElement;
			ShowContextMenuOf (tc, container, System.Windows.Controls.Primitives.PlacementMode.MousePoint, ResolveContextMenuSource (ic));
			e.Handled = true;
		}
		private void ItemsControl_TilePreviewStylusButtonDown (object sender, StylusButtonEventArgs e)
		{
			if (!IsBarrelButton (e.StylusButton)) return;
			var ic = sender as ItemsControl;
			var src = e.OriginalSource as DependencyObject;
			var tc = ResolveTileFromItemsControl (ic, src);
			if (tc == null) return;
			var container = ic.ContainerFromElement (src) as FrameworkElement;
			ShowContextMenuOf (tc, container, System.Windows.Controls.Primitives.PlacementMode.Bottom, ResolveContextMenuSource (ic));
			_stylusBarrelMenuShown = true;
			e.Handled = true;
		}
		private void ItemsControl_TilePreviewStylusButtonUp (object sender, StylusButtonEventArgs e)
		{
			if (!IsBarrelButton (e.StylusButton)) return;
			_stylusBarrelMenuShown = false;
			e.Handled = true;
		}
		private void ItemsControl_TilePreviewStylusSystemGesture (object sender, StylusSystemGestureEventArgs e)
		{
			if (e.SystemGesture != SystemGesture.RightTap) return;
			var ic = sender as ItemsControl;
			var src = e.OriginalSource as DependencyObject;
			var tc = ResolveTileFromItemsControl (ic, src);
			if (tc == null) return;
			var container = ic.ContainerFromElement (src) as FrameworkElement;
			ShowContextMenuOf (tc, container, System.Windows.Controls.Primitives.PlacementMode.Bottom, ResolveContextMenuSource (ic));
			e.Handled = true;
		}
		#endregion
		#region Aero Peek
		private bool _livePreviewActive = false;
		private void InitAeroPeek ()
		{
			if (Handle != IntPtr.Zero)
			{
				try { DWMAPI.RemoveFromAeroPeek (Handle); } catch { }
			}
			if (!DWMAPI.IsAeroPeekAvailable ()) return;
			AeroPeekButton.MouseEnter -= AeroPeekButton_MouseEnter;
			AeroPeekButton.MouseEnter += AeroPeekButton_MouseEnter;
			AeroPeekButton.MouseLeave -= AeroPeekButton_MouseLeave;
			AeroPeekButton.MouseLeave += AeroPeekButton_MouseLeave;
			AeroPeekButton.TouchDown -= AeroPeekButton_TouchDown;
			AeroPeekButton.TouchDown += AeroPeekButton_TouchDown;
			AeroPeekButton.TouchUp -= AeroPeekButton_TouchUp;
			AeroPeekButton.TouchUp += AeroPeekButton_TouchUp;
			AeroPeekButton.StylusDown -= AeroPeekButton_StylusDown;
			AeroPeekButton.StylusDown += AeroPeekButton_StylusDown;
			AeroPeekButton.StylusUp -= AeroPeekButton_StylusUp;
			AeroPeekButton.StylusUp += AeroPeekButton_StylusUp;
		}
		private void DestroyAeroPeek ()
		{
			AeroPeekButton.MouseEnter -= AeroPeekButton_MouseEnter;
			AeroPeekButton.MouseLeave -= AeroPeekButton_MouseLeave;
			AeroPeekButton.TouchDown -= AeroPeekButton_TouchDown;
			AeroPeekButton.TouchUp -= AeroPeekButton_TouchUp;
			AeroPeekButton.StylusDown -= AeroPeekButton_StylusDown;
			AeroPeekButton.StylusUp -= AeroPeekButton_StylusUp;
			DeactivateLivePreviewCore ();
		}
		private void ActivateLivePreviewCore ()
		{
			if (_livePreviewActive) return;
			if (Handle == IntPtr.Zero) return;
			_livePreviewActive = DWMAPI.ActivateLivePreview (Handle, Handle);
		}
		private void DeactivateLivePreviewCore ()
		{
			if (!_livePreviewActive) return;
			try { DWMAPI.DeactivateLivePreview (Handle, Handle); } catch { }
			_livePreviewActive = false;
		}
		private static bool IsTouchStylus (StylusDevice stylus)
		{
			return stylus != null
				&& stylus.TabletDevice != null
				&& stylus.TabletDevice.Type == TabletDeviceType.Touch;
		}
		private void AeroPeekButton_MouseEnter (object sender, MouseEventArgs e)
		{
			if (e.StylusDevice != null) return;
			ActivateLivePreviewCore ();
		}
		private void AeroPeekButton_MouseLeave (object sender, MouseEventArgs e)
		{
			if (e.StylusDevice != null) return;
			DeactivateLivePreviewCore ();
		}
		private void AeroPeekButton_TouchDown (object sender, TouchEventArgs e)
		{
			ActivateLivePreviewCore ();
		}
		private void AeroPeekButton_TouchUp (object sender, TouchEventArgs e)
		{
			DeactivateLivePreviewCore ();
		}
		private void AeroPeekButton_StylusDown (object sender, StylusEventArgs e)
		{
			if (IsTouchStylus (e.StylusDevice)) return;
			ActivateLivePreviewCore ();
		}
		private void AeroPeekButton_StylusUp (object sender, StylusEventArgs e)
		{
			if (IsTouchStylus (e.StylusDevice)) return;
			DeactivateLivePreviewCore ();
		}
		private void AeroPeekButton_Click (object sender, RoutedEventArgs e)
		{
			DeactivateLivePreviewCore ();
			ExplorerShell.ToggleDesktop ();
		}
		#endregion
		#region Pipe Server
		private void InitPipeServerSubscribe ()
		{
			SidebarPipe.Mail += PipeServer_Mailed;
			SidebarPipe.StartServer ();
		}
		private void DestroyPipeServerSubscribe ()
		{
			SidebarPipe.StopServer ();
			SidebarPipe.Mail -= PipeServer_Mailed;
		}
		private void PipeServer_Mailed (string name, object data, Type dataType)
		{
			TimeoutExtensions.SetTimeout (this, () => {
				var cc = App.CurrentUserConfig;
				switch (name)
				{
					case "NotifyUnpinTile":
						{
							var familyName = data as string;
							if (string.IsNullOrWhiteSpace (familyName)) return;
							RemoveTile (familyName);
						}
						break;

					case "NotifyPinTile":
						{
							var familyName = data as string;
							if (string.IsNullOrWhiteSpace (familyName)) return;
							PinTileToTop (familyName);
						}
						break;
					case "NotifyPinTileBottom":
						{
							var familyName = data as string;
							if (string.IsNullOrWhiteSpace (familyName)) return;
							PinTileToBottom (familyName);
						}
						break;
				}
			}, 200);
		}
		#endregion
		#region Theme
		private void InitSidebarTheme ()
		{
			var tmgr = App.ThemeMgr;
			tmgr.Changed += ThemeManager_Changed;
		}
		private void ThemeManager_Changed (object sender, EventArgs e)
		{
			var tmgr = App.ThemeMgr;
			var currTheme = tmgr.CurrentUserTheme;
			Context.Theme = currTheme;
			Context.ThemeName = currTheme?.ThemeName;
			foreach (var kv in tileCache)
				kv.Value?.Instance?.Instance?.OnHostChanged (TileHostEvent.ThemeChanged, Context);
			bool enableBlur = false;
			if (Application.Current.Resources.Contains ("EnableBlur"))
				enableBlur = (bool)Application.Current.Resources ["EnableBlur"];
			ApplyAero (enableBlur);
		}
		private void DestroySidebarTheme ()
		{
			var tmgr = App.ThemeMgr;
			tmgr.Changed -= ThemeManager_Changed;
		}
		#endregion
		#region First Run
		private void InitWhenFirstRun ()
		{
			if (App.CurrentUserConfig.FirstRun)
			{
				PinTileToBottom ("WindowsModern.ClockTile_6jwq91e3ytb32!App");
				PinTileToTop ("WindowsModern.SysTrayTile_6jwq91e3ytb32!App");
				PinTileToTop ("WindowsModern.QuickLaunchTile_6jwq91e3ytb32!App");
				PinTileToTop ("WindowsModern.NotificationHistoryTile_6jwq91e3ytb32!App");
				PinTileToTop ("WindowsModern.SearchTile_6jwq91e3ytb32!App");
				App.CurrentUserConfig.FirstRun = false;
			}
		}
		#endregion
		#region First Load
		private bool isSidebarViewFirstLoad = false;
		private TimeoutHandle stateSidebarViewTimer = null;
		public bool IsSidebarViewFirstLoad
		{
			get
			{
				var elderValue = isSidebarViewFirstLoad;
				if (stateSidebarViewTimer != null && elderValue)
				{
					TimeoutExtensions.ClearTimeout (this, stateSidebarViewTimer);
					stateSidebarViewTimer = null;
				}
				stateSidebarViewTimer = TimeoutExtensions.SetTimeout (this, () => {
					isSidebarViewFirstLoad = false;
					stateSidebarViewTimer = null;
				}, 300);
				return elderValue;
			}
			set { isSidebarViewFirstLoad = value; }
		}
		private void ReshowSidebarView ()
		{
			if (IsSidebarViewFirstLoad) return;
			SidebarView.Visibility = Visibility.Hidden;
			IsSidebarViewFirstLoad = true;
			foreach (var kv in tileCache)
			{
				if (kv.Value != null)
				{
					kv.Value.IsSidebarFirstLoad = true;
					if (kv.Value.IsFirstLoad && kv.Value.IsFirstLoaded)
					{
						kv.Value.ResetFirstStatus ();
						kv.Value.InitHeight ();
					}
				}
			}
			SidebarView.Visibility = Visibility.Visible;
			if (stateSidebarViewTimer != null && isSidebarViewFirstLoad)
			{
				TimeoutExtensions.ClearTimeout (this, stateSidebarViewTimer);
				stateSidebarViewTimer = null;
			}
			stateSidebarViewTimer = TimeoutExtensions.SetTimeout (this, () => {
				isSidebarViewFirstLoad = false;
				stateSidebarViewTimer = null;
			}, 300);
		}
		#endregion
		private void Window_Loaded (object sender, RoutedEventArgs e)
		{
			UpdateMinMaxWidth ();
			InitWhenFirstRun ();
			InitSettings ();
			InitNotifyIcon ();
			InitSystemEventHandlers ();
			InitOverflowArea ();
			InitTileDragDrop ();
			InitTaskbarViewContextMenu ();
			AttachTileInteraction (TaskbarTiles);
			AttachTileInteraction (OverflowTilesControl);
			InitAeroPeek ();
			InitTaskbarTilesList ();
			InitPipeServerSubscribe ();
			App.CurrentUserConfig.PropertyChanged -= CurrentUserConfig_PropertyChanged;
			App.CurrentUserConfig.PropertyChanged += CurrentUserConfig_PropertyChanged;
			TimeoutExtensions.SetTimeout (this, () => {
				RefreshTaskbarTileItems ();
			});
			InitSidebarTheme ();
		}
		private void Window_Closed (object sender, EventArgs e)
		{
			App.Current.Shutdown ();
		}
		protected override void OnClosing (CancelEventArgs e)
		{
			appBar?.Unregister ();
			appBar?.Dispose ();
			taskbarHelper?.Unregister ();
			taskbarHelper?.Dispose ();
			DestroySidebarTheme ();
			DestroyPipeServerSubscribe ();
			DestroyAeroPeek ();
			DestroyTaskbarTilesList ();
			DetachTileInteraction (TaskbarTiles);
			DetachTileInteraction (OverflowTilesControl);
			DestroyTaskbarViewContextMenu ();
			DestroyTileDragDrop ();
			DestroyOverflowArea ();
			DestroySystemEventHandlers ();
			UninitTilesList ();
			FlushPendingRemovals ();
			App.CurrentUserConfig.PropertyChanged -= CurrentUserConfig_PropertyChanged;
			notifyIcon.Visible = false;
			notifyIcon?.Dispose ();
			notifyIcon = null;
			DestoryHwndSource ();
			Current = null;
			base.OnClosing (e);
		}
		private void Window_SizeChanged (object sender, System.Windows.SizeChangedEventArgs e)
		{
			var cc = App.CurrentUserConfig;
			if (cc.IsMinimum & cc.OverlapTaskbar) RefreshWndPos ();
		}
		private void SidebarView_Loaded (object sender, RoutedEventArgs e)
		{
			ReshowSidebarView ();
		}
	}
	public class SidebarRequestInternal: ITileRequest
	{
		private DateTime genTime = DateTime.UtcNow;
		public object RequestDatas { get; set; }
		public string RequestId
		{
			get
			{
				string raw = $"{(RequestName ?? "").NNormalize ()}|{(RequestSource ?? "").NNormalize ()}|{(RequestTarget ?? "").NNormalize ()}|{genTime.Ticks}";
				using (var sha256 = SHA256.Create ())
				{
					byte [] hash = sha256.ComputeHash (Encoding.UTF8.GetBytes (raw));
					StringBuilder sb = new StringBuilder (32);
					for (int i = 0; i < 16; i++) sb.Append (hash [i].ToString ("x2"));
					return sb.ToString ();
				}
			}
		}
		public string RequestName { get; set; }
		public string RequestSource => "Sidebar";
		public string RequestTarget { get; set; }
		public object TransferDatas { get; set; }
	}
}
