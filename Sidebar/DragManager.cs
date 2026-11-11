using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Sidebar
{
	/// <summary>磁贴拖拽重排的写回宿主，负责把拖拽结果转换为配置层的 Move 操作。</summary>
	internal interface ITileReorderHost
	{
		/// <summary>把磁贴移动到指定面板的指定索引处；返回是否真的发生了位置变化。</summary>
		bool MoveTileToPanelIndex (string userModelId, Panel panel, int panelIndex);
	}
	internal enum TileDragDevice { Mouse, Stylus, Touch }
	/// <summary>侧边栏磁贴拖拽重排管理器，采用 Longhorn 风格的实时让位算法，支持鼠标、触屏笔与触摸。</summary>
	internal sealed class TileDragManager
	{
		private const double DragThreshold = 5.0;
		// 让位动画：短时长 + 缓出，磁贴跟随光标移动，同时保留一点柔性避免生硬。
		private const double DragMarginAnimMs = 80.0;
		// 归位动画：稍长一点，让"让位残余"落回零的过程中有轻微缓冲。
		private const double ReleaseMarginAnimMs = 240.0;
		// 浮动窗口落位动画：单段缓出，全程平滑减速，避免"先停滞再猛进"。
		private const double ReleaseWindowMs = 420.0;

		private readonly SidebarWindow _host;
		private readonly ITileReorderHost _reorder;
		private TileControl _source;
		private Panel _panel;
		private int _sourceIndex;
		private int _indexDrag = -1;
		private int _indexDrop = -1;
		private double _sourceHeight;
		private double _dpiX = 1.0;
		private double _dpiY = 1.0;
		private Point _startInWindow;
		private Point _grabOffset;
		private DragWindow _window;
		private bool _isDragging;
		private bool _isPending;
		private bool _isCompletingDrag;
		private TileDragDevice _device;
		private StylusDevice _stylus;
		private TouchDevice _touch;
		/// <summary>是否已经进入拖拽进行中状态。</summary>
		public bool IsDragging { get { return _isDragging; } }
		/// <summary>是否处于已按下但未超过阈值的待拖状态。</summary>
		public bool IsPending { get { return _isPending; } }
		public TileDragManager (SidebarWindow host)
		{
			if (host == null) throw new ArgumentNullException ("host");
			_host = host;
			_reorder = host as ITileReorderHost;
			if (_reorder == null) throw new InvalidOperationException ("SidebarWindow must implement ITileReorderHost.");
		}
		/// <summary>鼠标左键按下时进入待拖状态。</summary>
		public bool TryStartPending (TileControl source, MouseButtonEventArgs e)
		{
			if (e.StylusDevice != null) return false;
			if (e.ChangedButton != MouseButton.Left) return false;
			if (e.ClickCount != 1) return false;
			if (IsInteractiveElement (e.OriginalSource as DependencyObject, source)) return false;
			return StartPending (source, e.GetPosition (_host), TileDragDevice.Mouse);
		}
		/// <summary>触屏笔按下时进入待拖状态；触摸类型的 StylusDown 会被忽略，交给 Touch 路径处理。</summary>
		public bool TryStartPending (TileControl source, StylusDownEventArgs e)
		{
			StylusDevice dev = e.StylusDevice;
			if (dev == null || dev.TabletDevice == null) return false;
			if (dev.TabletDevice.Type == TabletDeviceType.Touch) return false;
			if (dev.InAir) return false;
			if (IsInteractiveElement (e.OriginalSource as DependencyObject, source)) return false;
			if (!StartPending (source, e.GetPosition (_host), TileDragDevice.Stylus)) return false;
			_stylus = dev;
			return true;
		}
		/// <summary>触摸按下时进入待拖状态，只有首次触点有效。</summary>
		public bool TryStartPending (TileControl source, TouchEventArgs e)
		{
			TouchDevice dev = e.TouchDevice;
			if (dev == null || dev.Captured != null) return false;
			if (IsInteractiveElement (e.OriginalSource as DependencyObject, source)) return false;
			if (!StartPending (source, e.GetTouchPoint (_host).Position, TileDragDevice.Touch)) return false;
			_touch = dev;
			return true;
		}
		private bool StartPending (TileControl source, Point pos, TileDragDevice device)
		{
			if (_isPending || _isDragging || _isCompletingDrag) return false;
			if (source == null) return false;
			if (source.AppStorage == null) return false;
			if (source.Instance == null || source.Instance.Config == null) return false;
			_source = source;
			_startInWindow = pos;
			_device = device;
			_isPending = true;
			_indexDrag = -1;
			_indexDrop = -1;
			return true;
		}
		/// <summary>由 SidebarWindow 在 PreviewMouseMove 中调用。</summary>
		public void OnMouseMove (MouseEventArgs e)
		{
			if (_isCompletingDrag) return;
			if (!_isPending && !_isDragging) return;
			if (_device != TileDragDevice.Mouse) return;
			if (!_isDragging)
			{
				if (e.LeftButton != MouseButtonState.Pressed) { Cancel (); return; }
				Point p = e.GetPosition (_host);
				if (!PassThreshold (p)) return;
				BeginDrag (p);
			}
			if (_isDragging)
			{
				UpdateDrag (e.GetPosition (_host));
				e.Handled = true;
			}
		}
		/// <summary>由 SidebarWindow 在 PreviewStylusMove 中调用。</summary>
		public void OnStylusMove (StylusEventArgs e)
		{
			if (_isCompletingDrag) return;
			if (!_isPending && !_isDragging) return;
			if (_device != TileDragDevice.Stylus) return;
			StylusDevice dev = e.StylusDevice;
			if (dev == null) return;
			if (dev.TabletDevice != null && dev.TabletDevice.Type == TabletDeviceType.Touch) return;
			if (!_isDragging)
			{
				if (dev.InAir) { Cancel (); return; }
				Point p = e.GetPosition (_host);
				if (!PassThreshold (p)) return;
				BeginDrag (p);
			}
			if (_isDragging)
			{
				UpdateDrag (e.GetPosition (_host));
				e.Handled = true;
			}
		}
		/// <summary>由 SidebarWindow 在 PreviewTouchMove 中调用。</summary>
		public void OnTouchMove (TouchEventArgs e)
		{
			if (_isCompletingDrag) return;
			if (!_isPending && !_isDragging) return;
			if (_device != TileDragDevice.Touch) return;
			TouchDevice dev = e.TouchDevice;
			if (dev == null || !ReferenceEquals (dev, _touch)) return;
			if (!_isDragging)
			{
				Point p = e.GetTouchPoint (_host).Position;
				if (!PassThreshold (p)) return;
				BeginDrag (p);
			}
			if (_isDragging)
			{
				UpdateDrag (e.GetTouchPoint (_host).Position);
				e.Handled = true;
			}
		}
		/// <summary>由 SidebarWindow 在 PreviewMouseLeftButtonUp 中调用。</summary>
		public void OnMouseUp (MouseButtonEventArgs e)
		{
			if (_device != TileDragDevice.Mouse) return;
			if (_isCompletingDrag) { e.Handled = true; return; }
			if (!_isPending && !_isDragging) return;
			if (_isDragging) CompleteDrag (); else Cancel ();
			e.Handled = true;
		}
		/// <summary>由 SidebarWindow 在 PreviewStylusUp 中调用。</summary>
		public void OnStylusUp (StylusEventArgs e)
		{
			if (_device != TileDragDevice.Stylus) return;
			if (_isCompletingDrag) { e.Handled = true; return; }
			if (!_isPending && !_isDragging) return;
			if (_isDragging) CompleteDrag (); else Cancel ();
			e.Handled = true;
		}
		/// <summary>由 SidebarWindow 在 PreviewTouchUp 中调用。</summary>
		public void OnTouchUp (TouchEventArgs e)
		{
			if (_device != TileDragDevice.Touch) return;
			if (_isCompletingDrag) { e.Handled = true; return; }
			if (!_isPending && !_isDragging) return;
			TouchDevice dev = e.TouchDevice;
			if (dev == null || !ReferenceEquals (dev, _touch)) return;
			if (_isDragging) CompleteDrag (); else Cancel ();
			e.Handled = true;
		}
		/// <summary>取消当前拖拽并恢复所有状态。</summary>
		public void Cancel ()
		{
			if (!_isPending && !_isDragging) return;
			if (_isDragging) RestoreSourcePosition ();
			RestoreAllSplitters ();
			CloseWindow ();
			ReleaseCapture ();
			_source = null;
			_panel = null;
			_indexDrag = -1;
			_indexDrop = -1;
			_isDragging = false;
			_isPending = false;
			_isCompletingDrag = false;
		}
		private bool PassThreshold (Point p)
		{
			double dx = p.X - _startInWindow.X;
			double dy = p.Y - _startInWindow.Y;
			return dx * dx + dy * dy >= DragThreshold * DragThreshold;
		}
		private void ReleaseCapture ()
		{
			if (_touch != null) { try { _touch.Capture (null); } catch { } _touch = null; }
			if (_stylus != null) { try { _stylus.Capture (null); } catch { } _stylus = null; }
			if (Mouse.Captured != null) Mouse.Capture (null);
		}
		private void CloseWindow ()
		{
			if (_window != null) { try { _window.Close (); } catch { } _window = null; }
		}
		private void BeginDrag (Point currentInWindow)
		{
			_isDragging = true;
			_isPending = false;
			_panel = _source.Parent as Panel;
			if (_panel == null) { Cancel (); return; }
			_sourceIndex = _panel.Children.IndexOf (_source);
			if (_sourceIndex < 0) { Cancel (); return; }
			_sourceHeight = _source.ActualHeight;
			if (double.IsNaN (_sourceHeight) || _sourceHeight <= 0) _sourceHeight = _source.DesiredSize.Height;
			if (double.IsNaN (_sourceHeight) || _sourceHeight <= 0) _sourceHeight = 60;
			Point topLeft = _source.TranslatePoint (new Point (0, 0), _host);
			_grabOffset = new Point (currentInWindow.X - topLeft.X, currentInWindow.Y - topLeft.Y);
			PresentationSource src = PresentationSource.FromVisual (_host);
			if (src != null && src.CompositionTarget != null)
			{
				_dpiX = src.CompositionTarget.TransformToDevice.M11;
				_dpiY = src.CompositionTarget.TransformToDevice.M22;
			}
			else
			{
				_dpiX = 1.0;
				_dpiY = 1.0;
			}
			HideSourceSplitter ();
			_window = new DragWindow {
				Owner = _host,
				WindowStartupLocation = WindowStartupLocation.Manual,
				ShowInTaskbar = false,
				Topmost = true,
				IsDragHost = true
			};
			Thickness cm = _window.ContentMargin;
			_window.Width = _source.ActualWidth + cm.Left + cm.Right;
			_window.Height = _source.ActualHeight + cm.Top + cm.Bottom;
			VisualBrush brush = new VisualBrush (_source) {
				Stretch = Stretch.None,
				AlignmentX = AlignmentX.Left,
				AlignmentY = AlignmentY.Top
			};
			_window.SetContent (new Rectangle {
				Width = _source.ActualWidth,
				Height = _source.ActualHeight,
				Fill = brush,
				IsHitTestVisible = false
			});
			MoveWindow (currentInWindow);
			_window.Show ();
			if (_device == TileDragDevice.Touch && _touch != null)
			{
				try { _touch.Capture (_host); } catch { }
			}
			else if (_device == TileDragDevice.Stylus && _stylus != null)
			{
				try { _stylus.Capture (_host); } catch { }
			}
			else
			{
				Mouse.Capture (_host, CaptureMode.Element);
			}
			_panel.Children.Remove (_source);
			if (_sourceIndex < _panel.Children.Count)
			{
				TileControl next = _panel.Children [_sourceIndex] as TileControl;
				if (next != null) AnimateMargin (next, _sourceHeight);
			}
			UpdateDrag (currentInWindow);
		}
		private void UpdateDrag (Point currentInWindow)
		{
			MoveWindow (currentInWindow);
			UpdateDropIndex (currentInWindow);
		}
		/// <summary>
		/// 拖拽期间用 WPF 的 Window.Left/Top（DIP 单位）定位浮窗。
		/// currentInWindow 是相对宿主客户区的 DIP；PointToScreen 拿到物理像素，
		/// 除以宿主 DPI 缩放换算回屏幕 DIP。这是 WPF 官方推荐的换算链路，
		/// 也避免了 SetWindowPos 在 DPI 感知模式下的坐标系歧义。
		/// </summary>
		private void MoveWindow (Point currentInWindow)
		{
			if (_window == null) return;
			Point screenPhysical = _host.PointToScreen (currentInWindow);
			double logicalX = screenPhysical.X / _dpiX;
			double logicalY = screenPhysical.Y / _dpiY;
			Thickness cm = _window.ContentMargin;
			_window.Left = logicalX - _grabOffset.X - cm.Left;
			_window.Top = logicalY - _grabOffset.Y - cm.Top;
		}
		private void UpdateDropIndex (Point currentInWindow)
		{
			if (_panel == null) return;
			Point p;
			try { p = _host.TranslatePoint (currentInWindow, _panel); }
			catch { return; }
			int visibleIndex = 0;
			double acc = 0;
			int newDrag = -1;
			int newDrop = -1;
			double t = 0;
			foreach (UIElement child in _panel.Children)
			{
				TileControl tc = child as TileControl;
				if (tc == null || tc == _source) continue;
				double ch = tc.ActualHeight;
				if (ch <= 0) { visibleIndex++; continue; }
				if (p.Y < acc + ch)
				{
					newDrag = visibleIndex;
					t = (p.Y - acc) / ch;
					if (t < 0) t = 0;
					if (t > 1) t = 1;
					newDrop = t < 0.5 ? visibleIndex : visibleIndex + 1;
					break;
				}
				acc += ch;
				visibleIndex++;
			}
			if (newDrag == -1)
			{
				newDrag = visibleIndex;
				newDrop = visibleIndex;
				t = 0;
			}
			_indexDrag = newDrag;
			_indexDrop = newDrop;
			ApplyMarginLayout (t);
		}
		private void ApplyMarginLayout (double t)
		{
			if (_panel == null) return;
			double h = _sourceHeight;
			double marginDrag = h * (1 - t);
			double marginNext = h * t;
			int visibleIndex = 0;
			foreach (UIElement child in _panel.Children)
			{
				TileControl tc = child as TileControl;
				if (tc == null || tc == _source) continue;
				double margin = 0;
				if (visibleIndex == _indexDrag) margin = marginDrag;
				else if (visibleIndex == _indexDrag + 1) margin = marginNext;
				AnimateMargin (tc, margin);
				visibleIndex++;
			}
		}
		/// <summary>让位动画：短时长 + 缓出，磁贴跟上光标但不硬切。</summary>
		private void AnimateMargin (TileControl tile, double top)
		{
			Thickness current = tile.Margin;
			if (Math.Abs (current.Top - top) < 0.01) return;
			Thickness target = new Thickness (current.Left, top, current.Right, current.Bottom);
			ThicknessAnimation anim = new ThicknessAnimation {
				From = current,
				To = target,
				Duration = TimeSpan.FromMilliseconds (DragMarginAnimMs),
				FillBehavior = FillBehavior.Stop,
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
			};
			EventHandler completed = null;
			completed = (s, e) => {
				anim.Completed -= completed;
				tile.BeginAnimation (FrameworkElement.MarginProperty, null);
				tile.Margin = target;
			};
			anim.Completed += completed;
			tile.BeginAnimation (FrameworkElement.MarginProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}
		private void CompleteDrag ()
		{
			if (_panel == null || _source == null)
			{
				FinishDragCleanup ();
				return;
			}
			string umid = _source.UserModelId;
			int insertIndex = _indexDrop;
			if (insertIndex < 0) insertIndex = 0;
			if (insertIndex > _panel.Children.Count) insertIndex = _panel.Children.Count;
			_isCompletingDrag = true;
			StartReleaseAnimation (insertIndex, umid);
		}
		/// <summary>拖拽收尾：浮窗单段缓出到目标位置，到位后清 Margin、插入源、同步配置。</summary>
		private void StartReleaseAnimation (int insertIndex, string userModelId)
		{
			if (_window == null)
			{
				AnimateMarginsToZeroAndInsert (insertIndex, userModelId);
				FinishDragCleanup ();
				return;
			}
			double targetLeft;
			double targetTop;
			if (!TryComputeDropWindowPosition (insertIndex, out targetLeft, out targetTop))
			{
				AnimateMarginsToZeroAndInsert (insertIndex, userModelId);
				FinishDragCleanup ();
				return;
			}
			double startLeft = _window.Left;
			double startTop = _window.Top;
			double dx = targetLeft - startLeft;
			double dy = targetTop - startTop;

			// 单段 CubicEase.EaseOut：起步快、收尾缓，落位全程连续减速。
			DoubleAnimation ax = new DoubleAnimation {
				From = startLeft,
				To = targetLeft,
				Duration = TimeSpan.FromMilliseconds (ReleaseWindowMs),
				FillBehavior = FillBehavior.Stop,
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};
			DoubleAnimation ay = new DoubleAnimation {
				From = startTop,
				To = targetTop,
				Duration = TimeSpan.FromMilliseconds (ReleaseWindowMs),
				FillBehavior = FillBehavior.Stop,
				EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
			};
			ay.Completed += (s, e) => {
				if (_window != null)
				{
					_window.BeginAnimation (Window.LeftProperty, null);
					_window.BeginAnimation (Window.TopProperty, null);
					_window.Left = targetLeft;
					_window.Top = targetTop;
				}
				AnimateMarginsToZeroAndInsert (insertIndex, userModelId);
				FinishDragCleanup ();
			};
			_window.BeginAnimation (Window.LeftProperty, ax, HandoffBehavior.SnapshotAndReplace);
			_window.BeginAnimation (Window.TopProperty, ay, HandoffBehavior.SnapshotAndReplace);
		}
		/// <summary>
		/// 计算浮动窗口在目标索引处应当处于的屏幕 DIP 坐标。
		///
		/// 两个关键点：
		/// 1) dropY 累加只加 ActualHeight，忽略 Margin.Top —— 让位期间这个 margin 是临时撑开的空间，
		///    而 AnimateMarginsToZeroAndInsert 会让它归零，源磁贴的**最终**位置就是"归零布局"的位置。
		/// 2) 窗口位置 = 目标内容位置 - ContentMargin，**绝不能再减 _grabOffset**。
		///    _grabOffset 只在拖拽期间用来让浮窗内容对齐光标，释放落位时目标是面板内的槽位而非光标位置。
		/// </summary>
		private bool TryComputeDropWindowPosition (int insertIndex, out double targetLeft, out double targetTop)
		{
			targetLeft = 0;
			targetTop = 0;
			if (_panel == null || _window == null) return false;

			double dropYInPanel = 0;
			int visibleIndex = 0;
			foreach (UIElement child in _panel.Children)
			{
				TileControl tc = child as TileControl;
				if (tc == null || tc == _source) continue;
				if (visibleIndex >= insertIndex) break;
				dropYInPanel += tc.ActualHeight;
				visibleIndex++;
			}

			Point panelOriginInWindow;
			try { panelOriginInWindow = _panel.TranslatePoint (new Point (0, 0), _host); }
			catch { return false; }
			Point anchorInWindow = new Point (panelOriginInWindow.X, panelOriginInWindow.Y + dropYInPanel);

			Point anchorPhysical = _host.PointToScreen (anchorInWindow);
			double anchorLogicalX = anchorPhysical.X / _dpiX;
			double anchorLogicalY = anchorPhysical.Y / _dpiY;

			Thickness cm = _window.ContentMargin;
			targetLeft = anchorLogicalX - cm.Left;
			targetTop = anchorLogicalY - cm.Top;
			return true;
		}
		private void FinishDragCleanup ()
		{
			RestoreAllSplitters ();
			CloseWindow ();
			ReleaseCapture ();
			_source = null;
			_panel = null;
			_indexDrag = -1;
			_indexDrop = -1;
			_isDragging = false;
			_isPending = false;
			_isCompletingDrag = false;
		}
		/// <summary>把源插回目标索引，对每个非 0 Margin 的磁贴启动归零动画；动画结束后同步配置。</summary>
		private void AnimateMarginsToZeroAndInsert (int insertIndex, string userModelId)
		{
			Panel panel = _panel;
			TileControl source = _source;
			if (panel == null || source == null) return;
			if (!panel.Children.Contains (source))
			{
				int back = insertIndex;
				if (back < 0) back = 0;
				if (back > panel.Children.Count) back = panel.Children.Count;
				panel.Children.Insert (back, source);
			}
			// 源下方的磁贴瞬间归零，避免与源之间出现 240ms 才收拢的空隙
			TileControl below = null;
			int belowIndex = insertIndex + 1;
			if (belowIndex >= 0 && belowIndex < panel.Children.Count)
			{
				below = panel.Children [belowIndex] as TileControl;
				if (below != null && below != source)
				{
					below.BeginAnimation (FrameworkElement.MarginProperty, null);
					Thickness m = below.Margin;
					below.Margin = new Thickness (m.Left, 0, m.Right, m.Bottom);
				}
			}
			List<TileControl> affected = new List<TileControl> ();
			foreach (UIElement child in panel.Children)
			{
				TileControl tc = child as TileControl;
				if (tc == null || tc == source || tc == below) continue;
				if (tc.Margin.Top != 0) affected.Add (tc);
			}
			if (affected.Count == 0)
			{
				if (!string.IsNullOrWhiteSpace (userModelId))
					_reorder.MoveTileToPanelIndex (userModelId, panel, insertIndex);
				return;
			}
			Storyboard sb = new Storyboard ();
			for (int i = 0; i < affected.Count; i++)
			{
				TileControl tc = affected [i];
				Thickness from = tc.Margin;
				Thickness to = new Thickness (from.Left, 0, from.Right, from.Bottom);
				ThicknessAnimation anim = new ThicknessAnimation {
					From = from,
					To = to,
					Duration = TimeSpan.FromMilliseconds (ReleaseMarginAnimMs),
					FillBehavior = FillBehavior.Stop,
					EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
				};
				Storyboard.SetTarget (anim, tc);
				Storyboard.SetTargetProperty (anim, new PropertyPath (FrameworkElement.MarginProperty));
				sb.Children.Add (anim);
			}
			sb.Completed += (s, e) => {
				for (int i = 0; i < affected.Count; i++)
				{
					TileControl tc = affected [i];
					tc.BeginAnimation (FrameworkElement.MarginProperty, null);
					tc.ClearValue (FrameworkElement.MarginProperty);
				}
				if (!string.IsNullOrWhiteSpace (userModelId))
				{
					_reorder.MoveTileToPanelIndex (userModelId, panel, insertIndex);
				}
			};
			sb.Begin ();
		}
		private void RestoreSourcePosition ()
		{
			if (_panel == null || _source == null) return;
			ClearAllMargins ();
			if (_panel.Children.Contains (_source)) return;
			int insertIndex = _sourceIndex;
			if (insertIndex < 0) insertIndex = 0;
			if (insertIndex > _panel.Children.Count) insertIndex = _panel.Children.Count;
			_panel.Children.Insert (insertIndex, _source);
		}
		private void ClearAllMargins ()
		{
			if (_panel == null) return;
			foreach (UIElement child in _panel.Children)
			{
				TileControl tc = child as TileControl;
				if (tc == null) continue;
				tc.BeginAnimation (FrameworkElement.MarginProperty, null);
				tc.ClearValue (FrameworkElement.MarginProperty);
			}
		}
		private static bool IsInteractiveElement (DependencyObject src, DependencyObject root)
		{
			DependencyObject cur = src;
			while (cur != null && !ReferenceEquals (cur, root))
			{
				if (cur is Thumb) return true;
				if (cur is ScrollBar) return true;
				if (cur is ButtonBase) return true;
				if (cur is TextBoxBase) return true;
				if (cur is Selector) return true;
				if (cur is Slider) return true;
				cur = GetParentSafe (cur);
			}
			return false;
		}
		/// <summary>隐藏被拖拽磁贴的分隔条；在抓快照前调用，避免浮窗带上分隔线。</summary>
		private void HideSourceSplitter ()
		{
			if (_source != null) _source.HideSplitter ();
		}
		/// <summary>恢复面板内所有磁贴（含源）的分隔条可见性。</summary>
		private void RestoreAllSplitters ()
		{
			if (_panel != null)
			{
				foreach (UIElement child in _panel.Children)
				{
					TileControl tc = child as TileControl;
					if (tc != null) tc.ShowSplitter ();
				}
			}
			if (_source != null) _source.ShowSplitter ();
		}
		/// <summary>安全获取父节点：Visual/Visual3D 走 VisualTreeHelper，其余走 LogicalTreeHelper。</summary>
		private static DependencyObject GetParentSafe (DependencyObject cur)
		{
			if (cur == null) return null;
			if (cur is Visual) return VisualTreeHelper.GetParent (cur);
			if (cur is System.Windows.Media.Media3D.Visual3D) return VisualTreeHelper.GetParent (cur);
			return LogicalTreeHelper.GetParent (cur);
		}
	}
}