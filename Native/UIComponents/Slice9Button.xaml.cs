using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sidebar
{
	/// <summary>
	/// Slice9Button.xaml 的交互逻辑
	/// </summary>
	public partial class Slice9Button: UserControl
	{
		public Slice9Button ()
		{
			InitializeComponent ();
			SubscribeOpacityEvents ();
			var isEnabledDescriptor = DependencyPropertyDescriptor.FromProperty (
				UIElement.IsEnabledProperty, typeof (Slice9Button));
			isEnabledDescriptor.AddValueChanged (this, OnBaseIsEnabledChanged);
			OnBaseIsEnabledChanged (this, EventArgs.Empty);
		}
		private ButtonStatus _status = ButtonStatus.Normal;
		private ButtonStatus Status
		{
			get { return _status; }
			set
			{
				_status = value;
				UpdateStatusDisplay ();
			}
		}
		private void UpdateStatusDisplay ()
		{
			switch (_status)
			{
				case ButtonStatus.Active:
					if (ExclusiveStateDisplay)
						if (ImageActive.Source == null)
							goto case ButtonStatus.Hover;
						else
						{
							ShowImage (ImageHover, false);
							ShowImage (ImageDisabled, false);
							ShowImage (ImageFocus, false);
							ShowImage (ImageNormal, false);
						}
					else
					{
						ShowImage (ImageDisabled, false);
						ShowImage (ImageNormal, true);
					}
					ShowImage (ImageActive, true);
					break;
				case ButtonStatus.Hover:
					if (ExclusiveStateDisplay)
						if (ImageHover.Source == null)
							goto case ButtonStatus.Normal;
						else
						{
							ShowImage (ImageActive, false);
							ShowImage (ImageDisabled, false);
							ShowImage (ImageFocus, false);
							ShowImage (ImageNormal, false);
						}
					else
					{
						ShowImage (ImageDisabled, false);
						ShowImage (ImageActive, false);
						ShowImage (ImageNormal, true);
					}
					ShowImage (ImageHover, true);
					break;
				case ButtonStatus.Normal:
					if (ExclusiveStateDisplay)
					{
						ShowImage (ImageActive, false);
						ShowImage (ImageHover, false);
						ShowImage (ImageDisabled, false);
						ShowImage (ImageFocus, false);
					}
					else
					{
						ShowImage (ImageDisabled, false);
						ShowImage (ImageActive, false);
						ShowImage (ImageHover, false);
						ShowImage (ImageFocus, false);
					}
					ShowImage (ImageNormal, true);
					break;
				case ButtonStatus.Focus:
					if (ExclusiveStateDisplay)
						if (ImageFocus.Source == null)
							goto case ButtonStatus.Normal;
						else
						{
							ShowImage (ImageActive, false);
							ShowImage (ImageHover, false);
							ShowImage (ImageDisabled, false);
							ShowImage (ImageNormal, false);
						}
					else
					{
						ShowImage (ImageDisabled, false);
						ShowImage (ImageActive, false);
						ShowImage (ImageHover, false);
						ShowImage (ImageNormal, true);
					}
					ShowImage (ImageFocus, true);
					break;
				case ButtonStatus.Disabled:
					if (ExclusiveStateDisplay)
						if (ImageDisabled.Source == null)
							goto case ButtonStatus.Normal;
						else
						{
							ShowImage (ImageActive, false);
							ShowImage (ImageHover, false);
							ShowImage (ImageFocus, false);
							ShowImage (ImageNormal, false);
						}
					else
					{
						ShowImage (ImageActive, false);
						ShowImage (ImageHover, false);
						ShowImage (ImageFocus, false);
						ShowImage (ImageNormal, true);
					}
					ShowImage (ImageDisabled, true);
					break;
			}
		}
		private void SubscribeOpacityEvents ()
		{
			SubscribeOpacity (ImageNormal);
			SubscribeOpacity (ImageFocus);
			SubscribeOpacity (ImageHover);
			SubscribeOpacity (ImageActive);
			SubscribeOpacity (ImageDisabled);
		}
		private void SubscribeOpacity (FrameworkElement element)
		{
			if (element == null) return;
			var descriptor = DependencyPropertyDescriptor.FromProperty (UIElement.OpacityProperty, typeof (FrameworkElement));
			descriptor.AddValueChanged (element, OnOpacityChanged);
			OnOpacityChanged (element, EventArgs.Empty);
		}
		private void OnOpacityChanged (object sender, EventArgs e)
		{
			FrameworkElement element = sender as FrameworkElement;
			if (element == null) return;
			element.Visibility = (element.Opacity <= 0) ? Visibility.Hidden : Visibility.Visible;
		}
		public static readonly RoutedEvent ClickEvent =
			EventManager.RegisterRoutedEvent ("Click", RoutingStrategy.Bubble,
				typeof (RoutedEventHandler), typeof (Slice9Button));
		public event RoutedEventHandler Click
		{
			add { AddHandler (ClickEvent, value); }
			remove { RemoveHandler (ClickEvent, value); }
		}
		private void OnBaseIsEnabledChanged (object sender, EventArgs e)
		{
			Status = IsEnabled ? (IsFocused ? ButtonStatus.Focus : ButtonStatus.Normal) : ButtonStatus.Disabled;
		}
		public static readonly DependencyProperty UseAnimationProperty =
			DependencyProperty.Register ("UseAnimation", typeof (bool), typeof (Slice9Button),
				new FrameworkPropertyMetadata (false));
		public bool UseAnimation
		{
			get { return (bool)GetValue (UseAnimationProperty); }
			set { SetValue (UseAnimationProperty, value); }
		}
		/// <summary>
		/// Animation Duration (Second)
		/// </summary>
		public static readonly DependencyProperty AnimationDurationProperty =
			DependencyProperty.Register ("AnimationDuration", typeof (double), typeof (Slice9Button),
				new FrameworkPropertyMetadata (0.2));
		/// <summary>
		/// Animation Duration (Second)
		/// </summary>
		public double AnimationDuration
		{
			get { return (double)GetValue (AnimationDurationProperty); }
			set { SetValue (AnimationDurationProperty, value); }
		}
		private bool isdown = false;
		private void UserControl_MouseEnter (object sender, MouseEventArgs e)
		{
			if (!IsEnabled) return;
			Status = ButtonStatus.Hover;
		}
		private void UserControl_MouseLeave (object sender, MouseEventArgs e)
		{
			if (!IsEnabled) return;
			Status = IsFocused ? ButtonStatus.Focus : ButtonStatus.Normal;
		}
		private void UserControl_MouseLeftButtonDown (object sender, MouseButtonEventArgs e)
		{
			if (!IsEnabled) return;
			isdown = true;
			Status = ButtonStatus.Active;
		}
		private void UserControl_MouseLeftButtonUp (object sender, MouseButtonEventArgs e)
		{
			if (!IsEnabled)
			{
				isdown = false;
				return;
			}
			if (isdown)
			{
				isdown = false;
				InvokeClick ();
			}
			if (Status != ButtonStatus.Active) return;
			else Status = ButtonStatus.Hover;
		}
		private void UserControl_TouchEnter (object sender, TouchEventArgs e)
		{
			if (!IsEnabled) return;
			Status = ButtonStatus.Hover;
		}
		private void UserControl_TouchLeave (object sender, TouchEventArgs e)
		{
			if (!IsEnabled) return;
			Status = IsFocused ? ButtonStatus.Focus : ButtonStatus.Normal;
		}
		private void UserControl_TouchUp (object sender, TouchEventArgs e)
		{
			if (!IsEnabled)
			{
				isdown = false;
				return;
			}
			if (isdown)
			{
				isdown = false;
				InvokeClick ();
			}
			if (Status != ButtonStatus.Active) return;
			else Status = ButtonStatus.Hover;
		}
		private void UserControl_TouchDown (object sender, TouchEventArgs e)
		{
			if (!IsEnabled) return;
			isdown = true;
			Status = ButtonStatus.Active;
		}
		private void InvokeClick ()
		{
			if (!IsEnabled) return;
			RoutedEventArgs args = new RoutedEventArgs (ClickEvent);
			RaiseEvent (args);
		}
		private void ShowImage (FrameworkElement fe, bool show)
		{
			if (fe == null) return;
			double targetOpacity = show ? 1.0 : 0.0;
			double currentOpacity = fe.Opacity;
			if (Math.Abs (currentOpacity - targetOpacity) < 0.001)
				return;
			fe.BeginAnimation (UIElement.OpacityProperty, null);
			fe.Opacity = currentOpacity;
			bool shouldAnimate =
				UseAnimation &&
				AnimationDuration > 0 &&
				!double.IsNaN (AnimationDuration) &&
				!double.IsInfinity (AnimationDuration);
			if (shouldAnimate)
			{
				double duration = AnimationDuration * Math.Abs (targetOpacity - currentOpacity);
				DoubleAnimation animation = new DoubleAnimation ();
				animation.From = currentOpacity;
				animation.To = targetOpacity;
				animation.Duration = TimeSpan.FromSeconds (duration);
				animation.FillBehavior = FillBehavior.HoldEnd;
				fe.BeginAnimation (UIElement.OpacityProperty, animation);
			}
			else
			{
				fe.Opacity = targetOpacity;
			}
		}
		public static readonly DependencyProperty NormalSourceProperty =
		   DependencyProperty.Register ("NormalSource", typeof (ImageSource), typeof (Slice9Button));
		public static readonly DependencyProperty NormalSliceThicknessProperty =
			DependencyProperty.Register ("NormalSliceThickness", typeof (Thickness), typeof (Slice9Button),
				new FrameworkPropertyMetadata (new Thickness (0)));
		public static readonly DependencyProperty NormalScalingModeProperty =
			DependencyProperty.Register ("NormalScalingMode", typeof (BitmapScalingMode), typeof (Slice9Button),
				new FrameworkPropertyMetadata (BitmapScalingMode.HighQuality));
		public ImageSource NormalSource
		{
			get { return (ImageSource)GetValue (NormalSourceProperty); }
			set { SetValue (NormalSourceProperty, value); }
		}
		public Thickness NormalSliceThickness
		{
			get { return (Thickness)GetValue (NormalSliceThicknessProperty); }
			set { SetValue (NormalSliceThicknessProperty, value); }
		}
		public BitmapScalingMode NormalScalingMode
		{
			get { return (BitmapScalingMode)GetValue (NormalScalingModeProperty); }
			set { SetValue (NormalScalingModeProperty, value); }
		}
		public static readonly DependencyProperty FocusSourceProperty =
			DependencyProperty.Register ("FocusSource", typeof (ImageSource), typeof (Slice9Button));
		public static readonly DependencyProperty FocusSliceThicknessProperty =
			DependencyProperty.Register ("FocusSliceThickness", typeof (Thickness), typeof (Slice9Button),
				new FrameworkPropertyMetadata (new Thickness (0)));
		public static readonly DependencyProperty FocusScalingModeProperty =
			DependencyProperty.Register ("FocusScalingMode", typeof (BitmapScalingMode), typeof (Slice9Button),
				new FrameworkPropertyMetadata (BitmapScalingMode.HighQuality));
		public ImageSource FocusSource
		{
			get { return (ImageSource)GetValue (FocusSourceProperty); }
			set { SetValue (FocusSourceProperty, value); }
		}
		public Thickness FocusSliceThickness
		{
			get { return (Thickness)GetValue (FocusSliceThicknessProperty); }
			set { SetValue (FocusSliceThicknessProperty, value); }
		}
		public BitmapScalingMode FocusScalingMode
		{
			get { return (BitmapScalingMode)GetValue (FocusScalingModeProperty); }
			set { SetValue (FocusScalingModeProperty, value); }
		}
		public static readonly DependencyProperty HoverSourceProperty =
			DependencyProperty.Register ("HoverSource", typeof (ImageSource), typeof (Slice9Button));
		public static readonly DependencyProperty HoverSliceThicknessProperty =
			DependencyProperty.Register ("HoverSliceThickness", typeof (Thickness), typeof (Slice9Button),
				new FrameworkPropertyMetadata (new Thickness (0)));
		public static readonly DependencyProperty HoverScalingModeProperty =
			DependencyProperty.Register ("HoverScalingMode", typeof (BitmapScalingMode), typeof (Slice9Button),
				new FrameworkPropertyMetadata (BitmapScalingMode.HighQuality));
		public ImageSource HoverSource
		{
			get { return (ImageSource)GetValue (HoverSourceProperty); }
			set { SetValue (HoverSourceProperty, value); }
		}
		public Thickness HoverSliceThickness
		{
			get { return (Thickness)GetValue (HoverSliceThicknessProperty); }
			set { SetValue (HoverSliceThicknessProperty, value); }
		}
		public BitmapScalingMode HoverScalingMode
		{
			get { return (BitmapScalingMode)GetValue (HoverScalingModeProperty); }
			set { SetValue (HoverScalingModeProperty, value); }
		}
		public static readonly DependencyProperty ActiveSourceProperty =
			DependencyProperty.Register ("ActiveSource", typeof (ImageSource), typeof (Slice9Button));
		public static readonly DependencyProperty ActiveSliceThicknessProperty =
			DependencyProperty.Register ("ActiveSliceThickness", typeof (Thickness), typeof (Slice9Button),
				new FrameworkPropertyMetadata (new Thickness (0)));
		public static readonly DependencyProperty ActiveScalingModeProperty =
			DependencyProperty.Register ("ActiveScalingMode", typeof (BitmapScalingMode), typeof (Slice9Button),
				new FrameworkPropertyMetadata (BitmapScalingMode.HighQuality));
		public ImageSource ActiveSource
		{
			get { return (ImageSource)GetValue (ActiveSourceProperty); }
			set { SetValue (ActiveSourceProperty, value); }
		}
		public Thickness ActiveSliceThickness
		{
			get { return (Thickness)GetValue (ActiveSliceThicknessProperty); }
			set { SetValue (ActiveSliceThicknessProperty, value); }
		}
		public BitmapScalingMode ActiveScalingMode
		{
			get { return (BitmapScalingMode)GetValue (ActiveScalingModeProperty); }
			set { SetValue (ActiveScalingModeProperty, value); }
		}
		public static readonly DependencyProperty DisabledSourceProperty =
			DependencyProperty.Register ("DisabledSource", typeof (ImageSource), typeof (Slice9Button));
		public static readonly DependencyProperty DisabledSliceThicknessProperty =
			DependencyProperty.Register ("DisabledSliceThickness", typeof (Thickness), typeof (Slice9Button),
				new FrameworkPropertyMetadata (new Thickness (0)));
		public static readonly DependencyProperty DisabledScalingModeProperty =
			DependencyProperty.Register ("DisabledScalingMode", typeof (BitmapScalingMode), typeof (Slice9Button),
				new FrameworkPropertyMetadata (BitmapScalingMode.HighQuality));
		public ImageSource DisabledSource
		{
			get { return (ImageSource)GetValue (DisabledSourceProperty); }
			set { SetValue (DisabledSourceProperty, value); }
		}
		public Thickness DisabledSliceThickness
		{
			get { return (Thickness)GetValue (DisabledSliceThicknessProperty); }
			set { SetValue (DisabledSliceThicknessProperty, value); }
		}
		public BitmapScalingMode DisabledScalingMode
		{
			get { return (BitmapScalingMode)GetValue (DisabledScalingModeProperty); }
			set { SetValue (DisabledScalingModeProperty, value); }
		}
		private void UserControl_GotFocus (object sender, RoutedEventArgs e)
		{
			if (!IsEnabled) return;
			if (Status == ButtonStatus.Normal) Status = ButtonStatus.Focus;
		}
		private void UserControl_LostFocus (object sender, RoutedEventArgs e)
		{
			if (!IsEnabled) return;
			if (Status == ButtonStatus.Focus) Status = ButtonStatus.Normal;
		}
		public static readonly DependencyProperty ChildProperty =
			DependencyProperty.Register (
				"Child",
				typeof (object),
				typeof (Slice9Button),
				new FrameworkPropertyMetadata (null));
		public object Child
		{
			get { return GetValue (ChildProperty); }
			set { SetValue (ChildProperty, value); }
		}
		public string Text
		{
			get { return Child as string; }
			set { Child = value; }
		}
		public static readonly DependencyProperty ExclusiveStateDisplayProperty =
			DependencyProperty.Register (
				"ExclusiveStateDisplay",
				typeof (bool),
				typeof (Slice9Button),
				new FrameworkPropertyMetadata (
					false,
					OnExclusiveStateDisplayChanged));
		public bool ExclusiveStateDisplay
		{
			get { return (bool)GetValue (ExclusiveStateDisplayProperty); }
			set { SetValue (ExclusiveStateDisplayProperty, value); }
		}
		private static void OnExclusiveStateDisplayChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Slice9Button button = d as Slice9Button;
			button?.UpdateStatusDisplay ();
		}
	}
}
