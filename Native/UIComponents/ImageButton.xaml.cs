using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Sidebar
{
	public partial class ImageButton: UserControl
	{
		public ImageButton ()
		{
			InitializeComponent ();
			SubscribeOpacityEvents ();
			this.HorizontalContentAlignment = HorizontalAlignment.Center;
			this.VerticalContentAlignment = VerticalAlignment.Center;
			var isEnabledDescriptor = DependencyPropertyDescriptor.FromProperty (
				UIElement.IsEnabledProperty, typeof (ImageButton));
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

		// ---------- 状态显示逻辑 ----------
		private void UpdateStatusDisplay ()
		{
			switch (_status)
			{
				case ButtonStatus.Active:
					if (ExclusiveStateDisplay)
					{
						if (ImageActive.Source == null)
							goto case ButtonStatus.Hover;
						else
						{
							ShowImage (ImageHover, false);
							ShowImage (ImageDisabled, false);
							ShowImage (ImageFocus, false);
							ShowImage (ImageNormal, false);
						}
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
					{
						if (ImageHover.Source == null)
							goto case ButtonStatus.Normal;
						else
						{
							ShowImage (ImageActive, false);
							ShowImage (ImageDisabled, false);
							ShowImage (ImageFocus, false);
							ShowImage (ImageNormal, false);
						}
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
					{
						if (ImageFocus.Source == null)
							goto case ButtonStatus.Normal;
						else
						{
							ShowImage (ImageActive, false);
							ShowImage (ImageHover, false);
							ShowImage (ImageDisabled, false);
							ShowImage (ImageNormal, false);
						}
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
					{
						if (ImageDisabled.Source == null)
							goto case ButtonStatus.Normal;
						else
						{
							ShowImage (ImageActive, false);
							ShowImage (ImageHover, false);
							ShowImage (ImageFocus, false);
							ShowImage (ImageNormal, false);
						}
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

		// ---------- 透明度动画辅助 ----------
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
			var descriptor = DependencyPropertyDescriptor.FromProperty (
				UIElement.OpacityProperty, typeof (FrameworkElement));
			descriptor.AddValueChanged (element, OnOpacityChanged);
			OnOpacityChanged (element, EventArgs.Empty);
		}

		private void OnOpacityChanged (object sender, EventArgs e)
		{
			FrameworkElement element = sender as FrameworkElement;
			if (element == null) return;
			element.Visibility = (element.Opacity <= 0) ? Visibility.Hidden : Visibility.Visible;
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

			bool shouldAnimate = UseAnimation &&
								 AnimationDuration > 0 &&
								 !double.IsNaN (AnimationDuration) &&
								 !double.IsInfinity (AnimationDuration);

			if (shouldAnimate)
			{
				double duration = AnimationDuration * Math.Abs (targetOpacity - currentOpacity);
				DoubleAnimation animation = new DoubleAnimation {
					From = currentOpacity,
					To = targetOpacity,
					Duration = TimeSpan.FromSeconds (duration),
					FillBehavior = FillBehavior.HoldEnd
				};
				fe.BeginAnimation (UIElement.OpacityProperty, animation);
			}
			else
			{
				fe.Opacity = targetOpacity;
			}
		}

		// ---------- 事件处理 ----------
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
			Status = ButtonStatus.Hover;
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
			Status = ButtonStatus.Hover;
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

		private void OnBaseIsEnabledChanged (object sender, EventArgs e)
		{
			Status = IsEnabled ? (IsFocused ? ButtonStatus.Focus : ButtonStatus.Normal) : ButtonStatus.Disabled;
		}

		// ---------- Click 事件 ----------
		public static readonly RoutedEvent ClickEvent =
			EventManager.RegisterRoutedEvent ("Click", RoutingStrategy.Bubble,
				typeof (RoutedEventHandler), typeof (ImageButton));

		public event RoutedEventHandler Click
		{
			add { AddHandler (ClickEvent, value); }
			remove { RemoveHandler (ClickEvent, value); }
		}

		// ---------- 依赖属性 ----------

		// 动画属性
		public static readonly DependencyProperty UseAnimationProperty =
			DependencyProperty.Register ("UseAnimation", typeof (bool), typeof (ImageButton),
				new FrameworkPropertyMetadata (false));

		public bool UseAnimation
		{
			get { return (bool)GetValue (UseAnimationProperty); }
			set { SetValue (UseAnimationProperty, value); }
		}

		public static readonly DependencyProperty AnimationDurationProperty =
			DependencyProperty.Register ("AnimationDuration", typeof (double), typeof (ImageButton),
				new FrameworkPropertyMetadata (0.2));

		public double AnimationDuration
		{
			get { return (double)GetValue (AnimationDurationProperty); }
			set { SetValue (AnimationDurationProperty, value); }
		}

		// 子内容
		public static readonly DependencyProperty ChildProperty =
			DependencyProperty.Register ("Child", typeof (object), typeof (ImageButton),
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

		// 独占状态显示模式
		public static readonly DependencyProperty ExclusiveStateDisplayProperty =
			DependencyProperty.Register ("ExclusiveStateDisplay", typeof (bool), typeof (ImageButton),
				new FrameworkPropertyMetadata (false, OnExclusiveStateDisplayChanged));

		public bool ExclusiveStateDisplay
		{
			get { return (bool)GetValue (ExclusiveStateDisplayProperty); }
			set { SetValue (ExclusiveStateDisplayProperty, value); }
		}

		private static void OnExclusiveStateDisplayChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ImageButton button = d as ImageButton;
			button?.UpdateStatusDisplay ();
		}

		// ---- 五个状态的 Source 和 Stretch ----

		// Normal
		public static readonly DependencyProperty NormalSourceProperty =
			DependencyProperty.Register ("NormalSource", typeof (ImageSource), typeof (ImageButton));

		public ImageSource NormalSource
		{
			get { return (ImageSource)GetValue (NormalSourceProperty); }
			set { SetValue (NormalSourceProperty, value); }
		}

		public static readonly DependencyProperty NormalStretchProperty =
			DependencyProperty.Register ("NormalStretch", typeof (Stretch), typeof (ImageButton),
				new FrameworkPropertyMetadata (Stretch.Uniform));

		public Stretch NormalStretch
		{
			get { return (Stretch)GetValue (NormalStretchProperty); }
			set { SetValue (NormalStretchProperty, value); }
		}

		// Focus
		public static readonly DependencyProperty FocusSourceProperty =
			DependencyProperty.Register ("FocusSource", typeof (ImageSource), typeof (ImageButton));

		public ImageSource FocusSource
		{
			get { return (ImageSource)GetValue (FocusSourceProperty); }
			set { SetValue (FocusSourceProperty, value); }
		}

		public static readonly DependencyProperty FocusStretchProperty =
			DependencyProperty.Register ("FocusStretch", typeof (Stretch), typeof (ImageButton),
				new FrameworkPropertyMetadata (Stretch.Uniform));

		public Stretch FocusStretch
		{
			get { return (Stretch)GetValue (FocusStretchProperty); }
			set { SetValue (FocusStretchProperty, value); }
		}

		// Hover
		public static readonly DependencyProperty HoverSourceProperty =
			DependencyProperty.Register ("HoverSource", typeof (ImageSource), typeof (ImageButton));

		public ImageSource HoverSource
		{
			get { return (ImageSource)GetValue (HoverSourceProperty); }
			set { SetValue (HoverSourceProperty, value); }
		}

		public static readonly DependencyProperty HoverStretchProperty =
			DependencyProperty.Register ("HoverStretch", typeof (Stretch), typeof (ImageButton),
				new FrameworkPropertyMetadata (Stretch.Uniform));

		public Stretch HoverStretch
		{
			get { return (Stretch)GetValue (HoverStretchProperty); }
			set { SetValue (HoverStretchProperty, value); }
		}

		// Active
		public static readonly DependencyProperty ActiveSourceProperty =
			DependencyProperty.Register ("ActiveSource", typeof (ImageSource), typeof (ImageButton));

		public ImageSource ActiveSource
		{
			get { return (ImageSource)GetValue (ActiveSourceProperty); }
			set { SetValue (ActiveSourceProperty, value); }
		}

		public static readonly DependencyProperty ActiveStretchProperty =
			DependencyProperty.Register ("ActiveStretch", typeof (Stretch), typeof (ImageButton),
				new FrameworkPropertyMetadata (Stretch.Uniform));

		public Stretch ActiveStretch
		{
			get { return (Stretch)GetValue (ActiveStretchProperty); }
			set { SetValue (ActiveStretchProperty, value); }
		}

		// Disabled
		public static readonly DependencyProperty DisabledSourceProperty =
			DependencyProperty.Register ("DisabledSource", typeof (ImageSource), typeof (ImageButton));

		public ImageSource DisabledSource
		{
			get { return (ImageSource)GetValue (DisabledSourceProperty); }
			set { SetValue (DisabledSourceProperty, value); }
		}

		public static readonly DependencyProperty DisabledStretchProperty =
			DependencyProperty.Register ("DisabledStretch", typeof (Stretch), typeof (ImageButton),
				new FrameworkPropertyMetadata (Stretch.Uniform));

		public Stretch DisabledStretch
		{
			get { return (Stretch)GetValue (DisabledStretchProperty); }
			set { SetValue (DisabledStretchProperty, value); }
		}
	}
}