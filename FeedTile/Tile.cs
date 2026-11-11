using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Runtime.InteropServices;
using Microsoft.Feeds.Interop;
using Sidebar;
using System.IO;
using Newtonsoft.Json;

namespace WindowsModern.FeedTile
{
	public class Tile: TileBase
	{
		public static IProgramFolder TileFolder { get; private set; }
		public static ISidebarFeatures SidebarFeatures { get; private set; }
		public static IFeedsManager FeedMgr { get; set; }
		public static TileOptions Options { get; private set; }
		public static TileBase TileInstance { get; private set; }
		public static ObservableCollection<FeedData> Feeds { get; } = new ObservableCollection<FeedData> ();
		public static ObservableCollection<FeedItemData> AllItems { get; } = new ObservableCollection<FeedItemData> ();

		private TilePanel tilePanel = null;
		private Timer _refreshTimer;
		private Timer _refreshDebounceTimer;
		private CancellationTokenSource _activeRefreshCts;
		private Thread _activeRefreshThread;
		private bool _refreshPending = false;
		private bool _isRefreshing = false;
		private bool _isUpdating = false;
		private readonly object _refreshControlLock = new object ();

		private const string CACHE_FILENAME = "FeedCache.json";
		private const int MAX_CACHE_ITEMS = 40;
		private TileBaseEventRouter router = null;

		/// <summary>
		/// 公开只读属性，指示是否正在进行刷新操作
		/// </summary>
		public static bool IsRefreshing
		{
			get
			{
				Tile tile = TileInstance as Tile;
				if (tile != null)
				{
					lock (tile._refreshControlLock)
					{
						return tile._isRefreshing;
					}
				}
				return false;
			}
			private set
			{
				Tile tile = TileInstance as Tile;
				if (tile != null)
				{
					lock (tile._refreshControlLock)
					{
						tile._isRefreshing = value;
					}
				}
			}
		}

		public static bool IsUpdating
		{
			get
			{
				Tile tile = TileInstance as Tile;
				if (tile != null)
				{
					lock (tile._refreshControlLock)
					{
						return tile._isUpdating;
					}
				}
				return false;
			}
			private set
			{
				Tile tile = TileInstance as Tile;
				if (tile != null)
				{
					lock (tile._refreshControlLock)
					{
						tile._isUpdating = value;
					}
				}
			}
		}

		public override void OnInitialize ()
		{
			TileInstance = this;
			TileFolder = Region;
			SidebarFeatures = Features;
			Region?.StringResources?.CleanRedundantValues ();
			UserRegion?.StringResources?.CleanRedundantValues ();
			Utilities.ReleaseLargeResourcesAsync ();
			Options = new TileOptions (Config);
			router = new TileEventRouter (this);
			var panel = TileUI as Panel;
			tilePanel = new TilePanel ();
			panel.Children.Add (tilePanel);
			Type feedMgrType = Type.GetTypeFromProgID ("Microsoft.FeedsManager");
			if (feedMgrType != null)
				FeedMgr = (IFeedsManager)Activator.CreateInstance (feedMgrType);
			LoadCacheAndDisplay ();
			Options.PropertyChanged += Options_PropertyChanged;
			RequestRefresh (debounceDelayMs: 0);
			_refreshTimer = new Timer (_ => RequestRefresh (), null, TimeSpan.FromMinutes (30), TimeSpan.FromMinutes (30));
			InitContextMenu ();
		}
		public override ContextMenu ContextMenu
		{
			get
			{
				try
				{
					var refreshItem = base.ContextMenu?.Items [0] as MenuItem;
					if (refreshItem == null) return base.ContextMenu;
					refreshItem.IsEnabled = !IsRefreshing;
				}
				catch { }
				return base.ContextMenu;
			}
			set
			{
				base.ContextMenu = value;
			}
		}
		private void InitContextMenu ()
		{
			var refreshItem = new MenuItem {
				Header = TileFolder?.StringResources?.SuitableResource ("OPTIONS_REFRESH", "Refresh")
			};
			refreshItem.Click += MenuItemRefresh_Click;
			var menu = new ContextMenu ();
			menu.Items.Add (refreshItem);
			ContextMenu = menu;
		}
		private void MenuItemRefresh_Click (object sender, RoutedEventArgs e)
		{
			RequestRefresh (debounceDelayMs: 0);
		}
		private void Options_PropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof (Options.SourceType):
				case nameof (Options.FeedList):
					// 选项变化时触发刷新
					RequestRefresh ();
					break;
			}
		}

		public override void OnDestroy ()
		{
			if (ContextMenu != null)
			{
				(ContextMenu.Items [0] as MenuItem).Click -= MenuItemRefresh_Click;
			}
			// 停止定时器
			_refreshTimer?.Dispose ();
			_refreshTimer = null;

			// 取消所有正在进行的刷新操作
			lock (_refreshControlLock)
			{
				_refreshPending = false;

				// 停止 debounce 定时器
				if (_refreshDebounceTimer != null)
				{
					_refreshDebounceTimer.Dispose ();
					_refreshDebounceTimer = null;
				}

				// 取消活跃的刷新操作
				if (_activeRefreshCts != null)
				{
					_activeRefreshCts.Cancel ();
					_activeRefreshCts.Dispose ();
					_activeRefreshCts = null;
				}

				// 等待活跃线程结束
				if (_activeRefreshThread != null && _activeRefreshThread.IsAlive)
				{
					_activeRefreshThread.Join (2000);
				}
				_activeRefreshThread = null;

				_isRefreshing = false;
			}

			// 在取消后台线程之前，先保存当前显示的缓存（如果 UserRegion 可用）
			if (UserRegion != null && AllItems.Count > 0)
			{
				try
				{
					var currentItems = AllItems.ToList ();
					SaveCache (currentItems);
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine ($"Save cache on destroy error: {ex.Message}");
				}
			}

			if (TileFeedItem.DetailPanel != null)
			{
				Panel detailParent = TileFeedItem.DetailPanel.Parent as Panel;
				if (detailParent != null && detailParent.Children != null)
				{
					detailParent.Children.Clear ();
				}
			}
			TileFeedItem.DetailPanel = null;

			if (tilePanel != null)
			{
				Panel tilePanelParent = tilePanel.Parent as Panel;
				if (tilePanelParent != null && tilePanelParent.Children != null)
				{
					tilePanelParent.Children.Clear ();
				}
			}
			tilePanel = null;

			if (router != null)
			{
				router.Dispose ();
			}
			router = null;

			Options.PropertyChanged -= Options_PropertyChanged;
			ReleaseComObject (FeedMgr);
			FeedMgr = null;
			TileFolder = null;
			SidebarFeatures = null;
			Options = null;
			TileInstance = null;

			Feeds?.Clear ();
			AllItems?.Clear ();
		}

		public override bool OnResponse (ITileResponse resp)
		{
			if (resp.ResponseSource == "Sidebar")
			{
				switch (resp.ResponseName)
				{
					case "ExtraFlyoutWindow":
						if (resp != null && resp.TransferDatas is FeedArticlePanel)
						{
							var fw = resp.ResponseData as FlyoutAboutEventArgs;
							fw.Window.AllowsTransparency = false;
							var panel = fw != null ? fw.FlyoutUI as Panel : null;
							fw.Window.Width = 377;
							fw.Window.Height = 300;
							fw.Window.Show ();
							var browserWindow = resp.TransferDatas as FeedArticlePanel;
							if (fw.ClientArea != null)
							{
								fw.ClientArea.Children.Add (browserWindow);
							}
							EventHandler closeHandler = null;
							closeHandler = (s, e) => {
								Panel browserWindowParent = browserWindow != null ? browserWindow.Parent as Panel : null;
								if (browserWindowParent != null && browserWindowParent.Children != null)
								{
									browserWindowParent.Children.Clear ();
								}
								if (browserWindow != null)
								{
									browserWindow.ClearPage ();
								}
								fw.Window.Closed -= closeHandler;
							};
							fw.Window.Closed += closeHandler;
							return true;
						}
						break;
				}
			}
			return false;
		}

		class TileEventRouter: TileBaseEventRouter
		{
			private FlyoutPanel flyoutPanel = null;
			private PropertiesPanel propPanel = null;
			public TileEventRouter (TileBase tb) : base (tb) { }
			public override void FlyoutForm_Init (object sender, FlyoutAboutEventArgs e)
			{
				e.Window.SizeToContent = SizeToContent.Height;
				if (flyoutPanel == null) flyoutPanel = new FlyoutPanel ();
				(flyoutPanel?.Parent as Panel)?.Children?.Clear ();
				e.ClientArea.Children.Add (flyoutPanel);
			}
			public override void FlyoutForm_Loaded (object sender, FlyoutAboutEventArgs e)
			{
				e.Window.UpdateLayout ();
				e.Window.MaxHeight = Math.Min (721, (SidebarFeatures.Config.CurrentScreen.WorkingArea.Height / SidebarFeatures.Config.CurrentScreen.GetDPI ()) - e.Window.Top);
				e.FlyoutWindowFeatures.FixPosition ();
			}
			public override void FlyoutForm_Closed (object sender, EventArgs e)
			{
				(flyoutPanel?.Parent as Panel)?.Children?.Clear ();
			}
			public override void PropertiesForm_Init (object sender, PropertiesAboutEventArgs e)
			{
				if (propPanel == null) propPanel = new PropertiesPanel ();
				e.ClientArea.Children.Add (propPanel);
				e.Window.SizeToContent = SizeToContent.WidthAndHeight;
			}
			public override void PropertiesForm_Closed (object sender, EventArgs e)
			{
				Panel propPanelParent = propPanel != null ? propPanel.Parent as Panel : null;
				if (propPanelParent != null && propPanelParent.Children != null)
				{
					propPanelParent.Children.Clear ();
				}
				propPanel = null;
			}
			public override void PropertiesForm_ClickOK (object sender, PropertiesAboutEventArgs e)
			{
				if (propPanel != null)
				{
					propPanel.OnSave ();
				}
			}
			public override void Router_WillDestroy ()
			{
				Panel propPanelParent = propPanel != null ? propPanel.Parent as Panel : null;
				if (propPanelParent != null && propPanelParent.Children != null)
				{
					propPanelParent.Children.Clear ();
				}
				propPanel = null;
			}
		}
		//public static void MarkItemAsRead (string feedPath, string localId)
		//{
		//	if (FeedMgr == null) return;
		//	if (string.IsNullOrEmpty (feedPath) || string.IsNullOrEmpty (localId)) return;

		//	try
		//	{
		//		IFeed feed = null;
		//		IFeedItem item = null;
		//		try
		//		{
		//			feed = FeedMgr.GetFeed (feedPath);
		//			if (feed == null) return;
		//			int id;
		//			if (!int.TryParse (localId, out id)) return;
		//			item = feed.GetItem (id);
		//			if (item != null && !item.IsRead)
		//			{
		//				item.IsRead = true;
		//				System.Diagnostics.Debug.WriteLine ($"Marked as read: {feedPath} / {localId}");
		//			}
		//		}
		//		finally
		//		{
		//			if (item != null) Marshal.ReleaseComObject (item);
		//			if (feed != null) Marshal.ReleaseComObject (feed);
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		System.Diagnostics.Debug.WriteLine ($"Error marking item as read: {ex.Message}");
		//	}
		//}
		public static void MarkItemAsRead (string feedPath, string localId)
		{
			if (string.IsNullOrEmpty (feedPath) || string.IsNullOrEmpty (localId)) return;

			// 自实现条目：更新本地已读缓存
			if (localId.StartsWith ("own:", StringComparison.OrdinalIgnoreCase))
			{
				var tile = TileInstance as Tile;
				if (tile != null)
				{
					var state = tile.LoadReadState ();
					if (state.Add (feedPath + "|" + localId))
						tile.SaveReadState (state);
				}
				return;
			}

			// 数字 ID：走 FeedMgr
			if (FeedMgr == null) return;
			int id;
			if (!int.TryParse (localId, out id)) return;
			try
			{
				IFeed feed = null;
				IFeedItem item = null;
				try
				{
					feed = FeedMgr.GetFeed (feedPath);
					if (feed == null) return;
					item = feed.GetItem (id);
					if (item != null && !item.IsRead)
						item.IsRead = true;
				}
				finally
				{
					if (item != null) Marshal.ReleaseComObject (item);
					if (feed != null) Marshal.ReleaseComObject (feed);
				}
			}
			catch { }
		}
		private void LoadCacheAndDisplay ()
		{
			if (UserRegion == null) return; // 没有用户目录则不使用缓存

			try
			{
				string cachePath = Path.Combine (UserRegion.FolderPath, CACHE_FILENAME);
				if (File.Exists (cachePath))
				{
					string json = File.ReadAllText (cachePath);
					var cachedItems = JsonConvert.DeserializeObject<List<FeedItemData>> (json);
					if (cachedItems != null && cachedItems.Count > 0)
					{
						if (System.Windows.Application.Current != null)
						{
							System.Windows.Application.Current.Dispatcher.Invoke (new Action (() => {
								AllItems.Clear ();
								foreach (var item in cachedItems.Take (MAX_CACHE_ITEMS))
									AllItems.Add (item);
							}));
						}
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine ($"Load cache error: {ex.Message}");
			}
		}

		private void SaveCache (List<FeedItemData> items)
		{
			if (UserRegion == null) return; // 没有用户目录则不保存缓存

			try
			{
				var toSave = items.Take (MAX_CACHE_ITEMS).ToList ();
				string json = JsonConvert.SerializeObject (toSave, new JsonSerializerSettings {
					ReferenceLoopHandling = ReferenceLoopHandling.Ignore
				});
				string cachePath = Path.Combine (UserRegion.FolderPath, CACHE_FILENAME);
				File.WriteAllText (cachePath, json);
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine ($"Save cache error: {ex.Message}");
			}
		}

		/// <summary>
		/// 统一的刷新请求入口，支持 debounce、唯一性和中断重启
		/// </summary>
		/// <param name="debounceDelayMs">防抖延迟（毫秒），0 表示立即执行</param>
		public void RequestRefresh (int debounceDelayMs = 500)
		{
			lock (_refreshControlLock)
			{
				_refreshPending = true;

				// 停止现有的 debounce 定时器
				if (_refreshDebounceTimer != null)
				{
					_refreshDebounceTimer.Dispose ();
					_refreshDebounceTimer = null;
				}

				// 取消正在进行的刷新
				if (_activeRefreshCts != null)
				{
					_activeRefreshCts.Cancel ();
					_activeRefreshCts.Dispose ();
					_activeRefreshCts = null;
				}

				if (debounceDelayMs == 0)
				{
					// 立即执行
					DoRefresh ();
				}
				else
				{
					// 启动 debounce 定时器
					_refreshDebounceTimer = new Timer (_ => DoRefresh (), null, debounceDelayMs, Timeout.Infinite);
				}
			}
		}

		/// <summary>
		/// 执行实际的刷新操作（在锁内调用）
		/// </summary>
		private void DoRefresh ()
		{
			lock (_refreshControlLock)
			{
				// 停止 debounce 定时器
				if (_refreshDebounceTimer != null)
				{
					_refreshDebounceTimer.Dispose ();
					_refreshDebounceTimer = null;
				}

				// 如果没有待处理的刷新请求，退出
				if (!_refreshPending)
					return;

				_refreshPending = false;

				// 创建新的取消令牌源
				_activeRefreshCts = new CancellationTokenSource ();
				var cts = _activeRefreshCts;

				// 启动新的刷新线程
				_activeRefreshThread = new Thread (() => {
					try
					{
						lock (_refreshControlLock)
						{
							_isRefreshing = true;
						}

						RefreshFeedsFull (cts.Token);
					}
					catch (OperationCanceledException)
					{
						System.Diagnostics.Debug.WriteLine ("Refresh canceled by new request.");
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine ($"Refresh error: {ex.Message}");
					}
					finally
					{
						lock (_refreshControlLock)
						{
							_isRefreshing = false;
							// 只清理当前的 CTS，如果已经被新线程替换则不清理
							if (_activeRefreshCts == cts)
							{
								_activeRefreshCts = null;
							}
							if (cts != null)
							{
								cts.Dispose ();
							}
						}
						Utilities.ReleaseLargeResourcesAsync ();
					}
				});

				_activeRefreshThread.SetApartmentState (ApartmentState.STA);
				_activeRefreshThread.Start ();
			}
		}

		/// <summary>
		/// 公开方法：请求刷新（默认 500ms debounce）
		/// </summary>
		public void RefreshFeeds ()
		{
			RequestRefresh (debounceDelayMs: 500);
		}

		private void RefreshFeedsFull (CancellationToken cancellationToken)
		{
			if (Options != null && Options.UseOwnUpdater)
			{
				RefreshFeedsFullOwn (cancellationToken);
				return;
			}
			IFeedsManager localFeedMgr = null;
			Type feedMgrType = Type.GetTypeFromProgID ("Microsoft.FeedsManager");
			if (feedMgrType != null)
				localFeedMgr = (IFeedsManager)Activator.CreateInstance (feedMgrType);
			if (localFeedMgr == null)
			{
				System.Diagnostics.Debug.WriteLine ("Cannot create local FeedsManager.");
				return;
			}

			try
			{
				var allFeedsDict = new Dictionary<string, FeedData> ();
				var allItemsList = new List<FeedItemData> ();

				// 收集所有源（异常已在内部处理）
				CollectAllFeeds (localFeedMgr.RootFolder, allFeedsDict, allItemsList, cancellationToken);

				if (cancellationToken.IsCancellationRequested) return;

				// 根据用户设置过滤源
				IEnumerable<FeedData> filteredFeeds;
				if (Options.SourceType == FeedSource.Selected && Options.FeedList != null && Options.FeedList.Count > 0)
				{
					var selectedPaths = new HashSet<string> (Options.FeedList);
					filteredFeeds = allFeedsDict.Values.Where (f => selectedPaths.Contains (f.Path));
				}
				else
				{
					filteredFeeds = allFeedsDict.Values;
				}

				// 排序并取前 ShowCountLimit 条
				var finalItems = filteredFeeds
					.SelectMany (f => f.Items)
					.OrderByDescending (item => item.PublishDate)
					.Take (100)
					.ToList ();

				var feedsList = allFeedsDict.Values.ToList ();

				if (System.Windows.Application.Current != null)
				{
					System.Windows.Application.Current.Dispatcher.Invoke (new Action (() => {
						UpdateCollectionWithDiff (Feeds, feedsList, (old, newE) => old.Path == newE.Path);
						UpdateCollectionWithDiff (AllItems, finalItems, (old, newE) => old.LocalId == newE.LocalId && old.Parent != null && newE.Parent != null && old.Parent.Path == newE.Parent.Path, true);
					}));
				}

				// 保存缓存（仅当 UserRegion 存在）
				SaveCache (finalItems);
			}
			finally
			{
				if (localFeedMgr != null)
					ReleaseComObject (localFeedMgr);
			}
		}

		private void CollectAllFeeds (IFeedFolder folder, Dictionary<string, FeedData> feedsDict, List<FeedItemData> allItemsList, CancellationToken cancellationToken)
		{
			if (folder == null) return;

			IFeedsEnum feedsEnum = null;
			try
			{
				feedsEnum = (IFeedsEnum)folder.Feeds;
				for (int i = 0; i < feedsEnum.Count; i++)
				{
					cancellationToken.ThrowIfCancellationRequested ();

					IFeed feed = null;
					try
					{
						feed = (IFeed)feedsEnum.Item (i);
						if (feed == null) continue;

						var feedData = LoadFeedData (feed);
						if (feedData != null)
						{
							feedsDict [feedData.Path] = feedData;
							allItemsList.AddRange (feedData.Items);
						}
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine ($"Error processing feed item {i}: {ex.Message}");
						// 吞掉异常，继续下一个源
					}
					finally
					{
						if (feed != null) ReleaseComObject (feed);
					}
				}
			}
			finally
			{
				if (feedsEnum != null) ReleaseComObject (feedsEnum);
			}

			IFeedsEnum subFoldersEnum = null;
			try
			{
				subFoldersEnum = (IFeedsEnum)folder.Subfolders;
				for (int i = 0; i < subFoldersEnum.Count; i++)
				{
					cancellationToken.ThrowIfCancellationRequested ();

					IFeedFolder subFolder = null;
					try
					{
						subFolder = (IFeedFolder)subFoldersEnum.Item (i);
						CollectAllFeeds (subFolder, feedsDict, allItemsList, cancellationToken);
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine ($"Error processing subfolder {i}: {ex.Message}");
						// 吞掉异常，继续下一个子文件夹
					}
					finally
					{
						if (subFolder != null) ReleaseComObject (subFolder);
					}
				}
			}
			finally
			{
				if (subFoldersEnum != null) ReleaseComObject (subFoldersEnum);
			}
		}

		private FeedData LoadFeedData (IFeed feed)
		{
			if (feed == null) return null;

			var feedData = new FeedData {
				Title = feed.Name ?? "",
				Description = feed.Description ?? "",
				Link = feed.Link ?? "",
				Url = feed.Url ?? "",
				UnreadCount = feed.UnreadItemCount,
				Path = feed.Path ?? ""
			};

			IFeedsEnum itemsEnum = null;
			try
			{
				itemsEnum = (IFeedsEnum)feed.Items;
				for (int j = 0; j < itemsEnum.Count; j++)
				{
					IFeedItem item = null;
					try
					{
						item = (IFeedItem)itemsEnum.Item (j);
						if (item == null) continue;

						DateTime pubDate = item.PubDate;
						if (pubDate == DateTime.MinValue || pubDate.Year == 1899)
							pubDate = item.Modified;

						var itemData = new FeedItemData {
							Title = item.Title ?? "",
							Description = item.Description ?? "",
							Link = item.Link ?? "",
							LocalId = item.LocalId.ToString (),
							IsRead = item.IsRead,
							PublishDate = pubDate,
							Parent = feedData
						};
						feedData.Items.Add (itemData);
					}
					catch (Exception ex)
					{
						System.Diagnostics.Debug.WriteLine ($"Error reading feed item {j} in feed {feedData.Path}: {ex.Message}");
						// 吞掉异常，继续下一个条目
					}
					finally
					{
						if (item != null) ReleaseComObject (item);
					}
				}
			}
			finally
			{
				if (itemsEnum != null) ReleaseComObject (itemsEnum);
			}

			return feedData;
		}

		private void UpdateCollectionWithDiff<T> (ObservableCollection<T> target, IList<T> newList, Func<T, T, bool> areEqual, bool maintainOrder = true)
		{
			if (target == null) return;

			// 删除不存在于新列表中的项
			for (int i = target.Count - 1; i >= 0; i--)
			{
				var oldItem = target [i];
				if (!newList.Any (newItem => areEqual (oldItem, newItem)))
					target.RemoveAt (i);
			}

			// 添加新项或调整顺序
			for (int i = 0; i < newList.Count; i++)
			{
				var newItem = newList [i];
				int existingIndex = -1;
				for (int j = 0; j < target.Count; j++)
				{
					if (areEqual (target [j], newItem))
					{
						existingIndex = j;
						break;
					}
				}
				if (existingIndex == -1)
					target.Insert (i, newItem);
				else if (maintainOrder && existingIndex != i)
					target.Move (existingIndex, i);
			}
		}

		private static void ReleaseComObject (object obj)
		{
			if (obj != null && Marshal.IsComObject (obj))
				Marshal.ReleaseComObject (obj);
		}

		// 静态字段
		private static readonly object _updateLock = new object ();
		private static Timer _updateDebounceTimer;

		/// <summary>
		/// 异步触发系统订阅源更新（使用 AsyncSyncAll），带防抖和单任务保证
		/// </summary>
		//public static void UpdateFeedsAsync ()
		//{
		//	lock (_updateLock)
		//	{
		//		// 取消之前的防抖定时器
		//		_updateDebounceTimer?.Dispose ();

		//		// 启动新的防抖定时器（延迟 300ms）
		//		_updateDebounceTimer = new Timer (_ =>
		//		{
		//			lock (_updateLock)
		//			{
		//				_updateDebounceTimer?.Dispose ();
		//				_updateDebounceTimer = null;

		//				// 实际执行更新
		//				if (FeedMgr == null) return;

		//				// 触发后台更新（AsyncSyncAll 立即返回，在后台执行）
		//				FeedMgr.AsyncSyncAll ();

		//				// 注意：AsyncSyncAll 是异步且不提供完成通知，
		//				// 因此 IsRefreshing 无法准确跟踪完成状态。
		//				// 这里我们临时设置为 true 并稍后重置（仅作为视觉反馈示例）
		//				IsUpdating = true;
		//				IsRefreshing = true;
		//				// 启动一个 10 秒后重置的定时器（实际完成时间可能更短或更长）
		//				Timer resetTimer = null;
		//				resetTimer = new Timer (task =>
		//				{
		//					IsRefreshing = false;
		//					IsUpdating = false;
		//					resetTimer?.Dispose ();
		//				}, null, 10000, Timeout.Infinite);
		//			}
		//		}, null, 300, Timeout.Infinite);
		//	}
		//}
		public static void UpdateFeedsAsync ()
		{
			lock (_updateLock)
			{
				_updateDebounceTimer?.Dispose ();
				_updateDebounceTimer = new Timer (_ =>
				{
					lock (_updateLock)
					{
						_updateDebounceTimer?.Dispose ();
						_updateDebounceTimer = null;

						var tile = TileInstance as Tile;
						if (tile == null) return;

						bool useOwn = Options != null && Options.UseOwnUpdater;
						if (useOwn)
						{
							if (!OwnUpdaterCooldownElapsed ())
								return;
							SetLastOwnUpdateUtc (DateTime.UtcNow);
							IsUpdating = true;
							IsRefreshing = true;
							tile.RequestRefresh (debounceDelayMs: 0);
						}
						else
						{
							if (FeedMgr == null) return;
							FeedMgr.AsyncSyncAll ();
							IsUpdating = true;
							IsRefreshing = true;
							Timer resetTimer = null;
							resetTimer = new Timer (t =>
							{
								IsRefreshing = false;
								IsUpdating = false;
								resetTimer?.Dispose ();
							}, null, 10000, Timeout.Infinite);
						}
					}
				}, null, 300, Timeout.Infinite);
			}
		}

		private const string READ_STATE_FILENAME = "ReadState.json";
		private readonly object _readStateLock = new object ();

		public HashSet<string> LoadReadState ()
		{
			lock (_readStateLock)
			{
				if (UserRegion == null) return new HashSet<string> (StringComparer.OrdinalIgnoreCase);
				try
				{
					string path = Path.Combine (UserRegion.FolderPath, READ_STATE_FILENAME);
					if (!File.Exists (path)) return new HashSet<string> (StringComparer.OrdinalIgnoreCase);
					var list = JsonConvert.DeserializeObject<List<string>> (File.ReadAllText (path));
					return new HashSet<string> (list ?? new List<string> (), StringComparer.OrdinalIgnoreCase);
				}
				catch { return new HashSet<string> (StringComparer.OrdinalIgnoreCase); }
			}
		}

		public void SaveReadState (HashSet<string> state)
		{
			lock (_readStateLock)
			{
				if (UserRegion == null) return;
				try
				{
					string path = Path.Combine (UserRegion.FolderPath, READ_STATE_FILENAME);
					File.WriteAllText (path, JsonConvert.SerializeObject (state.ToList ()));
				}
				catch { }
			}
		}
		private void RefreshFeedsFullOwn (CancellationToken cancellationToken)
		{
			var localFeedMgr = FeedMgr;
			if (localFeedMgr == null) return;
			try
			{
				var feedMetaList = new List<FeedData> ();
				CollectFeedMeta (localFeedMgr.RootFolder, feedMetaList, cancellationToken);
				if (cancellationToken.IsCancellationRequested) return;

				var readState = LoadReadState ();
				string cacheDir = UserRegion != null ? Path.Combine (UserRegion.FolderPath, "FeedSourceCache") : null;
				var allFeedsDict = new Dictionary<string, FeedData> ();
				var allItemsList = new List<FeedItemData> ();

				foreach (var feedData in feedMetaList)
				{
					if (cancellationToken.IsCancellationRequested) return;
					if (string.IsNullOrWhiteSpace (feedData.Url))
					{
						allFeedsDict [feedData.Path] = feedData;
						continue;
					}
					string xml = FeedUpdater.DownloadWithCache (feedData.Url, cacheDir);
					if (!string.IsNullOrEmpty (xml))
					{
						var items = FeedUpdater.ParseFeed (xml, feedData);
						int unread = 0;
						foreach (var item in items)
						{
							if (readState.Contains (feedData.Path + "|" + item.LocalId))
								item.IsRead = true;
							if (!item.IsRead) unread++;
							feedData.Items.Add (item);
						}
						feedData.UnreadCount = unread;
					}
					allFeedsDict [feedData.Path] = feedData;
					allItemsList.AddRange (feedData.Items);
				}

				if (cancellationToken.IsCancellationRequested) return;

				IEnumerable<FeedData> filteredFeeds;
				if (Options.SourceType == FeedSource.Selected && Options.FeedList != null && Options.FeedList.Count > 0)
				{
					var selectedPaths = new HashSet<string> (Options.FeedList);
					filteredFeeds = allFeedsDict.Values.Where (f => selectedPaths.Contains (f.Path));
				}
				else
				{
					filteredFeeds = allFeedsDict.Values;
				}

				var finalItems = filteredFeeds
					.SelectMany (f => f.Items)
					.OrderByDescending (item => item.PublishDate)
					.Take (100)
					.ToList ();

				var feedsList = allFeedsDict.Values.ToList ();

				if (System.Windows.Application.Current != null)
				{
					System.Windows.Application.Current.Dispatcher.Invoke (new Action (() =>
					{
						UpdateCollectionWithDiff (Feeds, feedsList, (old, ne) => old.Path == ne.Path);
						UpdateCollectionWithDiff (AllItems, finalItems,
							(old, ne) => old.LocalId == ne.LocalId && old.Parent != null && ne.Parent != null && old.Parent.Path == ne.Parent.Path,
							true);
					}));
				}

				SaveCache (finalItems);
			}
			finally
			{
				IsUpdating = false;
			}
		}

		private void CollectFeedMeta (IFeedFolder folder, List<FeedData> result, CancellationToken cancellationToken)
		{
			if (folder == null) return;
			IFeedsEnum feedsEnum = null;
			try
			{
				feedsEnum = (IFeedsEnum)folder.Feeds;
				for (int i = 0; i < feedsEnum.Count; i++)
				{
					cancellationToken.ThrowIfCancellationRequested ();
					IFeed feed = null;
					try
					{
						feed = (IFeed)feedsEnum.Item (i);
						if (feed == null) continue;
						result.Add (new FeedData {
							Title = feed.Name ?? "",
							Description = feed.Description ?? "",
							Link = feed.Link ?? "",
							Url = feed.Url ?? "",
							UnreadCount = 0,
							Path = feed.Path ?? ""
						});
					}
					catch { }
					finally { if (feed != null) ReleaseComObject (feed); }
				}
			}
			finally { if (feedsEnum != null) ReleaseComObject (feedsEnum); }

			IFeedsEnum subFolders = null;
			try
			{
				subFolders = (IFeedsEnum)folder.Subfolders;
				for (int i = 0; i < subFolders.Count; i++)
				{
					cancellationToken.ThrowIfCancellationRequested ();
					IFeedFolder sub = null;
					try
					{
						sub = (IFeedFolder)subFolders.Item (i);
						CollectFeedMeta (sub, result, cancellationToken);
					}
					catch { }
					finally { if (sub != null) ReleaseComObject (sub); }
				}
			}
			finally { if (subFolders != null) ReleaseComObject (subFolders); }
		}
		private static readonly TimeSpan OwnUpdateMinInterval = TimeSpan.FromMinutes (10);

		private static DateTime GetLastOwnUpdateUtc ()
		{
			var tile = TileInstance as Tile;
			var section = tile?.Config?.Xml?.Global;
			if (section == null) return DateTime.MinValue;
			try
			{
				var utc = section.Get<DateTime> ("LastOwnUpdateUtc", DateTime.MinValue);
				if (utc == DateTime.MinValue) return DateTime.MinValue;
				return utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime ();
			}
			catch { return DateTime.MinValue; }
		}

		private static void SetLastOwnUpdateUtc (DateTime utc)
		{
			var tile = TileInstance as Tile;
			var section = tile?.Config?.Xml?.Global;
			if (section == null) return;
			try
			{
				section.Set ("LastOwnUpdateUtc", utc.ToUniversalTime ());
			}
			catch { }
		}

		/// <summary>OwnUpdater 是否已进入允许更新的时间窗口。</summary>
		private static bool OwnUpdaterCooldownElapsed ()
		{
			var last = GetLastOwnUpdateUtc ();
			if (last == DateTime.MinValue) return true;
			return DateTime.UtcNow - last >= OwnUpdateMinInterval;
		}
	}
}