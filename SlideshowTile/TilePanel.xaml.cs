using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Sidebar;

namespace WindowsModern.SlideshowTile
{
	public partial class TilePanel: UserControl, IDisposable
	{
		private Image [] imgEleArr;
		private short serial = 0;
		private DispatcherTimer _timer;
		private bool _isAnimating = false;
		private int _currentImageIndex = -1;
		private string _currentImagePath = null;
		private Random _random = new Random ();

		// 预加载缓存
		private BitmapImage _preloadedBitmap;
		private int _preloadedImageIndex = -1;
		private object _preloadLock = new object ();
		private Thread _preloadThread;
		private volatile bool _isPreloading = false;
		private volatile bool _stopPreload = false;

		// 高度动画
		private DoubleAnimation _heightAnimation;

		// 集合变化防抖
		private Timer _collectionChangeDebounceTimer = null;
		private readonly object _debounceTimerLock = new object ();
		private volatile bool _isDisposed = false;
		private const int COLLECTION_CHANGE_DEBOUNCE_MS = 500;

		// ✅ 定期GC相关字段
		private int _gcTotalCount = 0;      // GC总计次数
		private int _gcCurrentCount = 0;    // GC当前计次
		private readonly object _gcCountLock = new object ();

		public TilePanel ()
		{
			InitializeComponent ();
			imgEleArr = new [] { Image1, Image2 };
		}

		private void UserControl_Loaded (object sender, RoutedEventArgs e)
		{
			var descriptor = DependencyPropertyDescriptor.FromProperty (UIElement.OpacityProperty, typeof (UIElement));
			descriptor.AddValueChanged (Image1, OnOpacityChanged);
			descriptor.AddValueChanged (Image2, OnOpacityChanged);

			Tile.TileOptions.PropertyChanged += TileOptions_PropertyChanged;
			Tile.ImageFiles.CollectionChanged += ImageFiles_CollectionChanged;

			Image1.Opacity = 1;
			Image2.Opacity = 0;
			serial = 0;

			// 尝试恢复上次显示的图片
			RestoreLastPicture ();

			StartTimer ();
			StartPreloadThread ();
		}

		private void RestoreLastPicture ()
		{
			string lastRecord = Tile.TileOptions.CurrentPictureRecord;
			if (!string.IsNullOrEmpty (lastRecord))
			{
				// 规范化路径：去除首尾空白，用于比较
				string normalizedPath = lastRecord.Trim ();
				int idx = FindImageIndexByPath (normalizedPath);
				if (idx >= 0)
				{
					_currentImageIndex = idx;
					_currentImagePath = Tile.ImageFiles [idx];
					if (TryLoadImageAtIndex (idx, true))
					{
						// 立即预加载下一张
						SchedulePreload ();
						return;
					}
				}
			}
			// 无法恢复，加载第一张
			LoadNextImage (true);
		}

		/// <summary>
		/// 根据路径查找图片索引（忽略大小写和首尾空白）
		/// </summary>
		private int FindImageIndexByPath (string normalizedPath)
		{
			if (string.IsNullOrEmpty (normalizedPath) || Tile.ImageFiles.Count == 0)
				return -1;

			for (int i = 0; i < Tile.ImageFiles.Count; i++)
			{
				string itemPath = Tile.ImageFiles [i]?.Trim () ?? "";
				if (string.Equals (itemPath, normalizedPath, StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}
			return -1;
		}

		private void UserControl_Unloaded (object sender, RoutedEventArgs e)
		{
			var descriptor = DependencyPropertyDescriptor.FromProperty (UIElement.OpacityProperty, typeof (UIElement));
			descriptor.RemoveValueChanged (Image1, OnOpacityChanged);
			descriptor.RemoveValueChanged (Image2, OnOpacityChanged);

			StopTimer ();
			StopPreloadThread ();
		}

		private void OnOpacityChanged (object sender, EventArgs e)
		{
			var img = sender as Image;
			if (img == null) return;
			var newv = img.Opacity <= 0 ? Visibility.Collapsed : Visibility.Visible;
			if (img.Visibility != newv) img.Visibility = newv;
		}

		private void TileOptions_PropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof (Tile.TileOptions.CycleDelaySecond))
			{
				RestartTimer ();
			}
			else if (e.PropertyName == nameof (Tile.TileOptions.RandomPlay))
			{
				// 随机模式切换时，清空预加载
				ClearPreload ();
			}
		}

		private void ImageFiles_CollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_isDisposed) return;

			// 防抖处理：500ms 内的多次变化只执行一次
			lock (_debounceTimerLock)
			{
				if (_collectionChangeDebounceTimer != null)
				{
					_collectionChangeDebounceTimer.Dispose ();
					_collectionChangeDebounceTimer = null;
				}

				_collectionChangeDebounceTimer = new Timer (
					_ => HandleImageFilesCollectionChanged (),
					null,
					COLLECTION_CHANGE_DEBOUNCE_MS,
					Timeout.Infinite
				);
			}
		}

		/// <summary>
		/// 处理图片列表变化的防抖回调
		/// </summary>
		private void HandleImageFilesCollectionChanged ()
		{
			if (_isDisposed) return;

			// 清空预加载
			ClearPreload ();

			// 更新索引：检查当前路径是否仍存在于列表
			if (!string.IsNullOrEmpty (_currentImagePath))
			{
				string normalizedPath = _currentImagePath.Trim ();
				int newIndex = FindImageIndexByPath (normalizedPath);

				if (newIndex >= 0)
				{
					// 路径仍存在，更新索引
					_currentImageIndex = newIndex;
				}
				else
				{
					// 路径不存在，重置索引和路径
					_currentImageIndex = -1;
					_currentImagePath = null;
				}
			}
			else
			{
				// 如果之前没有记录路径，也重置索引
				_currentImageIndex = -1;
			}

			// 尝试恢复上次记录（若仍存在），或加载新的
			Dispatcher.Invoke (new Action (() => {
				if (!_isDisposed)
				{
					RestoreLastPicture ();
				}
			}), DispatcherPriority.Normal);
		}

		/// <summary>
		/// ✅ 根据计时器间隔计算GC总计次数
		/// </summary>
		private int CalculateGcTotalCount (double cycleDelaySecond)
		{
			const double GC_INTERVAL_SECONDS = 30.0;

			if (cycleDelaySecond >= GC_INTERVAL_SECONDS)
			{
				// 大于等于30秒时，返回1（每30秒触发一次GC）
				return 1;
			}
			else if (cycleDelaySecond > 0)
			{
				// 小于30秒，使用退一法计算：30 * 1000 / (cycleDelaySecond * 1000)
				// 简化为：30 / cycleDelaySecond，向下取整
				int total = (int)(GC_INTERVAL_SECONDS / cycleDelaySecond);
				return Math.Max (1, total);
			}
			else
			{
				return 1;
			}
		}

		private void StartTimer ()
		{
			if (_timer != null) return;
			_timer = new DispatcherTimer ();
			_timer.Interval = TimeSpan.FromSeconds (Tile.TileOptions.CycleDelaySecond);
			_timer.Tick += OnTimerTick;
			_timer.Start ();

			// ✅ 初始化GC计数
			_gcTotalCount = CalculateGcTotalCount (Tile.TileOptions.CycleDelaySecond);
			_gcCurrentCount = 0;

			System.Diagnostics.Debug.WriteLine (
				$"Timer started: Interval={Tile.TileOptions.CycleDelaySecond}s, GC Total Count={_gcTotalCount}");
		}

		private void StopTimer ()
		{
			if (_timer != null)
			{
				_timer.Stop ();
				_timer.Tick -= OnTimerTick;
				_timer = null;
			}
		}

		private void RestartTimer ()
		{
			if (_timer != null)
			{
				_timer.Interval = TimeSpan.FromSeconds (Tile.TileOptions.CycleDelaySecond);

				// ✅ 重新计算GC总计次数
				_gcTotalCount = CalculateGcTotalCount (Tile.TileOptions.CycleDelaySecond);
				_gcCurrentCount = 0;

				System.Diagnostics.Debug.WriteLine (
					$"Timer restarted: Interval={Tile.TileOptions.CycleDelaySecond}s, GC Total Count={_gcTotalCount}");
			}
		}

		private void OnTimerTick (object sender, EventArgs e)
		{
			if (_isAnimating) return;
			LoadNextImage (false);
			lock (_gcCountLock)
			{
				if ((_gcCurrentCount++) % _gcTotalCount == 0)
				{
					// System.Diagnostics.Debug.WriteLine ($"Triggering GC: Current Count={_gcCurrentCount}, Total Count={_gcTotalCount}");

					Utilities.ReleaseLargeResourcesAsync ();
				}
			}
		}

		/// <summary>
		/// 预加载线程入口
		/// </summary>
		private void StartPreloadThread ()
		{
			_stopPreload = false;
			_preloadThread = new Thread (PreloadThreadProc) {
				IsBackground = true,
				Name = "ImagePreloadThread"
			};
			_preloadThread.Start ();
		}

		private void StopPreloadThread ()
		{
			_stopPreload = true;
			if (_preloadThread != null)
			{
				_preloadThread.Join (2000);
				_preloadThread = null;
			}
			ClearPreload ();
		}

		/// <summary>
		/// 预加载线程循环
		/// </summary>
		private void PreloadThreadProc ()
		{
			while (!_stopPreload)
			{
				Thread.Sleep (100);
				if (_stopPreload) break;

				// 定期尝试预加载
				lock (_preloadLock)
				{
					if (_isPreloading || _preloadedBitmap != null)
						continue;

					if (Tile.ImageFiles.Count <= 1)
						continue;

					int nextIdx = CalculateNextImageIndex ();
					if (nextIdx == _preloadedImageIndex && _preloadedBitmap != null)
						continue;

					_isPreloading = true;
				}

				// 在锁外执行加载
				try
				{
					int nextIdx = CalculateNextImageIndex ();
					BitmapImage bitmap = LoadBitmapImage (nextIdx);
					if (bitmap != null)
					{
						lock (_preloadLock)
						{
							// ✅ 清理旧的预加载缓存
							if (_preloadedBitmap != null)
							{
								_preloadedBitmap = null;
								Utilities.ReleaseLargeResourcesAsync ();
							}
							_preloadedBitmap = bitmap;
							_preloadedImageIndex = nextIdx;
						}
					}
				}
				catch
				{
					// 预加载错误忽略，不影响主流程
				}
				finally
				{
					lock (_preloadLock)
					{
						_isPreloading = false;
					}
				}
			}
		}

		/// <summary>
		/// 计算下一张图片的索引
		/// </summary>
		private int CalculateNextImageIndex ()
		{
			if (_currentImageIndex < 0)
				return 0;

			if (Tile.TileOptions.RandomPlay)
			{
				if (Tile.ImageFiles.Count == 1)
					return _currentImageIndex;
				int nextIdx;
				do
				{
					nextIdx = _random.Next (Tile.ImageFiles.Count);
				} while (nextIdx == _currentImageIndex);
				return nextIdx;
			}
			else
			{
				return (_currentImageIndex + 1) % Tile.ImageFiles.Count;
			}
		}

		/// <summary>
		/// 触发预加载任务
		/// </summary>
		private void SchedulePreload ()
		{
			// 预加载线程会自动处理
		}

		/// <summary>
		/// 清空预加载缓存
		/// </summary>
		private void ClearPreload ()
		{
			lock (_preloadLock)
			{
				_preloadedBitmap = null;
				_preloadedImageIndex = -1;
			}
		}

		/// <summary>
		/// 在后台线程加载图片
		/// </summary>
		private BitmapImage LoadBitmapImage (int idx)
		{
			if (idx < 0 || idx >= Tile.ImageFiles.Count)
				return null;

			string file = Tile.ImageFiles [idx];
			if (string.IsNullOrEmpty (file))
				return null;

			try
			{
				var bitmap = new BitmapImage ();
				bitmap.BeginInit ();
				bitmap.UriSource = new Uri (file, UriKind.Absolute);
				bitmap.CacheOption = BitmapCacheOption.None;
				bitmap.EndInit ();
				var sidebarWidth = Tile.TileInstance.Features.Config.Width;
				if (bitmap.Width > sidebarWidth) bitmap.DecodePixelWidth = (int)(sidebarWidth * UITheme.DPIDouble);
				bitmap.Freeze ();
				return bitmap;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine ($"Preload image error: {ex.Message}");
				return null;
			}
		}

		private void LoadNextImage (bool forceRefresh)
		{
			if (_isAnimating) return;
			if (Tile.ImageFiles.Count == 0) return;

			int attempts = 0;
			int maxAttempts = Tile.ImageFiles.Count;

			int nextIndex = _currentImageIndex;
			do
			{
				if (forceRefresh || _currentImageIndex < 0)
				{
					nextIndex = 0;
				}
				else
				{
					if (Tile.TileOptions.RandomPlay)
					{
						// 随机选择，排除当前图片
						if (Tile.ImageFiles.Count == 1)
							nextIndex = _currentImageIndex;
						else
						{
							do
							{
								nextIndex = _random.Next (Tile.ImageFiles.Count);
							} while (nextIndex == _currentImageIndex);
						}
					}
					else
					{
						nextIndex = (_currentImageIndex + 1) % Tile.ImageFiles.Count;
					}
				}

				// 当只有一张图片且非强制刷新时，仍允许加载（可能重复，但会尝试）
				if (nextIndex == _currentImageIndex && Tile.ImageFiles.Count > 1 && !forceRefresh)
					nextIndex = (nextIndex + 1) % Tile.ImageFiles.Count;

				// 优先使用预加载的图片
				BitmapImage bitmap = GetPreloadedBitmapIfAvailable (nextIndex);
				if (bitmap != null)
				{
					ApplyBitmapAndSwitch (bitmap, nextIndex);
					_currentImageIndex = nextIndex;
					_currentImagePath = Tile.ImageFiles [nextIndex];
					Tile.TileOptions.CurrentPictureRecord = _currentImagePath;
					SchedulePreload ();
					return;
				}

				// 预加载不可用，直接加载
				if (TryLoadImageAtIndex (nextIndex, false))
				{
					_currentImageIndex = nextIndex;
					_currentImagePath = Tile.ImageFiles [nextIndex];
					Tile.TileOptions.CurrentPictureRecord = _currentImagePath;
					SchedulePreload ();
					return;
				}

				// 加载失败，移除无效文件
				string invalidFile = Tile.ImageFiles [nextIndex];
				System.Diagnostics.Debug.WriteLine ($"Failed to load image: {invalidFile}");
				Application.Current.Dispatcher.Invoke (new Action (() => Tile.ImageFiles.Remove (invalidFile)));

				attempts++;
				if (attempts >= maxAttempts) break;
			} while (Tile.ImageFiles.Count > 0);
		}

		/// <summary>
		/// 获取预加载的图片（如果索引匹配）
		/// </summary>
		private BitmapImage GetPreloadedBitmapIfAvailable (int idx)
		{
			lock (_preloadLock)
			{
				if (_preloadedBitmap != null && _preloadedImageIndex == idx)
				{
					BitmapImage result = _preloadedBitmap;
					_preloadedBitmap = null;
					_preloadedImageIndex = -1;
					return result;
				}
			}
			return null;
		}

		/// <summary>
		/// 应用预加载的图片并执行切换动画
		/// </summary>
		private void ApplyBitmapAndSwitch (BitmapImage bitmap, int idx)
		{
			int targetSerial = (serial == 0) ? 1 : 0;
			Image targetImg = imgEleArr [targetSerial];
			targetImg.Source = bitmap;

			// 根据图片尺寸调整容器高度
			AdjustContainerHeightByBitmap (bitmap);

			SwitchTo (targetSerial);
		}

		private bool TryLoadImageAtIndex (int idx, bool forceSwitch)
		{
			if (idx < 0 || idx >= Tile.ImageFiles.Count) return false;
			string file = Tile.ImageFiles [idx];
			if (string.IsNullOrEmpty (file)) return false;

			try
			{
				var bitmap = LoadBitmapImage (idx);
				if (bitmap == null) return false;

				int targetSerial = (serial == 0) ? 1 : 0;
				Image targetImg = imgEleArr [targetSerial];
				targetImg.Source = null;
				targetImg.Source = bitmap;

				// 根据图片尺寸调整容器高度
				AdjustContainerHeightByBitmap (bitmap);

				SwitchTo (targetSerial);
				return true;
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine ($"Load image error: {ex.Message}");
				return false;
			}
			finally
			{
				UpdateLayout ();
			}
		}

		/// <summary>
		/// 根据图片尺寸动态调整容器高度（带动画过渡）
		/// </summary>
		private void AdjustContainerHeightByBitmap (BitmapImage bitmap)
		{
			if (bitmap == null) return;

			try
			{
				// 获取父容器宽度
				FrameworkElement parent = this.Parent as FrameworkElement;
				if (parent == null) return;

				double containerWidth = parent.ActualWidth;
				if (containerWidth <= 0) return;

				// 计算图片宽高比
				double imageAspectRatio = bitmap.PixelWidth / (double)bitmap.PixelHeight;

				// 计算所需高度：高度 = 宽度 / 宽高比
				double calculatedHeight = containerWidth / imageAspectRatio;

				// 停止现有的高度动画
				if (_heightAnimation != null)
				{
					this.BeginAnimation (HeightProperty, null);
					_heightAnimation = null;
				}

				// 如果当前高度为0或未设置，直接应用，无需动画
				if (this.Height <= 0 || double.IsNaN (this.Height))
				{
					this.Height = calculatedHeight;
				}
				else
				{
					// 创建高度变化动画
					_heightAnimation = new DoubleAnimation (
						this.Height,
						calculatedHeight,
						new Duration (TimeSpan.FromSeconds (0.4))) {
						EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
					};

					this.BeginAnimation (HeightProperty, _heightAnimation);
				}

				System.Diagnostics.Debug.WriteLine (
					$"Image: {bitmap.PixelWidth}x{bitmap.PixelHeight}, " +
					$"AspectRatio: {imageAspectRatio:F2}, " +
					$"ContainerWidth: {containerWidth}, " +
					$"CalculatedHeight: {calculatedHeight:F1}");
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine ($"Adjust height error: {ex.Message}");
			}
		}

		private void SwitchTo (int newSerial)
		{
			if (_isAnimating) return;
			_isAnimating = true;
			UpdateLayout ();
			Image oldImg = imgEleArr [serial];
			Image newImg = imgEleArr [newSerial];

			// 新图片初始状态：透明
			newImg.Opacity = 0;

			// 旧图片淡出
			var fadeOut = new DoubleAnimation (1, 0, new Duration (TimeSpan.FromSeconds (0.4)));
			fadeOut.Completed += (s, e) => {
				oldImg.Opacity = 0;
				// ✅ 关键：清理旧图片的Source，释放内存
				oldImg.Source = null;
			};

			// 新图片淡入
			var fadeIn = new DoubleAnimation (0, 1, new Duration (TimeSpan.FromSeconds (0.4))) {
				EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
			};

			fadeIn.Completed += (s, e) => {
				_isAnimating = false;
				serial = (short)newSerial;
			};

			oldImg.BeginAnimation (Image.OpacityProperty, fadeOut);
			newImg.BeginAnimation (Image.OpacityProperty, fadeIn);
		}

		public void Dispose ()
		{
			_isDisposed = true;

			try
			{
				// 停止计时器
				StopTimer ();
				StopPreloadThread ();

				// ✅ 清理两个Image的Source
				if (Image1 != null) Image1.Source = null;
				if (Image2 != null) Image2.Source = null;

				// 停止防抖计时器
				lock (_debounceTimerLock)
				{
					if (_collectionChangeDebounceTimer != null)
					{
						_collectionChangeDebounceTimer.Dispose ();
						_collectionChangeDebounceTimer = null;
					}
				}

				// 移除事件监听
				if (Tile.TileOptions != null)
					Tile.TileOptions.PropertyChanged -= TileOptions_PropertyChanged;
				if (Tile.ImageFiles != null)
					Tile.ImageFiles.CollectionChanged -= ImageFiles_CollectionChanged;

				// ✅ 清理预加载缓存
				ClearPreload ();

				// ✅ 移除Opacity属性变化监听
				var descriptor = DependencyPropertyDescriptor.FromProperty (UIElement.OpacityProperty, typeof (UIElement));
				if (descriptor != null)
				{
					descriptor.RemoveValueChanged (Image1, OnOpacityChanged);
					descriptor.RemoveValueChanged (Image2, OnOpacityChanged);
				}

				// ✅ 触发垃圾回收
				Utilities.ReleaseLargeResourcesAsync ();
			}
			catch { }
		}
		FlyoutPanel flyoutPanel = null;
		public void OnFlyoutInit (FlyoutAboutEventArgs e)
		{
			if (flyoutPanel == null) flyoutPanel = new FlyoutPanel ();
			(flyoutPanel?.Parent as Panel)?.Children?.Clear ();
			e.ClientArea.Children.Add (flyoutPanel);
			flyoutPanel.SetFile (_currentImagePath);
		}
		public void OnFlyoutClosed ()
		{
			(flyoutPanel?.Parent as Panel)?.Children?.Clear ();
		}
	}
}