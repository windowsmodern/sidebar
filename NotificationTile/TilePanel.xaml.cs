using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WindowsModern.NotificationHistoryTile
{
	/// <summary>
	/// 通知图标点击事件的自定义委托。
	/// </summary>
	public delegate void NotificationClickedHandler (object sender, NotificationStoreData data);

	public partial class TilePanel: UserControl
	{
		private int MaxVisible
		{
			get
			{
				try
				{
					var element = NotificationPanelProp;
					if (element == null) return 8;
					if (double.IsNaN (element.Width)) return 8;
					if (element.Width < 1) return 8;
					return (int)element.Width;
				}
				catch
				{
					return 8;
				}
			}
		}
		private int MaxStackSize
		{
			get
			{
				try
				{
					var element = NotificationPanelProp;
					if (element == null) return 4;
					if (double.IsNaN (element.Height)) return 4;
					if (element.Height < 1) return 4;
					return (int)element.Height;
				}
				catch
				{
					return 4;
				}
			}
		}
		private double StackOffset
		{
			get
			{
				try
				{
					var element = NotificationButtonProp;
					if (element == null) return 4;
					if (double.IsNaN (element.Width)) return 4;
					return element.Width;
				}
				catch
				{
					return 4;
				}
			}
		}
		private double StackMergeDistance
		{
			get
			{
				try
				{
					var element = NotificationButtonProp;
					if (element == null) return 20;
					if (double.IsNaN (element.Height)) return 20;
					return element.Height;
				}
				catch
				{
					return 20;
				}
			}
		}
		private const double MinimumTimeSpanMinutes = 30.0;

		private readonly DispatcherTimer _timer;
		private readonly DispatcherTimer _sizeChangedTimer;
		private readonly List<Entry> _entries = new List<Entry> ();
		private readonly List<NotificationStack> _stacks = new List<NotificationStack> ();

		private double _panelWidth;
		private double _panelHeight;
		private bool _isLoaded;
		private bool _layoutQueued;

		private double _buttonWidth = 19.0;
		private double _buttonHeight = 16.0;

		private sealed class Entry
		{
			public Button Button;
			public NotificationStoreData Data;
			public double DesiredOffset;
		}

		private sealed class NotificationStack
		{
			public readonly List<Entry> Entries = new List<Entry> ();
			public double Offset;
		}

		// ============================================================
		// History 依赖属性
		// ============================================================
		public static readonly DependencyProperty HistoryProperty =
			DependencyProperty.Register (
				"History",
				typeof (NotificationHistory),
				typeof (TilePanel),
				new PropertyMetadata (null, OnHistoryPropertyChanged));

		public NotificationHistory History
		{
			get { return (NotificationHistory)GetValue (HistoryProperty); }
			set { SetValue (HistoryProperty, value); }
		}

		/// <summary>
		/// 点击通知图标时触发。
		/// </summary>
		public event NotificationClickedHandler NotificationClicked;

		public TilePanel ()
		{
			InitializeComponent ();

			_timer = new DispatcherTimer ();
			_timer.Interval = TimeSpan.FromMinutes (1.0);
			_sizeChangedTimer = new DispatcherTimer ();
			_sizeChangedTimer.Interval = TimeSpan.FromMilliseconds (300);
		}

		// ============================================================
		// History 依赖属性变化
		// ============================================================
		private static void OnHistoryPropertyChanged (
			DependencyObject d,
			DependencyPropertyChangedEventArgs e)
		{
			TilePanel self = (TilePanel)d;

			NotificationHistory oldHistory =
				e.OldValue as NotificationHistory;

			if (oldHistory != null)
				oldHistory.CollectionChanged -= self.OnHistoryCollectionChanged;

			NotificationHistory newHistory =
				e.NewValue as NotificationHistory;

			if (newHistory != null)
				newHistory.CollectionChanged += self.OnHistoryCollectionChanged;

			if (self.IsLoaded)
				self.RebuildChildren ();
		}

		private void OnHistoryCollectionChanged (
			object sender,
			NotifyCollectionChangedEventArgs e)
		{
			if (!Dispatcher.CheckAccess ())
			{
				Dispatcher.BeginInvoke (new Action (RebuildChildren));
				return;
			}

			RebuildChildren ();
		}

		// ============================================================
		// 生命周期
		// ============================================================
		private void UserControl_Loaded (object sender, RoutedEventArgs e)
		{
			_isLoaded = true;

			UpdateButtonMetrics ();

			if (History == null)
			{
				NotificationHistory history =
					DataContext as NotificationHistory;

				if (history != null)
				{
					History = history;
				}
				else
				{
					Tile tile = DataContext as Tile;

					if (tile != null)
						History = tile.History;
				}
			}

			if (History != null)
			{
				History.CollectionChanged -= OnHistoryCollectionChanged;
				History.CollectionChanged += OnHistoryCollectionChanged;
			}
			_timer.Tick -= Timer_Tick;
			_timer.Tick += Timer_Tick;
			_sizeChangedTimer.Tick -= SizeChangedTimer_Tick;
			_sizeChangedTimer.Tick += SizeChangedTimer_Tick;
			_timer.Start ();
			RebuildChildren ();
		}

		private void SizeChangedTimer_Tick (object sender, EventArgs e)
		{
			_sizeChangedTimer.Stop ();
			RecomputeOffsets ();
			ApplyLayout ();
		}

		private void UserControl_Unloaded (object sender, RoutedEventArgs e)
		{
			_isLoaded = false;
			_timer.Tick -= Timer_Tick;
			_timer.Stop ();
			_sizeChangedTimer.Tick -= SizeChangedTimer_Tick;
			_sizeChangedTimer.Stop ();
			if (History != null)
				History.CollectionChanged -= OnHistoryCollectionChanged;

			ClearChildren ();
		}

		private void Timer_Tick (object sender, EventArgs e)
		{
			RecomputeOffsets ();
			ApplyLayout ();
		}

		// ============================================================
		// 清理
		// ============================================================
		private void ClearChildren ()
		{
			for (int i = 0; i < _entries.Count; i++)
			{
				Entry entry = _entries [i];

				if (entry.Button != null)
				{
					entry.Button.Click -= OnElementClick;
					NoticePanel.Children.Remove (entry.Button);
				}
			}

			_entries.Clear ();
			_stacks.Clear ();
		}

		// ============================================================
		// 重建通知按钮
		// ============================================================
		private void RebuildChildren ()
		{
			if (!_isLoaded && !IsLoaded)
				return;

			ClearChildren ();

			if (History == null)
				return;

			Style style =
				TryFindResource ("TileNotificationButton") as Style;

			List<NotificationStoreData> snapshot =
				History.ToList ();

			// 确保通知按照时间从旧到新排列。
			snapshot.Sort (delegate (
				 NotificationStoreData a,
				 NotificationStoreData b)
			{
				if (a == null && b == null)
					return 0;

				if (a == null)
					return -1;

				if (b == null)
					return 1;

				return DateTime.Compare (
					a.TimeStampUtc,
					b.TimeStampUtc);
			});

			int start =
				Math.Max (0, snapshot.Count - MaxVisible);

			for (int i = start; i < snapshot.Count; i++)
			{
				NotificationStoreData data = snapshot [i];

				if (data == null)
					continue;

				Button button = new Button ();
				button.DataContext = data;
				button.Style = style;
				button.ToolTip = BuildToolTip (data);
				button.Click += OnElementClick;

				Entry entry = new Entry ();
				entry.Button = button;
				entry.Data = data;

				_entries.Add (entry);
				NoticePanel.Children.Add (button);
			}

			// 先执行一次布局。
			RecomputeOffsets ();
			ApplyLayout ();

			// 等待 WPF 完成测量和排列，再获取 ActualWidth/ActualHeight。
			QueueLayoutUpdate ();
		}

		private static string BuildToolTip (NotificationStoreData data)
		{
			string time =
				data.TimeStampUtc.ToLocalTime ().ToString ("g");

			if (!string.IsNullOrEmpty (data.InfoTitle))
				return time + "\n" + data.InfoTitle;

			return time + "\n" + data.Info;
		}

		// ============================================================
		// 动态获取按钮尺寸（从隐藏的 NotificationStyleButton 读取）
		// ============================================================
		private void UpdateButtonMetrics ()
		{
			double w = double.NaN;
			double h = double.NaN;

			if (NotificationStyleButton != null)
			{
				w = NotificationStyleButton.Width;
				h = NotificationStyleButton.Height;

				if (double.IsNaN (w) || w <= 0.0)
					w = NotificationStyleButton.ActualWidth;

				if (double.IsNaN (h) || h <= 0.0)
					h = NotificationStyleButton.ActualHeight;
			}

			if (double.IsNaN (w) || double.IsInfinity (w) || w <= 0.0)
				w = 19.0;

			if (double.IsNaN (h) || double.IsInfinity (h) || h <= 0.0)
				h = 16.0;

			_buttonWidth = w;
			_buttonHeight = h;
		}

		// ============================================================
		// 动态计算堆叠有效宽度
		// 等价于原始 NotificationHistoryPanel 的 15 + 4*(count-1)
		// ============================================================
		private double GetStackWidth (NotificationStack stack)
		{
			if (stack == null || stack.Entries.Count == 0)
				return 0.0;

			return (_buttonWidth - StackOffset)
				+ StackOffset * (stack.Entries.Count - 1);
		}

		// ============================================================
		// 点击通知
		// ============================================================
		private void OnElementClick (object sender, RoutedEventArgs e)
		{
			Button button = sender as Button;

			if (button == null)
				return;

			NotificationStoreData data =
				button.DataContext as NotificationStoreData;

			if (data == null)
				return;

			NotificationClicked?.Invoke (this, data);
		}

		// ============================================================
		// 时间位置计算
		// ============================================================
		private void RecomputeOffsets ()
		{
			if (_entries.Count == 0 || _panelWidth <= 0.0)
				return;

			DateTime now = DateTime.UtcNow;

			DateTime oldest =
				_entries [0].Data.TimeStampUtc;

			double minutesSinceOldest =
				(now - oldest).TotalMinutes;

			double timeScale =
				Math.Max (
					MinimumTimeSpanMinutes,
					minutesSinceOldest);

			for (int i = 0; i < _entries.Count; i++)
			{
				Entry entry = _entries [i];

				double ageMinutes =
					(now - entry.Data.TimeStampUtc).TotalMinutes;

				if (ageMinutes < 0.0)
					ageMinutes = 0.0;

				double input =
					ageMinutes / timeScale;

				double transformed = input;

				if (timeScale > MinimumTimeSpanMinutes)
					transformed = Math.Sqrt (input);

				if (transformed < 0.0)
					transformed = 0.0;

				if (transformed > 1.0)
					transformed = 1.0;

				entry.DesiredOffset =
					(1.0 - transformed) * _panelWidth;
			}

			SplitAllStacks ();
			FixStacks ();
		}

		// ============================================================
		// 每个通知独立成为一个堆叠
		// ============================================================
		private void SplitAllStacks ()
		{
			_stacks.Clear ();

			for (int i = 0; i < _entries.Count; i++)
			{
				Entry entry = _entries [i];

				NotificationStack stack =
					new NotificationStack ();

				stack.Entries.Add (entry);
				stack.Offset = entry.DesiredOffset;

				_stacks.Add (stack);
			}
		}

		// ============================================================
		// 合并相邻堆叠
		// ============================================================
		private void FixStacks ()
		{
			for (int i = 0; i < _stacks.Count; i++)
			{
				NotificationStack stack1 =
					_stacks [i];

				while (stack1.Entries.Count < MaxStackSize &&
					stack1.Offset >= 0.0 &&
					i + 1 < _stacks.Count)
				{
					NotificationStack stack2 =
						_stacks [i + 1];

					if (stack2.Offset < 0.0 ||
						stack2.Offset - stack1.Offset >
						StackMergeDistance)
					{
						break;
					}

					CombineStacks (stack1, stack2);

					if (stack2.Entries.Count == 0)
						_stacks.RemoveAt (i + 1);
				}
			}
		}

		private void CombineStacks (
			NotificationStack stack1,
			NotificationStack stack2)
		{
			while (stack1.Entries.Count < MaxStackSize &&
				stack2.Entries.Count > 0)
			{
				Entry entry = stack2.Entries [0];

				stack2.Entries.RemoveAt (0);
				stack1.Entries.Add (entry);
			}
		}

		// ============================================================
		// 应用布局（完全对齐 NotificationHistoryPanel.ArrangeCore）
		// ============================================================
		private void ApplyLayout ()
		{
			if (_panelWidth <= 0.0)
				return;

			// 从左到右修正堆叠位置。
			double val = 0.0;

			for (int i = 0; i < _stacks.Count; i++)
			{
				NotificationStack stack = _stacks [i];
				double stackWidth = GetStackWidth (stack);

				stack.Offset = Math.Max (val, stack.Offset);
				val = stack.Offset + stackWidth;
			}

			// 从右到左修正堆叠位置。
			double num = _panelWidth;

			for (int i = _stacks.Count - 1; i >= 0; i--)
			{
				NotificationStack stack = _stacks [i];
				double stackWidth = GetStackWidth (stack);

				stack.Offset = Math.Min (stack.Offset, num - stackWidth);

				if (stack.Offset < 0.0)
					stack.Offset = 0.0;

				num = stack.Offset;
			}

			// 垂直基准：等价于原始 NotificationHistoryPanel 的 15
			double stackBaseY = _buttonWidth - StackOffset;

			// 实际设置按钮位置。
			for (int i = 0; i < _stacks.Count; i++)
			{
				NotificationStack stack = _stacks [i];
				int count = stack.Entries.Count;

				for (int k = 0; k < count; k++)
				{
					Entry entry = stack.Entries [k];

					double left = stack.Offset + StackOffset * (k - 1);
					double top = stackBaseY - StackOffset * (count - 1) + StackOffset * k;

					Canvas.SetLeft (entry.Button, left);
					Canvas.SetTop (entry.Button, top);
				}
			}
		}

		// ============================================================
		// 延迟布局
		// ============================================================
		private void QueueLayoutUpdate ()
		{
			if (_layoutQueued)
				return;

			_layoutQueued = true;

			Dispatcher.BeginInvoke (
				new Action (delegate
				{
					_layoutQueued = false;

					if (!_isLoaded && !IsLoaded)
						return;

					UpdateButtonMetrics ();
					RecomputeOffsets ();
					ApplyLayout ();
				}),
				DispatcherPriority.Loaded);
		}

		// ============================================================
		// Canvas 尺寸变化
		// ============================================================
		private void Canvas_SizeChanged (
			object sender,
			SizeChangedEventArgs e)
		{
			_panelWidth = e.NewSize.Width;
			_panelHeight = e.NewSize.Height;

			UpdateButtonMetrics ();
			RecomputeOffsets ();
			ApplyLayout ();

			QueueLayoutUpdate ();
		}

		private void NotificationPanelProp_SizeChanged (object sender, SizeChangedEventArgs e)
		{
			OnSizeChanged ();
		}
		private void NotificationButtonProp_SizeChanged (object sender, SizeChangedEventArgs e)
		{
			OnSizeChanged ();
		}
		private void OnSizeChanged ()
		{
			_sizeChangedTimer.Stop ();
			_sizeChangedTimer.Start ();
		}
		private void NotificationStyleButton_SizeChanged (object sender, SizeChangedEventArgs e)
		{
			OnSizeChanged ();
		}
	}
}