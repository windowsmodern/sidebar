using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace WindowsModern.SnowFall
{
	/// <summary>
	/// 在承载区域内绘制飘落雪花的 FrameworkElement。
	/// 放在 Grid / Panel 中会自动填满。
	/// 使用 DispatcherTimer(33ms ≈ 30FPS) + OnRender 直绘，避免 CompositionTarget 全局钩子拖累多实例。
	/// 支持可选的 SnowflakeStrokeColor：设置后雪花画成"白心 + 深色边"，在浅色背景上也能看清。
	/// </summary>
	public sealed class SnowfallOverlay: FrameworkElement
	{
		#region 依赖属性
		public static readonly DependencyProperty SnowflakeCountProperty =
			DependencyProperty.Register (nameof (SnowflakeCount), typeof (int), typeof (SnowfallOverlay),
				new FrameworkPropertyMetadata (60, FrameworkPropertyMetadataOptions.AffectsRender, OnStructureChanged));

		public static readonly DependencyProperty SnowflakeSizeProperty =
			DependencyProperty.Register (nameof (SnowflakeSize), typeof (double), typeof (SnowfallOverlay),
				new FrameworkPropertyMetadata (2.0, FrameworkPropertyMetadataOptions.AffectsRender, OnStructureChanged));

		public static readonly DependencyProperty SpeedFactorProperty =
			DependencyProperty.Register (nameof (SpeedFactor), typeof (double), typeof (SnowfallOverlay),
				new FrameworkPropertyMetadata (1.0));

		public static readonly DependencyProperty WindProperty =
			DependencyProperty.Register (nameof (Wind), typeof (double), typeof (SnowfallOverlay),
				new FrameworkPropertyMetadata (0.1));

		public static readonly DependencyProperty SwayFactorProperty =
			DependencyProperty.Register (nameof (SwayFactor), typeof (double), typeof (SnowfallOverlay),
				new FrameworkPropertyMetadata (1.0));

		public static readonly DependencyProperty BaseOpacityProperty =
			DependencyProperty.Register (nameof (BaseOpacity), typeof (double), typeof (SnowfallOverlay),
				new FrameworkPropertyMetadata (0.85, FrameworkPropertyMetadataOptions.AffectsRender, OnStructureChanged));

		public static readonly DependencyProperty SnowColorProperty =
			DependencyProperty.Register (nameof (SnowColor), typeof (Color), typeof (SnowfallOverlay),
				new FrameworkPropertyMetadata (Color.FromRgb (0xF0, 0xF6, 0xFF),
					FrameworkPropertyMetadataOptions.AffectsRender, OnColorChanged));

		/// <summary>
		/// 可选：雪花描边颜色。设置后每片雪花先画一个略大的描边圆，再画白心。
		/// 浅色背景上白色雪花不显眼，描边能让它清晰可见。
		/// </summary>
		public static readonly DependencyProperty SnowflakeStrokeColorProperty =
			DependencyProperty.Register (nameof (SnowflakeStrokeColor), typeof (Color?), typeof (SnowfallOverlay),
				new FrameworkPropertyMetadata (null, FrameworkPropertyMetadataOptions.AffectsRender, OnColorChanged));

		public int SnowflakeCount { get { return (int)GetValue (SnowflakeCountProperty); } set { SetValue (SnowflakeCountProperty, value); } }
		public double SnowflakeSize { get { return (double)GetValue (SnowflakeSizeProperty); } set { SetValue (SnowflakeSizeProperty, value); } }
		public double SpeedFactor { get { return (double)GetValue (SpeedFactorProperty); } set { SetValue (SpeedFactorProperty, value); } }
		public double Wind { get { return (double)GetValue (WindProperty); } set { SetValue (WindProperty, value); } }
		public double SwayFactor { get { return (double)GetValue (SwayFactorProperty); } set { SetValue (SwayFactorProperty, value); } }
		public double BaseOpacity { get { return (double)GetValue (BaseOpacityProperty); } set { SetValue (BaseOpacityProperty, value); } }
		public Color SnowColor { get { return (Color)GetValue (SnowColorProperty); } set { SetValue (SnowColorProperty, value); } }
		public Color? SnowflakeStrokeColor { get { return (Color?)GetValue (SnowflakeStrokeColorProperty); } set { SetValue (SnowflakeStrokeColorProperty, value); } }

		private static void OnStructureChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((SnowfallOverlay)d)._structureDirty = true;
		}
		private static void OnColorChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			((SnowfallOverlay)d)._brushCache.Clear ();
		}
		#endregion

		#region 内部状态
		private readonly List<Snowflake> _flakes = new List<Snowflake> ();
		private readonly Random _rng = new Random ();
		private readonly DispatcherTimer _timer;
		private readonly Dictionary<byte, SolidColorBrush> _brushCache = new Dictionary<byte, SolidColorBrush> ();
		private DateTime _lastTick;
		private bool _isRunning;
		private bool _structureDirty = true;
		private bool _resizePending = true;
		#endregion

		public SnowfallOverlay ()
		{
			IsHitTestVisible = false;
			ClipToBounds = true;
			SnapsToDevicePixels = true;
			_timer = new DispatcherTimer (DispatcherPriority.Render, Dispatcher) {
				Interval = TimeSpan.FromMilliseconds (33)
			};
			_timer.Tick += OnTick;
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
			IsVisibleChanged += OnIsVisibleChanged;
			SizeChanged += OnSizeChanged;
		}

		#region 生命周期
		private void OnLoaded (object sender, RoutedEventArgs e) { TryStart (); }
		private void OnUnloaded (object sender, RoutedEventArgs e) { Stop (); }
		private void OnIsVisibleChanged (object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((bool)e.NewValue) TryStart (); else Stop ();
		}
		private void OnSizeChanged (object sender, SizeChangedEventArgs e) { _resizePending = true; }

		private void TryStart ()
		{
			if (_isRunning || !IsLoaded) return;
			if (ActualWidth <= 0 || ActualHeight <= 0) return;
			_isRunning = true;
			_structureDirty = true;
			_resizePending = true;
			_lastTick = DateTime.UtcNow;
			_timer.Start ();
		}
		private void Stop ()
		{
			if (!_isRunning) return;
			_isRunning = false;
			_timer.Stop ();
		}
		#endregion

		#region 帧循环
		private void OnTick (object sender, EventArgs e)
		{
			if (!IsLoaded || !IsVisible || ActualWidth <= 0 || ActualHeight <= 0)
			{
				Stop ();
				return;
			}
			var now = DateTime.UtcNow;
			double dt = (now - _lastTick).TotalSeconds;
			_lastTick = now;
			if (dt <= 0 || dt > 0.5) dt = 0.033;
			Update (dt);
			InvalidateVisual ();
		}

		private void Update (double dt)
		{
			double w = ActualWidth, h = ActualHeight;
			if (w <= 0 || h <= 0) return;

			if (_structureDirty || _resizePending)
			{
				_structureDirty = false;
				_resizePending = false;
				RebuildFlakes (w, h);
				return;
			}

			double wind = Wind;
			for (int i = 0; i < _flakes.Count; i++)
			{
				var f = _flakes [i];
				f.Phase += f.PhaseSpeed * dt;
				f.Y += f.Speed * dt;
				f.X += (Math.Sin (f.Phase) * f.Sway + wind * f.Speed) * dt;

				if (f.Y - f.Size > h) ResetFlake (f, fromTop: true, w: w, h: h);
				else if (f.X - f.Size > w) f.X = -f.Size;
				else if (f.X + f.Size < 0) f.X = w + f.Size;
			}
		}
		#endregion

		#region 雪花池
		private void RebuildFlakes (double w, double h)
		{
			int count = Math.Max (0, SnowflakeCount);
			while (_flakes.Count < count) _flakes.Add (new Snowflake ());
			while (_flakes.Count > count) _flakes.RemoveAt (_flakes.Count - 1);

			for (int i = 0; i < _flakes.Count; i++)
			{
				var f = _flakes [i];
				ResetFlake (f, fromTop: false, w: w, h: h);
				f.Y = _rng.NextDouble () * h;
			}
		}

		private void ResetFlake (Snowflake f, bool fromTop, double w, double h)
		{
			double baseSize = Math.Max (1.0, SnowflakeSize);
			double size = baseSize * (0.5 + _rng.NextDouble () * 1.1);
			f.Size = size;
			f.Speed = (22.0 + size * 9.0) * Math.Max (0.1, SpeedFactor);
			f.Sway = size * 1.6 * Math.Max (0.0, SwayFactor);
			f.Phase = _rng.NextDouble () * Math.PI * 2.0;
			f.PhaseSpeed = 0.6 + _rng.NextDouble () * 1.4;
			f.Opacity = BaseOpacity * (0.55 + _rng.NextDouble () * 0.45);
			f.X = _rng.NextDouble () * w;
			f.Y = fromTop ? (-size - _rng.NextDouble () * 40.0) : (_rng.NextDouble () * h);
		}
		#endregion

		#region 渲染
		protected override void OnRender (DrawingContext dc)
		{
			base.OnRender (dc);
			if (_flakes.Count == 0) return;
			var color = SnowColor;
			var stroke = SnowflakeStrokeColor;
			bool hasStroke = stroke.HasValue;
			for (int i = 0; i < _flakes.Count; i++)
			{
				var f = _flakes [i];
				double r = f.Size * 0.5;
				if (hasStroke)
				{
					// 深色描边：稍大一圈，模拟"白心 + 深边"
					double sr = r + 0.8;
					dc.DrawEllipse (GetBrush (f.Opacity * 0.55, stroke.Value), null, new Point (f.X, f.Y), sr, sr);
				}
				dc.DrawEllipse (GetBrush (f.Opacity, color), null, new Point (f.X, f.Y), r, r);
			}
		}

		private SolidColorBrush GetBrush (double opacity, Color baseColor)
		{
			int ai = (int)Math.Round (Math.Max (0.0, Math.Min (1.0, opacity)) * 255.0);
			byte a = (byte)((ai / 16) * 16);
			SolidColorBrush b;
			if (_brushCache.TryGetValue (a, out b)) return b;
			b = new SolidColorBrush (Color.FromArgb (a, baseColor.R, baseColor.G, baseColor.B));
			b.Freeze ();
			_brushCache [a] = b;
			return b;
		}
		#endregion
	}
}