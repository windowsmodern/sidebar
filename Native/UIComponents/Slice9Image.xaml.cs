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

namespace Sidebar
{
	/// <summary>
	/// Slice9Image.xaml 的交互逻辑
	/// </summary>
	public partial class Slice9Image: UserControl
	{
		public Slice9Image ()
		{
			InitializeComponent ();
		}
		/// <summary>
		/// Image Source
		/// </summary>
		public static readonly DependencyProperty SourceProperty =
			DependencyProperty.Register ("Source", typeof (ImageSource), typeof (Slice9Image),
				new FrameworkPropertyMetadata (null, OnSourceChanged));
		/// <summary>
		/// Slice (Left, Top, Right, Bottom)
		/// </summary>
		public static readonly DependencyProperty SliceThicknessProperty =
		   DependencyProperty.Register ("SliceThickness", typeof (Thickness), typeof (Slice9Image),
			   new FrameworkPropertyMetadata (new Thickness (0), OnSliceThicknessChanged));
		/// <summary>
		/// Image Display Quality
		/// </summary>
		public static readonly DependencyProperty ScalingModeProperty =
		   DependencyProperty.Register ("ScalingMode", typeof (BitmapScalingMode), typeof (Slice9Image),
			   new FrameworkPropertyMetadata (BitmapScalingMode.HighQuality, OnScalingModeChanged));
		/// <summary>
		/// Image Source
		/// </summary>
		public ImageSource Source
		{
			get { return (ImageSource)GetValue (SourceProperty); }
			set { SetValue (SourceProperty, value); }
		}
		/// <summary>
		/// Slice (Left, Top, Right, Bottom)
		/// </summary>
		public Thickness SliceThickness
		{
			get { return (Thickness)GetValue (SliceThicknessProperty); }
			set { SetValue (SliceThicknessProperty, value); }
		}
		/// <summary>
		/// Image Display Quality
		/// </summary>
		public BitmapScalingMode ScalingMode
		{
			get { return (BitmapScalingMode)GetValue (ScalingModeProperty); }
			set { SetValue (ScalingModeProperty, value); }
		}
		private BitmapSource _bitmapSource;
		private static void OnSourceChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Slice9Image control = (Slice9Image)d;
			control.UpdateSlice ();
		}
		private static void OnSliceThicknessChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Slice9Image control = (Slice9Image)d;
			control.UpdateGridDefinition ();
			control.UpdateSlice ();
		}
		private static void OnScalingModeChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Slice9Image control = (Slice9Image)d;
			control.ApplyScalingMode ();
		}
		private void UpdateGridDefinition ()
		{
			SliceTop.Height = new GridLength (SliceThickness.Top);
			SliceBottom.Height = new GridLength (SliceThickness.Bottom);
			SliceLeft.Width = new GridLength (SliceThickness.Left);
			SliceRight.Width = new GridLength (SliceThickness.Right);
		}
		private void UpdateSlice ()
		{
			_bitmapSource = GetBitmapSource (Source);
			if (_bitmapSource == null)
			{
				ClearImages ();
				return;
			}
			int width = _bitmapSource.PixelWidth;
			int height = _bitmapSource.PixelHeight;
			int left = (int)Math.Round (SliceThickness.Left);
			int top = (int)Math.Round (SliceThickness.Top);
			int right = (int)Math.Round (SliceThickness.Right);
			int bottom = (int)Math.Round (SliceThickness.Bottom);
			if (left + right >= width || top + bottom >= height)
			{
				ClearImages ();
				return;
			}
			int centerWidth = width - left - right;
			int centerHeight = height - top - bottom;
			ImageTopLeft.Source = CreateCroppedBitmap (0, 0, left, top);
			//ImageTop.Source = CreateCroppedBitmap (left, 0, centerWidth, top);
			ImageTopRight.Source = CreateCroppedBitmap (left + centerWidth, 0, right, top);
			//ImageLeft.Source = CreateCroppedBitmap (0, top, left, centerHeight);
			//ImageCenter.Source = CreateCroppedBitmap (left, top, centerWidth, centerHeight);
			//ImageRight.Source = CreateCroppedBitmap (left + centerWidth, top, right, centerHeight);
			ImageBottomLeft.Source = CreateCroppedBitmap (0, top + centerHeight, left, bottom);
			//ImageBottom.Source = CreateCroppedBitmap (left, top + centerHeight, centerWidth, bottom);
			ImageBottomRight.Source = CreateCroppedBitmap (left + centerWidth, top + centerHeight, right, bottom);
			SetBorderBackground (BorderImageTop, CreateCroppedBitmap (left, 0, centerWidth, top));
			SetBorderBackground (BorderImageLeft, CreateCroppedBitmap (0, top, left, centerHeight));
			SetBorderBackground (BorderImageCenter, CreateCroppedBitmap (left, top, centerWidth, centerHeight));
			SetBorderBackground (BorderImageRight, CreateCroppedBitmap (left + centerWidth, top, right, centerHeight));
			SetBorderBackground (BorderImageBottom, CreateCroppedBitmap (left, top + centerHeight, centerWidth, bottom));
		}
		private CroppedBitmap CreateCroppedBitmap (int x, int y, int w, int h)
		{
			if (w <= 0 || h <= 0) return null;
			return new CroppedBitmap (_bitmapSource, new Int32Rect (x, y, w, h));
		}
		private void SetBorderBackground (Border border, CroppedBitmap bitmap)
		{
			if (border == null) return;
			if (bitmap == null)
			{
				border.Background = null;
				return;
			}
			var brush = new ImageBrush (bitmap);
			if (brush.CanFreeze) brush.Freeze ();
			border.Background = brush;
		}
		private void ClearImages ()
		{
			ImageTopLeft.Source = null;
			//ImageTop.Source = null;
			ImageTopRight.Source = null;
			//ImageLeft.Source = null;
			//ImageCenter.Source = null;
			//ImageRight.Source = null;
			ImageBottomLeft.Source = null;
			//ImageBottom.Source = null;
			ImageBottomRight.Source = null;
			ClearBorderBackground (BorderImageTop);
			ClearBorderBackground (BorderImageLeft);
			ClearBorderBackground (BorderImageCenter);
			ClearBorderBackground (BorderImageRight);
			ClearBorderBackground (BorderImageBottom);
		}
		private void ClearBorderBackground (Border border)
		{
			if (border != null) border.Background = null;
		}
		private BitmapSource GetBitmapSource (ImageSource source)
		{
			if (source == null) return null;
			BitmapSource bs = source as BitmapSource;
			if (bs != null) return bs;
			BitmapImage bitmap = source as BitmapImage;
			if (bitmap != null)
			{
				if (bitmap.CanFreeze) return bitmap;
				BitmapImage copy = new BitmapImage ();
				copy.BeginInit ();
				copy.UriSource = bitmap.UriSource;
				copy.CacheOption = BitmapCacheOption.OnLoad;
				copy.EndInit ();
				copy.Freeze ();
				return copy;
			}
			return null;
		}
		private void ApplyScalingMode ()
		{
			Image [] images = { ImageTopLeft, ImageTopRight, ImageBottomLeft,ImageBottomRight,
				/* ImageTop, ImageLeft, ImageCenter, ImageRight, ImageBottom */};
			
			foreach (var img in images)
			{
				if (img != null)
					RenderOptions.SetBitmapScalingMode (img, ScalingMode);
			}
		}
	}
}
