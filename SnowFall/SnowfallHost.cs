using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WindowsModern.SnowFall
{
	/// <summary>
	/// 在任意内容之上叠加雪花的 Grid 装饰器。
	/// 雪花层固定为第一个子元素且 ZIndex = 1000；后续加入的子元素自动排在雪花下方。
	/// </summary>
	public sealed class SnowfallHost: Grid
	{
		private readonly SnowfallOverlay _overlay;

		public SnowfallHost ()
		{
			_overlay = new SnowfallOverlay { IsHitTestVisible = false };
			Children.Add (_overlay);
			Panel.SetZIndex (_overlay, 1000);
		}

		public static readonly DependencyProperty SnowflakeCountProperty =
			DependencyProperty.Register (nameof (SnowflakeCount), typeof (int), typeof (SnowfallHost),
				new PropertyMetadata (60, (d, e) => ((SnowfallHost)d)._overlay.SnowflakeCount = (int)e.NewValue));
		public static readonly DependencyProperty SnowflakeSizeProperty =
			DependencyProperty.Register (nameof (SnowflakeSize), typeof (double), typeof (SnowfallHost),
				new PropertyMetadata (2.0, (d, e) => ((SnowfallHost)d)._overlay.SnowflakeSize = (double)e.NewValue));
		public static readonly DependencyProperty SpeedFactorProperty =
			DependencyProperty.Register (nameof (SpeedFactor), typeof (double), typeof (SnowfallHost),
				new PropertyMetadata (1.0, (d, e) => ((SnowfallHost)d)._overlay.SpeedFactor = (double)e.NewValue));
		public static readonly DependencyProperty WindProperty =
			DependencyProperty.Register (nameof (Wind), typeof (double), typeof (SnowfallHost),
				new PropertyMetadata (0.1, (d, e) => ((SnowfallHost)d)._overlay.Wind = (double)e.NewValue));
		public static readonly DependencyProperty SwayFactorProperty =
			DependencyProperty.Register (nameof (SwayFactor), typeof (double), typeof (SnowfallHost),
				new PropertyMetadata (1.0, (d, e) => ((SnowfallHost)d)._overlay.SwayFactor = (double)e.NewValue));
		public static readonly DependencyProperty BaseOpacityProperty =
			DependencyProperty.Register (nameof (BaseOpacity), typeof (double), typeof (SnowfallHost),
				new PropertyMetadata (0.85, (d, e) => ((SnowfallHost)d)._overlay.BaseOpacity = (double)e.NewValue));
		public static readonly DependencyProperty SnowColorProperty =
			DependencyProperty.Register (nameof (SnowColor), typeof (Color), typeof (SnowfallHost),
				new PropertyMetadata (Color.FromRgb (0xF0, 0xF6, 0xFF), (d, e) => ((SnowfallHost)d)._overlay.SnowColor = (Color)e.NewValue));
		public static readonly DependencyProperty SnowflakeStrokeColorProperty =
			DependencyProperty.Register (nameof (SnowflakeStrokeColor), typeof (Color?), typeof (SnowfallHost),
				new PropertyMetadata (null, (d, e) => ((SnowfallHost)d)._overlay.SnowflakeStrokeColor = (Color?)e.NewValue));

		public int SnowflakeCount { get { return (int)GetValue (SnowflakeCountProperty); } set { SetValue (SnowflakeCountProperty, value); } }
		public double SnowflakeSize { get { return (double)GetValue (SnowflakeSizeProperty); } set { SetValue (SnowflakeSizeProperty, value); } }
		public double SpeedFactor { get { return (double)GetValue (SpeedFactorProperty); } set { SetValue (SpeedFactorProperty, value); } }
		public double Wind { get { return (double)GetValue (WindProperty); } set { SetValue (WindProperty, value); } }
		public double SwayFactor { get { return (double)GetValue (SwayFactorProperty); } set { SetValue (SwayFactorProperty, value); } }
		public double BaseOpacity { get { return (double)GetValue (BaseOpacityProperty); } set { SetValue (BaseOpacityProperty, value); } }
		public Color SnowColor { get { return (Color)GetValue (SnowColorProperty); } set { SetValue (SnowColorProperty, value); } }
		public Color? SnowflakeStrokeColor { get { return (Color?)GetValue (SnowflakeStrokeColorProperty); } set { SetValue (SnowflakeStrokeColorProperty, value); } }
	}
}