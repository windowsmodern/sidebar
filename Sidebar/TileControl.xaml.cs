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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;
using System.Windows.Controls.Primitives;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.ComponentModel;
using System.Windows.Markup;

namespace Sidebar
{
	/// <summary>
	/// TileControl.xaml 的交互逻辑
	/// </summary>
	public partial class TileControl: UserControl, ITileElement
	{
		private TileContext tileContext = null;
		public TileContext TileContext => tileContext;
		public TileInstance Instance { get; private set; }
		public TileApplicationStorage AppStorage { get; private set; }
		public string UserModelId => AppStorage?.UserModelId;
		#region Instance
		private void SetInstance (TileInstance inst)
		{
			if (Instance != null)
			{
				Instance.Config.PropertyChanged -= Config_PropertyChanged;
				TileContentContainer.Children.Clear ();
			}
			Instance = inst;
			if (Instance != null)
			{
				Instance.Config.PropertyChanged -= Config_PropertyChanged;
				Instance.Config.PropertyChanged += Config_PropertyChanged;
			}
		}
		private void Config_PropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (!Dispatcher.CheckAccess ())
			{
				Dispatcher.BeginInvoke (System.Windows.Threading.DispatcherPriority.DataBind, new Action (() => Config_PropertyChanged (sender, e)));
				return;
			}
			switch (e.PropertyName)
			{
				case nameof (TileConfig.Pinned):
					IsPinned = Instance?.Config?.Pinned ?? false;
					break;
				case nameof (TileConfig.AutoSize):
				case nameof (TileConfig.Height):
					//if (_heightInitialized && !_isAnimating && !_isDragging)
					//PlayRuntimeHeightAnimation ();
					break;
			}
		}
		private void SetAppStorage (TileApplicationStorage tas)
		{
			AppStorage = tas;
			ContentContainer = InitTileContentContainer ();
			if (tas.Infomation.VisualElements.Tile.Overflow != TileOverflow.Auto)
			{
				TileContentContainerControl.MaxHeight = tas.Infomation.VisualElements.Tile.MaxHeight;
			}
			TileSplitterThumb.IsEnabled = tas.Infomation.VisualElements.Tile.Overflow != TileOverflow.Auto;
		}
		public static TileControl CreateTileControl (TileApplicationStorage tas, ISidebarFeatures isf = null, bool sidebarFirstLoad = false)
		{
			var ctrl = new TileControl (sidebarFirstLoad);
			ctrl.SetAppStorage (tas);
			var inst = new TileInstance (tas, ctrl.ContentContainer, isf);
			ctrl.SetInstance (inst);
			inst.Initialize ();
			ctrl.DisplayName = tas.DisplayName;
			ctrl.Icon = tas.LogoImage;
			return ctrl;
		}
		/// <summary>
		/// 获取磁贴所属的 SidebarWindow。
		/// 当磁贴位于溢出窗口（HideTilesContainWindow）时，Window.GetWindow 会返回
		/// HideTilesContainWindow，此时回退到全局的 SidebarWindow.Current。
		/// </summary>
		private static SidebarWindow GetSidebarHost (DependencyObject element)
		{
			var window = Window.GetWindow (element);
			var host = window as SidebarWindow;
			if (host != null) return host;
			var hidden = window as HideTilesContainWindow;
			if (hidden != null) return hidden.MainWindow;
			return SidebarWindow.Current;
		}
		public SidebarWindow SidebarHost => GetSidebarHost (this);
		#endregion
		public TileControl (bool sidebarFirstLoad = false)
		{
			isSidebarFirstLoad = sidebarFirstLoad;
			InitializeComponent ();
			tileContext = new TileContext (this, TileContentContainerControl);
			VisualView = new TileVisualView (this);
			InitTileDragDrop ();
			InitFlyoutHelper ();
		}
		public Task DestroyTileControl ()
		{
			try { propertiesWnd?.Close (); } catch { }
			propertiesWnd = null;
			try { flyoutWnd?.Close (); } catch { }
			flyoutWnd = null;
			DestroyFlyoutHelper ();
			DestroyTileDragDrop ();
			var tcs = new TaskCompletionSource<object> ();
			this.SetTimeout (() => {
				Height = ActualHeight;
				var sb = Resources ["TileUnloadAnimation"] as Storyboard;
				if (sb == null)
				{
					tcs.TrySetResult (null);
					return;
				}
				EventHandler completed = null;
				completed = (s, e) => {
					sb.Completed -= completed;
					tcs.TrySetResult (null);
				};
				sb.Completed += completed;
				sb.Begin (this);
			}, 0);
			return tcs.Task.ContinueWith (_ => {
				SidebarHost.EnsureTileRemoveFromOverflow (this);
				(Parent as Panel)?.Children?.Remove (this);
				Instance?.Instance?.OnDestroy ();
				tileContext?.Dispose ();
				tileContext = null;
			}, TaskScheduler.FromCurrentSynchronizationContext ());
		}
		#region Public Properties
		public Panel ContentContainer { get; private set; }
		public string DisplayName
		{
			get { return TileDisplayNameControl?.Text; }
			set { TileDisplayNameControl.Text = value; }
		}
		public ImageSource Icon
		{
			get { return TileDisplayImageControl?.Source; }
			set { TileDisplayImageControl.Source = value; }
		}
		public Panel InitTileContentContainer ()
		{
			switch (AppStorage.Infomation.VisualElements.Tile.Overflow)
			{
				default:
				case TileOverflow.Auto:
				case TileOverflow.Hidden:
					return TileContentContainer;
				case TileOverflow.Scale:
					var vb = new Viewbox ();
					TileContentContainer.Children.Add (vb);
					var grid = new Grid ();
					vb.Child = grid;
					return grid;
				case TileOverflow.Scroll:
					var sv = new ScrollViewer ();
					sv.HorizontalContentAlignment = HorizontalAlignment.Stretch;
					sv.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
					sv.SetResourceReference (ScrollViewer.StyleProperty, "AutoHideScrollViewer");
					TileContentContainer.Children.Add (sv);
					var sg = new Grid ();
					sv.Content = sg;
					return sg;
			}
			return TileContentContainer;
		}
		public TileVisualView VisualView { get; set; }
		#endregion
		#region Pinned
		public static readonly DependencyProperty IsPinnedProperty = DependencyProperty.Register (nameof (IsPinned), typeof (bool), typeof (TileControl), new FrameworkPropertyMetadata (false, FrameworkPropertyMetadataOptions.AffectsRender));
		public bool IsPinned { get { return (bool)GetValue (IsPinnedProperty); } set { SetValue (IsPinnedProperty, value); } }
		#endregion
		#region Height
		private void TileFirstLoadStoryboard_Completed (object sender, EventArgs e)
		{
			Height = double.NaN;
			isfirstrunfinish = true;
			TileContentContainerControl.MinHeight = AppStorage.Infomation.VisualElements.Tile.MinHeight;
			isSidebarFirstLoad = false;
		}
		bool isfirstrun = false;
		bool isfirstrunfinish = false;
		public bool IsFirstLoad => isfirstrun;
		public bool IsFirstLoaded => isfirstrunfinish;
		private double InitContentHeight ()
		{
			var tc = Instance.Config;
			var ovfl = AppStorage.Infomation.VisualElements.Tile.Overflow;
			if (ovfl == TileOverflow.Auto || tc.AutoSize) return double.NaN;
			var ret = tc.Height;
			double minh = AppStorage.Infomation.VisualElements.Tile.MinHeight,
				maxh = AppStorage.Infomation.VisualElements.Tile.MaxHeight;
			if (ret < minh) ret = AppStorage.Infomation.VisualElements.Tile.DefaultHeight;
			if (ret > maxh) ret = maxh;
			return ret;
		}
		public bool IsOverflowAutoMode => AppStorage?.Infomation?.VisualElements?.Tile?.Overflow == TileOverflow.Auto;
		public bool IsAutoMode => IsOverflowAutoMode || (Instance?.Config?.AutoSize ?? false);
		private bool isSidebarFirstLoad = false;
		public bool IsSidebarFirstLoad
		{
			get { return isSidebarFirstLoad; }
			set { isSidebarFirstLoad = value; }
		}
		public void InitHeight ()
		{
			if (isfirstrun) return;
			var ht = InitContentHeight ();
			TileContentContainerControl.Height = ht;
			if (!IsOverflowAutoMode)
			{
				var vet = AppStorage.Infomation.VisualElements.Tile;
				TileContentContainerControl.MinHeight = vet.MinHeight;
				TileContentControlContainer.MaxHeight = vet.MaxHeight;
			}
			isfirstrun = true;
			UpdateLayout ();
			TimeoutExtensions.SetTimeout (this, () => {
				if (isSidebarFirstLoad)
				{
					var sb = Resources ["TileFirstLoadAnimationWithSidebar"] as Storyboard;
					sb.Begin ();
					Visibility = Visibility.Visible;
				}
				else
				{
					var sb = Resources ["TileFirstLoadAnimation"] as Storyboard;
					var da = sb.Children.First () as DoubleAnimation;
					da.To = ActualHeight;
					sb.Begin ();
					Visibility = Visibility.Visible;
				}
			});
		}
		public void ResetFirstStatus ()
		{
			isfirstrun = false;
			isfirstrunfinish = false;
			Visibility = Visibility.Hidden;
		}
		private void TileContentContainerControl_SizeChanged (object sender, System.Windows.SizeChangedEventArgs e)
		{
			Instance?.Instance?.OnHostChanged (TileHostEvent.Resize, tileContext, sender);
		}
		private double lastTCCHeight = -1;
		private bool isanimating = false;
		private void TileContentContainer_SizeChanged (object sender, System.Windows.SizeChangedEventArgs e)
		{
			if (!isfirstrunfinish || !isfirstrun || isanimating || _isThumbDragging)
			{
				lastTCCHeight = TileContentContainer.ActualHeight;
				return;
			}
			if (lastTCCHeight < 0) lastTCCHeight = TileContentContainer.ActualHeight;
			if (IsAutoMode)
			{
				var sb = Resources ["TileHeightChangeAnimation"] as Storyboard;
				//sb.Stop ();
				var da = sb.Children.First () as DoubleAnimation;
				da.From = lastTCCHeight;
				da.To = TileContentContainerControl.ActualHeight;
				if (TargetHeightWhenAutoSize != null && !double.IsNaN (TargetHeightWhenAutoSize.Value))
					da.To = TargetHeightWhenAutoSize ?? da.To;
				isanimating = true;
				sb.Begin ();
			}
			lastTCCHeight = TileContentContainer.ActualHeight;
		}
		private void TileHeightChangeAnimation_Completed (object sender, EventArgs e)
		{
			isanimating = false;
			if (IsAutoMode)
			{
				TileContentContainerControl.Height = double.NaN;
			}
			else
			{
				TileContentContainerControl.Height = InitContentHeight ();
			}
		}
		private void RootTileElement_SizeChanged (object sender, System.Windows.SizeChangedEventArgs e)
		{
			Instance?.Instance?.OnHostChanged (TileHostEvent.Resize, tileContext, sender);
		}
		private double _dragStartContentHeight;
		private bool _isThumbDragging;
		private bool _dragChangedAutoSize;
		private void TileSplitterThumb_DragStarted (object sender, DragStartedEventArgs e)
		{
			if (Instance?.Config != null) Instance.Config.AutoSize = false;
			_isThumbDragging = true;
			_dragChangedAutoSize = false;
			TileContentContainerControl.BeginAnimation (HeightProperty, null);
			isanimating = false;
			_dragStartContentHeight = TileContentContainerControl.ActualHeight;
			if (double.IsNaN (_dragStartContentHeight) || _dragStartContentHeight <= 0)
				_dragStartContentHeight = TileContentContainer.ActualHeight;
		}
		private void TileSplitterThumb_DragDelta (object sender, DragDeltaEventArgs e)
		{
			if (!_isThumbDragging) return;
			if (Instance?.Config?.AutoSize ?? false)
			{
				if (Instance?.Config != null) Instance.Config.AutoSize = false;
				_dragChangedAutoSize = true;
			}
			double currentHeight = TileContentContainerControl.ActualHeight;
			if (double.IsNaN (currentHeight) || currentHeight <= 0)
				currentHeight = TileContentContainer.ActualHeight;
			double delta = e.VerticalChange;
			var dock = DockPanel.GetDock (TileSplitterThumb);
			double newHeight;
			if (dock == Dock.Bottom)
				newHeight = currentHeight + delta;
			else if (dock == Dock.Top)
				newHeight = currentHeight - delta;
			else
				newHeight = currentHeight - delta;
			var ve = AppStorage.Infomation.VisualElements.Tile;
			double minH = (ve?.MinHeight ?? 10) > 0 ? (ve?.MinHeight ?? 10) : 0;
			double maxH = (ve?.MaxHeight ?? 32768) > 0 ? (ve?.MaxHeight ?? 32768) : double.PositiveInfinity;
			newHeight = Math.Max (minH, Math.Min (maxH, newHeight));
			if (newHeight < 0) newHeight = 0;
			TileContentContainerControl.Height = newHeight;
			Instance.Config.Height = newHeight;
		}
		private void TileSplitterThumb_DragCompleted (object sender, DragCompletedEventArgs e)
		{
			_isThumbDragging = false;
		}
		private void TileSplitterThumb_PreviewMouseLeftButtonDown (object sender, MouseButtonEventArgs e)
		{
			if (e.ClickCount == 2)
			{
				ToggleAutoSize ();
				e.Handled = true;
			}
		}
		private void ToggleAutoSize ()
		{
			if (Instance == null || AppStorage == null) return;
			bool newAutoSize = !Instance.Config.AutoSize;
			Instance.Config.AutoSize = newAutoSize;
			if (newAutoSize)
			{
				TileContentContainerControl.BeginAnimation (HeightProperty, null);
				TileContentContainerControl.Height = double.NaN;
			}
			else
			{
				var sb = Resources ["TileHeightChangeAnimation"] as Storyboard;
				sb.Stop ();
				var da = sb.Children.First () as DoubleAnimation;
				da.From = lastTCCHeight;
				da.To = InitContentHeight ();
				isanimating = true;
				sb.Begin ();
				TargetHeightWhenAutoSize = null;
			}
		}
		public double ContentActualHeight => TileContentContainerControl.ActualHeight;
		public double BorderActualHeight => ActualHeight - ContentActualHeight;
		public double? TargetHeightWhenAutoSize = null;
		public double ContentMinHeight => AppStorage?.Infomation?.VisualElements?.Tile?.MinHeight ?? 10;
		public double ContentMaxHeight => AppStorage?.Infomation?.VisualElements?.Tile?.MaxHeight ?? 32768;
		#endregion
		#region Context Menu
		internal TileContextMenuSource ContextMenuSource { get; set; } = TileContextMenuSource.Normal;
		private void MenuItemAutoSize_Click (object sender, RoutedEventArgs e)
		{
			ToggleAutoSize ();
		}
		private void MenuItemPinnedBottom_Click (object sender, RoutedEventArgs e)
		{
			var host = SidebarHost;
			if (host == null) return;
			if (MenuItemPinnedBottom.IsChecked) host.PinTileToBottom (this);
			else host.PinTileToTop (this);
		}
		private void MenuItemMoveUp_Click (object sender, RoutedEventArgs e)
		{
			var host = SidebarHost;
			host?.MoveTileUp (this);
		}
		private void MenuItemMoveDown_Click (object sender, RoutedEventArgs e)
		{
			var host = SidebarHost;
			host?.MoveTileDown (this);
		}
		private void MenuItemProperties_Click (object sender, RoutedEventArgs e)
		{
			OpenPropertiesWindow ();
		}
		private void MenuItemRemove_Click (object sender, RoutedEventArgs e)
		{
			var host = SidebarHost;
			host?.RemoveTile (this);
		}
		private void TileContextMenu_Opened (object sender, RoutedEventArgs e)
		{
			InitTileContextMenuCustomItems ();
			if (Instance?.Config != null)
			{
				MenuItemAutoSize.IsChecked = IsOverflowAutoMode || IsAutoMode;
				MenuItemPinnedBottom.IsChecked = Instance.Config.Pinned;
			}
			MenuItemAutoSize.IsEnabled = !(AppStorage?.Infomation?.VisualElements?.Tile?.Overflow == TileOverflow.Auto);
			MenuItemProperties.IsEnabled = AppStorage?.Infomation?.HasProperties ?? false;
			var host = SidebarHost;
			MenuItemMoveUp.IsEnabled = host?.CanMoveTileUp (this) == true;
			MenuItemMoveDown.IsEnabled = host?.CanMoveTileDown (this) == true;
			var source = ContextMenuSource;
			ContextMenuSource = TileContextMenuSource.Normal;
			ApplyContextMenuVisibilityForSource (source);
		}
		private void ApplyContextMenuVisibilityForSource (TileContextMenuSource source)
		{
			bool hideBasics = source != TileContextMenuSource.Normal;
			bool showRemoveOverflow = source == TileContextMenuSource.Overflow;
			SetItemVisible (MenuItemAutoSize, !hideBasics);
			SetItemVisible (MenuItemSeparatorForTile1, !hideBasics);
			SetItemVisible (MenuItemPinnedBottom, !hideBasics);
			SetItemVisible (MenuItemSeparatorForTile2, !hideBasics);
			SetItemVisible (MenuItemMoveUp, !hideBasics);
			SetItemVisible (MenuItemMoveDown, !hideBasics);
			SetItemVisible (MenuItemSeparatorForTile3, !hideBasics);
			SetItemVisible (MenuItemRemoveFromOverflow, showRemoveOverflow);
			SetItemVisible (MenuItemProperties, MenuItemProperties.IsEnabled || source == TileContextMenuSource.Normal);
			MenuItemRemoveFromOverflow.IsEnabled = showRemoveOverflow;
		}
		private static void SetItemVisible (UIElement element, bool visible)
		{
			if (element == null) return;
			element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
		}
		private void InitTileContextMenuCustomItems ()
		{
			MenuItemTileContextMenuItemSeparator.Visibility = Visibility.Collapsed;
			var tileMenu = Instance?.Instance?.ContextMenu;
			if (tileMenu != null && tileMenu.Items.Count > 0)
			{
				MenuItemTileContextMenuItemSeparator.Visibility = Visibility.Visible;
				for (int i = tileMenu.Items.Count - 1; i >= 0; i--)
				{
					var item = tileMenu.Items [i];
					tileMenu.Items.RemoveAt (i);
					TileContextMenu.Items.Insert (0, item);
				}
			}
		}
		private void TileContextMenu_Closed (object sender, RoutedEventArgs e)
		{
			var tileMenu = Instance?.Instance?.ContextMenu;
			var seindex = TileContextMenu.Items.IndexOf (MenuItemTileContextMenuItemSeparator);
			if (seindex > 0 && (tileMenu?.Items?.Count ?? 0) != seindex) for (var i = seindex - 1; i >= 0; i--)
				{
					var item = TileContextMenu.Items [i];
					TileContextMenu.Items.RemoveAt (i);
					tileMenu.Items.Insert (0, item);
				}
			ApplyContextMenuVisibilityForSource (TileContextMenuSource.Normal);
		}
		private void MenuItemRemoveFromOverflow_Click (object sender, RoutedEventArgs e)
		{
			var host = SidebarHost;
			host?.MoveTileOutOfOverflow (this);
		}
		#endregion
		#region Overflow (Hide Tile)
		public bool IsSizeHeightWhenOverflowed = false;
		private bool _overflowLock = false;
		private TimeoutHandle _overflowLockTimeout = null;
		public bool IsOverflowLock
		{
			get { return _overflowLock; }
			set
			{
				if (_overflowLock == value) return;
				_overflowLock = value;
				if (_overflowLockTimeout != null) TimeoutExtensions.ClearTimeout (this, _overflowLockTimeout);
				_overflowLockTimeout = null;
				if (_overflowLock)
				{
					_overflowLockTimeout = TimeoutExtensions.SetTimeout (this, () => {
						_overflowLock = false;
					}, 501);
				}
			}
		}
		#endregion
		#region Tile Drag/Drop
		/// <summary>初始化磁贴拖拽手势。</summary>
		private void InitTileDragDrop ()
		{
			PreviewMouseLeftButtonDown += TileDragDrop_PreviewMouseLeftButtonDown;
			PreviewStylusDown += TileDragDrop_PreviewStylusDown;
			PreviewTouchDown += TileDragDrop_PreviewTouchDown;
			Stylus.SetIsPressAndHoldEnabled (this, false);
			Stylus.SetIsFlicksEnabled (this, false);
			Stylus.SetIsTapFeedbackEnabled (this, false);
			Stylus.SetIsTouchFeedbackEnabled (this, false);
		}
		/// <summary>卸载磁贴拖拽手势。</summary>
		private void DestroyTileDragDrop ()
		{
			PreviewMouseLeftButtonDown -= TileDragDrop_PreviewMouseLeftButtonDown;
			PreviewStylusDown -= TileDragDrop_PreviewStylusDown;
			PreviewTouchDown -= TileDragDrop_PreviewTouchDown;
		}
		private void TileDragDrop_PreviewMouseLeftButtonDown (object sender, MouseButtonEventArgs e)
		{
			if (e.StylusDevice != null) return;
			if (IsInSplitter (e.OriginalSource as DependencyObject)) return;
			SidebarWindow host = Window.GetWindow (this) as SidebarWindow;
			if (host == null || host.DragManager == null) return;
			if (host.IsTileDragging) return;
			host.DragManager.TryStartPending (this, e);
		}
		private void TileDragDrop_PreviewStylusDown (object sender, StylusDownEventArgs e)
		{
			if (e.StylusDevice == null) return;
			if (e.StylusDevice.TabletDevice != null && e.StylusDevice.TabletDevice.Type == TabletDeviceType.Touch) return;
			if (IsInSplitter (e.OriginalSource as DependencyObject)) return;
			SidebarWindow host = Window.GetWindow (this) as SidebarWindow;
			if (host == null || host.DragManager == null) return;
			if (host.IsTileDragging) return;
			host.DragManager.TryStartPending (this, e);
		}
		private void TileDragDrop_PreviewTouchDown (object sender, TouchEventArgs e)
		{
			if (e.TouchDevice == null) return;
			if (IsInSplitter (e.OriginalSource as DependencyObject)) return;
			SidebarWindow host = Window.GetWindow (this) as SidebarWindow;
			if (host == null || host.DragManager == null) return;
			if (host.IsTileDragging) return;
			host.DragManager.TryStartPending (this, e);
		}
		private bool IsInSplitter (DependencyObject src)
		{
			DependencyObject cur = src;
			while (cur != null)
			{
				if (cur == TileSplitterThumb) return true;
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
		/// <summary>隐藏分隔条，用于拖拽期间避免视觉干扰。</summary>
		internal void HideSplitter ()
		{
			TileSplitterThumb.Visibility = Visibility.Hidden;
		}
		/// <summary>恢复分隔条可见性。</summary>
		internal void ShowSplitter ()
		{
			TileSplitterThumb.ClearValue (UIElement.VisibilityProperty);
		}
		#endregion
		#region Flyout
		private FlyoutDoubleClickHelper _flyoutDoubleClickHelper;
		private DispatcherTimer flyoutOpenTimer = new DispatcherTimer () {
			Interval = TimeSpan.FromSeconds (0.1),
		};
		private void FlyoutOpenTimer_Tick (object sender, EventArgs e)
		{
			flyoutOpenTimer.Stop ();
			OpenFlyout ();
		}
		private void InitFlyoutHelper ()
		{
			flyoutOpenTimer.Tick += FlyoutOpenTimer_Tick;
			_flyoutDoubleClickHelper = new FlyoutDoubleClickHelper (this, this, CustomIgnoreForFlyoutDoubleClick);
			_flyoutDoubleClickHelper.DoubleClicked += FlyoutDoubleClickHelper_DoubleClicked;
			_flyoutDoubleClickHelper.Attach ();
		}
		private void DestroyFlyoutHelper ()
		{
			flyoutOpenTimer.Tick -= FlyoutOpenTimer_Tick;
			if (_flyoutDoubleClickHelper != null)
			{
				_flyoutDoubleClickHelper.DoubleClicked -= FlyoutDoubleClickHelper_DoubleClicked;
				_flyoutDoubleClickHelper.Dispose ();
			}
			_flyoutDoubleClickHelper = null;
		}
		private bool CustomIgnoreForFlyoutDoubleClick (DependencyObject src)
		{
			DependencyObject cur = src;
			while (cur != null && !ReferenceEquals (cur, this))
			{
				if (ReferenceEquals (cur, FlyoutIcon)) return true;
				if (cur is Visual) cur = VisualTreeHelper.GetParent (cur);
				else if (cur is System.Windows.Media.Media3D.Visual3D) cur = VisualTreeHelper.GetParent (cur);
				else cur = LogicalTreeHelper.GetParent (cur);
			}
			return false;
		}
		private void FlyoutDoubleClickHelper_DoubleClicked (object sender, EventArgs e)
		{
			var host = SidebarWindow.Current;
			if (host != null && host.DragManager != null)
				host.DragManager.Cancel ();
			flyoutOpenTimer.Stop ();
			flyoutOpenTimer.Start ();
		}
		private FlyoutWindow flyoutWnd = null;
		public FlyoutWindow CurrentFlyout => flyoutWnd;
		private void OpenFlyout ()
		{
			if (!(AppStorage?.Infomation?.HasFlyout ?? false)) return;
			if (flyoutWnd != null)
			{
				flyoutWnd.Activate ();
				return;
			}
			var wnd = new FlyoutWindow (this);
			flyoutWnd = wnd;
			var header = wnd.FlyoutHeader;
			RoutedEventHandler loadedHandler = null;
			SizeChangedEventHandler sizeChangedHandler = null;
			CancelEventHandler closingHandler = null;
			EventHandler closedHandler = null;
			RoutedEventHandler headerDownHandler = null;
			loadedHandler = (s, e) =>
				Instance?.Instance.OnHostChanged (TileHostEvent.FlyoutShow, tileContext, s);
			sizeChangedHandler = (s, e) =>
				Instance?.Instance.OnHostChanged (TileHostEvent.FlyoutShow, tileContext, s);
			closingHandler = (s, e) =>
				Instance?.Instance.OnHostChanged (TileHostEvent.FlyoutClosing, tileContext, s);
			closedHandler = (s, e) => {
				wnd.Loaded -= loadedHandler;
				wnd.SizeChanged -= sizeChangedHandler;
				wnd.Closing -= closingHandler;
				wnd.Closed -= closedHandler;
				header.Click -= headerDownHandler;
				try { wnd.FlyoutContentContainer?.Children?.Clear (); } catch { }
				if (ReferenceEquals (flyoutWnd, wnd))
				{
					flyoutWnd = null;
					tileContext.FlyoutWindow = null;
					tileContext.FlyoutUI = null;
					tileContext.IsFlyoutShow = false;
				}
				Instance?.Instance.OnHostChanged (TileHostEvent.FlyoutClosed, tileContext, s);
			};
			wnd.Loaded += loadedHandler;
			wnd.SizeChanged += sizeChangedHandler;
			wnd.Closing += closingHandler;
			wnd.Closed += closedHandler;
			header.ContextMenu = this.TileContextMenu;
			header.ContextMenu.PlacementTarget = header;
			headerDownHandler = (s, e) => {
				if (header.ContextMenu != null)
					header.ContextMenu.IsOpen = true;
				e.Handled = true;
			};
			header.Click += headerDownHandler;
			var fve = AppStorage.Infomation.VisualElements.Flyout;
			var inst = Instance.Instance;
			if (inst is KarlSidebarTile || inst is LongBarTile)
			{
				wnd.FlyoutContentContainer.Width = double.NaN;
				wnd.FlyoutContentContainer.Height = double.NaN;
				wnd.SizeToContent = SizeToContent.WidthAndHeight;
			}
			else
			{
				wnd.FlyoutContentContainer.Width = fve.FlyoutWidth;
				wnd.FlyoutContentContainer.Height = fve.FlyoutHeight;
			}
			Flyout_OnInit ();
			wnd.Show ();
		}
		internal void Flyout_OnInit ()
		{
			tileContext.FlyoutWindow = flyoutWnd;
			tileContext.FlyoutUI = flyoutWnd.FlyoutContentContainer;
			tileContext.IsFlyoutShow = false;
			Instance?.Instance.OnHostChanged (TileHostEvent.FlyoutInit, tileContext, this);
		}
		internal void RequireOpenFlyout ()
		{
			if (!(AppStorage?.Infomation?.HasFlyout ?? false)) return;
			flyoutOpenTimer.Stop ();
			flyoutOpenTimer.Start ();
		}
		#endregion
		#region Properties Window
		private TilePropertiesForm propertiesWnd = null;
		private void OpenPropertiesWindow ()
		{
			if (propertiesWnd != null)
			{
				propertiesWnd.Activate ();
				return;
			}
			propertiesWnd = new TilePropertiesForm ();
			tileContext.PropertiesWindow = propertiesWnd;
			tileContext.PropertiesContent = propertiesWnd.TilePropertiesContent;
			string displayName = AppStorage?.DisplayName;
			if (string.IsNullOrEmpty (displayName))
				displayName = AppStorage?.UserModelId ?? "";
			propertiesWnd.Title = string.Format (
				App.ProgramFolder.StringResources.SuitableResource ("TILEPROPERTIES_TITLE"),
				displayName
			);
			RoutedEventHandler loadedHandler = null;
			CancelEventHandler closingHandler = null;
			EventHandler closedHandler = null;
			RoutedEventHandler okClickHandler = null;
			RoutedEventHandler cancelClickHandler = null;
			loadedHandler = (s, e) => {
				Instance?.Instance?.OnHostChanged (TileHostEvent.PropertiesLoad, tileContext, s);
			};
			closingHandler = (s, e) => {
				Instance?.Instance?.OnHostChanged (TileHostEvent.PropertiesClosing, tileContext, s);
			};
			closedHandler = (s, e) => {
				try { propertiesWnd?.TilePropertiesContent?.Children?.Clear (); } catch { }
				if (propertiesWnd != null)
				{
					propertiesWnd.Loaded -= loadedHandler;
					propertiesWnd.Closing -= closingHandler;
					propertiesWnd.Closed -= closedHandler;
					propertiesWnd.TilePropertiesOkButton.Click -= okClickHandler;
					propertiesWnd.TilePropertiesCancelButton.Click -= cancelClickHandler;
				}
				propertiesWnd = null;
				tileContext.PropertiesWindow = null;
				tileContext.PropertiesContent = null;
				Instance?.Instance?.OnHostChanged (TileHostEvent.PropertiesClosed, tileContext, s);
				App.ReleaseLargeResourcesAsync ();
			};
			okClickHandler = (s, e) => {
				try
				{
					Instance?.Instance?.OnHostChanged (TileHostEvent.PropertiesClickOkButton, tileContext, s);
					propertiesWnd.Close ();
				}
				catch (Exception ex)
				{
					MessageBox.Show (propertiesWnd, ex.Message, ex.GetType ().ToString (), MessageBoxButton.OK, MessageBoxImage.Error);
				}
			};
			cancelClickHandler = (s, e) => {
				try
				{
					Instance?.Instance?.OnHostChanged (TileHostEvent.PropertiesClickCancelButton, tileContext, s);
					propertiesWnd.Close ();
				}
				catch (Exception ex)
				{
					MessageBox.Show (propertiesWnd, ex.Message, ex.GetType ().ToString (), MessageBoxButton.OK, MessageBoxImage.Error);
				}
			};
			propertiesWnd.Loaded += loadedHandler;
			propertiesWnd.Closing += closingHandler;
			propertiesWnd.Closed += closedHandler;
			propertiesWnd.TilePropertiesOkButton.Click += okClickHandler;
			propertiesWnd.TilePropertiesCancelButton.Click += cancelClickHandler;
			Instance?.Instance?.OnHostChanged (TileHostEvent.PropertiesInit, tileContext);
			propertiesWnd.Show ();
		}
		#endregion
		#region Require
		/// <summary>根据当前 Config 和 Manifest 计算内容区应处的高度。</summary>
		private double GetTileContentSuggestHeight (double? customHeight = null)
		{
			var vet = AppStorage?.Infomation?.VisualElements?.Tile;
			var configHeight = Instance?.Config?.Height ?? (vet != null ? vet.DefaultHeight : 0);
			if (configHeight == 0 && vet != null) configHeight = vet.DefaultHeight;
			double finalHeight = 0;
			if (configHeight == null || Instance?.Config?.AutoSize == true || vet?.Overflow == TileOverflow.Auto)
			{
				TileContentContainerControl.Height = double.NaN;
				TileContentContainerControl.UpdateLayout ();
				finalHeight = TileContentContainerControl.ActualHeight;
			}
			else finalHeight = customHeight ?? configHeight;
			if (vet?.Overflow == TileOverflow.Auto)
			{
				finalHeight = Math.Max (vet?.MinHeight ?? 20, finalHeight);
			}
			else
			{
				finalHeight = Math.Min (finalHeight, vet?.MaxHeight ?? 32767);
				finalHeight = Math.Max (finalHeight, vet?.MinHeight ?? 20);
			}
			return finalHeight;
		}
		private DoubleAnimation _currentHeightAnimation = null;
		private bool _isHeightAnimating = false;
		private int _heightAnimationVersion = 0;
		private double _lastTargetHeight = double.NaN;
		/// <summary>把组件高度从 elderHeight 补间到 newHeight；返回的 Task 在动画结束或被打断时完成。</summary>
		private Task TransToNewHeight (FrameworkElement component, double elderHeight, double? newHeight = null, TimeSpan? timeout = null)
		{
			var tcs = new TaskCompletionSource<bool> ();
			if (component == null || !component.IsLoaded || component.Visibility == Visibility.Collapsed)
			{
				tcs.SetResult (true);
				return tcs.Task;
			}
			component.UpdateLayout ();
			double targetHeight = newHeight ?? component.ActualHeight;
			if (double.IsNaN (targetHeight) || double.IsInfinity (targetHeight) || targetHeight < 0 ||
				double.IsNaN (elderHeight) || double.IsInfinity (elderHeight) || elderHeight < 0)
			{
				tcs.SetResult (true);
				return tcs.Task;
			}
			if (Math.Abs (targetHeight - elderHeight) < 0.01)
			{
				tcs.SetResult (true);
				return tcs.Task;
			}
			int version = ++_heightAnimationVersion;
			if (_isHeightAnimating)
			{
				component.Height = _lastTargetHeight;
				component.UpdateLayout ();
			}
			double originalHeight = component.Height;
			_lastTargetHeight = targetHeight;
			component.BeginAnimation (FrameworkElement.HeightProperty, null);
			_isHeightAnimating = true;
			component.Height = elderHeight;
			var animation = new DoubleAnimation {
				From = elderHeight,
				To = targetHeight,
				Duration = timeout ?? TimeSpan.FromSeconds (0.4),
				FillBehavior = FillBehavior.Stop
			};
			EventHandler completedHandler = null;
			completedHandler = (s, e) => {
				animation.Completed -= completedHandler;
				if (version != _heightAnimationVersion)
				{
					tcs.TrySetCanceled ();
					return;
				}
				component.BeginAnimation (FrameworkElement.HeightProperty, null);
				component.Height = originalHeight;
				_isHeightAnimating = false;
				tcs.TrySetResult (true);
			};
			animation.Completed += completedHandler;
			_currentHeightAnimation = animation;
			component.BeginAnimation (FrameworkElement.HeightProperty, animation, HandoffBehavior.SnapshotAndReplace);
			return tcs.Task;
		}
		private DoubleAnimation _currentOpacityAnimation = null;
		private int _opacityAnimationVersion = 0;
		/// <summary>把组件的透明度从 elderOpacity 补间到 newOpacity；返回的 Task 在动画结束或被打断时完成。</summary>
		private Task TransToNewOpacity (FrameworkElement component, double elderOpacity, double? newOpacity = null, TimeSpan? timeout = null)
		{
			var tcs = new TaskCompletionSource<bool> ();
			if (component == null)
			{
				tcs.SetResult (true);
				return tcs.Task;
			}
			if (!component.IsLoaded)
			{
				tcs.SetResult (true);
				return tcs.Task;
			}
			double targetOpacity = newOpacity ?? component.Opacity;
			if (double.IsNaN (targetOpacity) || double.IsInfinity (targetOpacity) ||
				double.IsNaN (elderOpacity) || double.IsInfinity (elderOpacity))
			{
				tcs.SetResult (true);
				return tcs.Task;
			}
			targetOpacity = Math.Max (0, Math.Min (1, targetOpacity));
			elderOpacity = Math.Max (0, Math.Min (1, elderOpacity));
			if (Math.Abs (targetOpacity - elderOpacity) < 0.001)
			{
				tcs.SetResult (true);
				return tcs.Task;
			}
			int version = ++_opacityAnimationVersion;
			double originalOpacity = component.Opacity;
			component.BeginAnimation (UIElement.OpacityProperty, null);
			component.Opacity = elderOpacity;
			var animation = new DoubleAnimation {
				From = elderOpacity,
				To = targetOpacity,
				Duration = timeout ?? TimeSpan.FromSeconds (0.4),
				FillBehavior = FillBehavior.Stop
			};
			EventHandler completedHandler = null;
			completedHandler = (s, e) => {
				animation.Completed -= completedHandler;
				if (version != _opacityAnimationVersion)
				{
					tcs.TrySetCanceled ();
					return;
				}
				component.BeginAnimation (UIElement.OpacityProperty, null);
				component.Opacity = originalOpacity;
				tcs.TrySetResult (true);
			};
			animation.Completed += completedHandler;
			_currentOpacityAnimation = animation;
			component.BeginAnimation (UIElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);
			return tcs.Task;
		}
		/// <summary>立刻结束指定动画，把动画的终值直接写入目标属性。</summary>
		public void CompleteAnimationImmediately (FrameworkElement target, DependencyProperty property, DoubleAnimation animation)
		{
			if (animation == null) return;
			double endValue = animation.To ?? (double)target.GetValue (property) + (animation.By ?? 0);
			target.BeginAnimation (property, null);
			target.SetValue (property, endValue);
		}
		/// <summary>停止当前 AutoSize 自动高度补间并复位 isanimating。</summary>
		private void StopAutoSizeStoryboard ()
		{
			try
			{
				var sb = Resources ["TileHeightChangeAnimation"] as Storyboard;
				if (sb != null) sb.Stop ();
			}
			catch { }
			isanimating = false;
		}
		/// <summary>由磁贴程序请求把内容高度设为 customHeight（受 AutoSize / Overflow 约束，带动画）。</summary>
		public void RequireSetTileContentHeight (double customHeight)
		{
			StopAutoSizeStoryboard ();
			CompleteAnimationImmediately (TileContentContainerControl, HeightProperty, _currentHeightAnimation);
			var s = GetTileContentSuggestHeight (customHeight);
			var elder = TileContentContainerControl.ActualHeight;
			TileContentContainerControl.Height = s;
			TransToNewHeight (TileContentContainerControl, elder, s).ContinueWith (task => {
				if (TileContentContainerControl == null) return;
				Dispatcher.Invoke (new Action (() => TileContentContainerControl.Height = s));
			});
		}
		/// <summary>由磁贴程序请求把内容高度设为 customHeight（忽略 AutoSize，带动画）。</summary>
		public void RequireSetTileContentHeightIgnoreAutoSize (double customHeight)
		{
			StopAutoSizeStoryboard ();
			TargetHeightWhenAutoSize = customHeight;
			TileContentContainerControl.BeginAnimation (FrameworkElement.HeightProperty, null);
			CompleteAnimationImmediately (TileContentContainerControl, HeightProperty, _currentHeightAnimation);
			var elder = TileContentContainerControl.ActualHeight;
			TileContentContainerControl.Height = customHeight;
			TimeoutExtensions.SetTimeout (this, () => {
				TransToNewHeight (TileContentContainerControl, elder, customHeight).ContinueWith (task => {
					if (TileContentContainerControl == null) return;
					Dispatcher.Invoke (new Action (() => TileContentContainerControl.Height = customHeight));
				});
			}, 100);
		}
		/// <summary>由磁贴程序请求把内容高度设为 customHeight 并持久化到 Config。</summary>
		public void RequireSetTileContentHeightAndSave (double customHeight)
		{
			StopAutoSizeStoryboard ();
			CompleteAnimationImmediately (TileContentContainerControl, HeightProperty, _currentHeightAnimation);
			try { Instance.Config.Height = customHeight; } catch { }
			var elder = TileContentContainerControl.ActualHeight;
			TileContentContainerControl.Height = customHeight;
			TransToNewHeight (TileContentContainerControl, elder, customHeight).ContinueWith (task => {
				if (TileContentContainerControl == null) return;
				Dispatcher.Invoke (new Action (() => TileContentContainerControl.Height = customHeight));
			});
		}
		/// <summary>由磁贴程序请求对内容区播放一次淡入动画；target 为动画时长（秒），null 时使用默认 0.4s。</summary>
		public void RequireLoadTileOpacityAnime (double? target = null)
		{
			CompleteAnimationImmediately (TileContentContainerControl, OpacityProperty, _currentOpacityAnimation);
			if (target == null)
				TransToNewOpacity (TileContentContainerControl, 0);
			else
				TransToNewOpacity (TileContentContainerControl, 0, null, TimeSpan.FromSeconds (target ?? 0.4));
		}
		#endregion
		private void RootTileElement_Loaded (object sender, RoutedEventArgs e)
		{
			Instance?.Instance?.OnLoad ();
			InitHeight ();
		}
		private void RootTileElement_Unloaded (object sender, RoutedEventArgs e)
		{
			Instance?.Instance?.OnUnload ();
		}
		public override string ToString () => UserModelId ?? base.ToString ();
	}
	public struct TileVisualView: ITileElement
	{
		private ITileElement ite;
		public TileControl Element => ite as TileControl;
		public Panel ContentContainer => ite?.ContentContainer;
		public string DisplayName => ite?.DisplayName;
		public ImageSource Icon => ite?.Icon;
		public string UserModelId => ite?.UserModelId;
		public override bool Equals (object obj)
		{
			if (obj is ITileElement) return ((ITileElement)obj).UserModelId.Equals (UserModelId);
			return base.Equals (obj);
		}
		public override int GetHashCode ()
		{
			return UserModelId?.GetHashCode () ?? base.GetHashCode ();
		}
		public override string ToString () => $"TileVisualView[{UserModelId}]";
		public TileVisualView (ITileElement _ite) { ite = _ite; }
	}
	public enum TileContextMenuSource
	{
		Normal,
		Taskbar,
		Overflow
	}
}