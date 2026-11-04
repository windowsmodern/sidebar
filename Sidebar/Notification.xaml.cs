using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Sidebar
{
	/// <summary>
	/// Notification.xaml 的交互逻辑
	/// </summary>
	public partial class Notification: Window, IWindowInterop
	{
		private DispatcherTimer widthRefreshTimer = new DispatcherTimer () {
			Interval = TimeSpan.FromSeconds (0.1)
		};
		private Thickness timeoutInitMargin;
		private WindowInteropHelper wih = null;
		public WindowInteropHelper InteropHelper => wih;
		public IntPtr Handle => InteropHelper?.Handle ?? IntPtr.Zero;
		public IntPtr WndOwner => InteropHelper?.Owner ?? IntPtr.Zero;
		public Notification ()
		{
			InitializeComponent ();
			if (DWMAPI.IsElderWindows () && true)
			{
				AllowsTransparency = false;
			}
			wih = new WindowInteropHelper (this);
			timeoutInitMargin = TimeoutBar.Margin;
			HWND hwnd = (HWND)Handle;
			hwnd.Styles &= ~(Win32.WindowStyles.WS_CAPTION | Win32.WindowStyles.WS_BORDER | Win32.WindowStyles.WS_DLGFRAME | (Win32.WindowStyles)(0x00C00000L | 0x00C0000L));
			hwnd.StylesEx |= Win32.ExtendedWindowStyles.WS_EX_LAYERED;
		}
		private void CloseButton_Click (object sender, RoutedEventArgs e)
		{
			try
			{
				foreach (var h in tipCloseClick)
					h?.Invoke (this, e);
				Close ();
			}
			catch { }
		}
		private void BackgroundContainer_SizeChanged (object sender, System.Windows.SizeChangedEventArgs e)
		{
			var tag = BackgroundHeightChange.Tag;
			var splited = (tag as string ?? "").Split (',');
			List<double> members = null;
			try
			{
				members = splited.Select (s => Convert.ToDouble (s)).ToList ();
				if (members.Count <= 0) members.Add (Convert.ToDouble (tag as string));
			}
			catch { }
			if (members == null) members = new List<double> ();
			if (members.Count == 0)
			{
				members.Add (0);
				members.Add (0);
				members.Add (0);
				members.Add (0);
			}
			else if (members.Count == 1)
			{
				members.Add (members [0]);
				members.Add (members [0]);
				members.Add (members [0]);
			}
			else if (members.Count == 2)
			{
				members.Add (members [0]);
				members.Add (members [1]);
			}
			else if (members.Count == 3) members.Add (members [1]);
			var height = BackgroundContainer.ActualHeight - BackgroundHeightChange.ActualHeight - BackgroundHeightChange.ActualWidth;
			if (height < 0) height = 0;
			var top = BackgroundHeightChange.ActualHeight;
			BackgroundContainer.Clip = new RectangleGeometry (
				new Rect (
					0,
					top,
					BackgroundContainer.ActualWidth,
					height
				), // 裁剪区域
				members [0],
				members [1]
			);
			var contains = Application.Current.Resources.Contains ("EnableNotificationClipRound");
			bool isclip = false;
			if (contains) isclip = (bool)Application.Current.Resources ["EnableNotificationClipRound"] | DWMAPI.IsElderWindows ();
			if (isclip)
			{
				Matrix transform = GetTransformToDevice (this);
				double scaleX = transform.M11;
				double scaleY = transform.M22;
				int w = (int)Math.Round (BackgroundContainer.ActualWidth * scaleX);
				int h = (int)Math.Round ((height + BackgroundHeightChange.Height) * scaleY + 1);
				int radiusX = (int)Math.Round (members [0] * scaleX);
				int radiusY = (int)Math.Round (members [1] * scaleY);
				ApplyRoundedRegion (0, (int)(top * scaleY), w, h, radiusX, radiusY);
			}
		}
		private static Matrix GetTransformToDevice (Window window)
		{
			var source = PresentationSource.FromVisual (window);
			return source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;
		}
		private void ApplyRoundedRegion (int l, int t, int w, int h, int r1, int r2)
		{
			IntPtr hRgn = Win32WindowNative.CreateRoundRectRgn (l, t, w, h, r1, r2);
			Win32WindowNative.SetWindowRgn (this.Handle, hRgn, true);
			Win32WindowNative.DeleteObject (hRgn);
		}
		private void UpdateWidth ()
		{
			ClearValue (WidthProperty);
			UpdateLayout ();
			var width = MessageTitle.ActualWidth +
				(MessageTitle.Margin.Left + MessageTitle.Margin.Right) +
				TextAreaBorderOuter.BorderThickness.Left + TextAreaBorderOuter.BorderThickness.Right + TextAreaBorderOuter.Margin.Left + TextAreaBorderOuter.Margin.Right + TextAreaBorderOuter.Padding.Left + TextAreaBorderOuter.Padding.Right +
				TextAreaBorderInner.BorderThickness.Left + TextAreaBorderInner.BorderThickness.Right + TextAreaBorderInner.Margin.Left + TextAreaBorderInner.Margin.Right + TextAreaBorderInner.Padding.Left + TextAreaBorderInner.Padding.Right +
				TextAreaTextArea.BorderThickness.Left + TextAreaTextArea.BorderThickness.Right + TextAreaTextArea.Margin.Left + TextAreaTextArea.Margin.Right + TextAreaTextArea.Padding.Left + TextAreaTextArea.Padding.Right +
				NotificationUI.Margin.Left + NotificationUI.Margin.Right + NotificationUI.BorderThickness.Left + NotificationUI.BorderThickness.Right + NotificationUI.Padding.Left + NotificationUI.Padding.Right;
			if (double.IsNaN (width) || width < MinWidth) ClearValue (WidthProperty);
			Width = width;
			UpdateLayout ();
		}
		private TimeSpan timeSpan = TimeSpan.FromSeconds (30);
		private TimeSpan ringTimeSpan = new TimeSpan (0);
		private void Root_Loaded (object sender, RoutedEventArgs e)
		{
			widthRefreshTimer.Tick -= WidthRefreshTimer_Tick;
			widthRefreshTimer.Tick += WidthRefreshTimer_Tick;
			widthRefreshTimer.Start ();
			UpdateLocation ();
			AnimateTimeoutBar ();
			App.CurrentUserConfig.PropertyChanged += CurrentUserConfig_PropertyChanged;
			var ringtimeout = SoundHelper.PlayNotificationAndGetDuration ();
			ringTimeSpan = TimeSpan.FromMilliseconds (ringtimeout);
			UpdateTimeout (TimeSpan.FromMilliseconds (Math.Max (ringTimeSpan.Milliseconds, timeSpan.Milliseconds)));
			ScreenChangeNotifier.WorkingAreaChanged += ScreenChangeNotifier_WorkingAreaChanged;
		}
		private void ScreenChangeNotifier_WorkingAreaChanged (object sender, EventArgs e)
		{
			UpdateLocation ();
		}
		private void CurrentUserConfig_PropertyChanged (object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			var cuc = App.CurrentUserConfig;
			switch (e.PropertyName)
			{
				case nameof (cuc.ThemeName):
					var contains = Application.Current.Resources.Contains ("EnableNotificationClipRound");
					bool isclip = false;
					if (contains) isclip = (bool)Application.Current.Resources ["EnableNotificationClipRound"] || DWMAPI.IsElderWindows ();
					if (isclip) BackgroundContainer_SizeChanged (this, null);
					else Win32WindowNative.SetWindowRgn (this.Handle, IntPtr.Zero, true);
					bool isaero = false;
					var aerpcontains = Application.Current.Resources.Contains ("EnableNotificationBlur");
					if (aerpcontains) isaero = (bool)Application.Current.Resources ["EnableNotificationBlur"] && !DWMAPI.IsElderWindows ();
					ChangeAeroStatus (isaero);
					UpdateWidth ();
					UpdateLocation ();
					break;
				case nameof (cuc.Direction):
					UpdateLocation ();
					break;
			}
		}
		public void UpdateTimeout (TimeSpan ts)
		{
			timeSpan = TimeSpan.FromMilliseconds (Math.Max (ringTimeSpan.Milliseconds, ts.Milliseconds));
			if (IsLoaded) AnimateTimeoutBar ();
		}
		private AnimationClock ac = null;
		private void AnimateTimeoutBar ()
		{
			if (TimeoutBar == null || !IsLoaded) return;
			if (TimeoutBar.ActualHeight <= 0)
			{
				Dispatcher.BeginInvoke (new Action (() => AnimateTimeoutBar ()), System.Windows.Threading.DispatcherPriority.Loaded);
				return;
			}
			double initTop = timeoutInitMargin.Top;
			double height = TimeoutBar.ActualHeight;
			double targetTop = height - Math.Abs (initTop) - 10;
			if (targetTop <= 0) targetTop = 0;
			if (timeSpan.Seconds <= 0) timeSpan = TimeSpan.FromSeconds (30);
			ThicknessAnimation animation = new ThicknessAnimation {
				From = new Thickness (0, initTop, 0, 0),
				To = new Thickness (0, targetTop, 0, 0),
				Duration = timeSpan
			};
			animation.Completed += TimeoutAnimation_Completed;
			ac = animation.CreateClock ();
			TimeoutBar.ApplyAnimationClock (Control.MarginProperty, ac);
		}
		private void TimeoutAnimation_Completed (object sender, EventArgs e)
		{
			try { this.Close (); } catch { }
		}
		public void PauseTimeoutAnimation ()
		{
			if (ac != null && ac.Controller != null)
			{
				ac.Controller.Pause ();
			}
		}
		public void ResumeTimeoutAnimation ()
		{
			if (ac != null && ac.Controller != null)
			{
				ac.Controller.Resume ();
			}
		}
		private void WidthRefreshTimer_Tick (object sender, EventArgs e)
		{
			widthRefreshTimer?.Stop ();
			UpdateWidth ();
			UpdateLocation ();
		}
		private void Root_Unloaded (object sender, RoutedEventArgs e)
		{
			widthRefreshTimer.Tick -= WidthRefreshTimer_Tick;
		}
		private void Root_SizeChanged (object sender, System.Windows.SizeChangedEventArgs e)
		{
			UpdateLocation ();
		}
		private void UpdateLocation ()
		{
			HWND hwnd = Handle;
			var scr = ScreenHelper.GetScreenByHWND (hwnd) ?? System.Windows.Forms.Screen.PrimaryScreen;
			var wa = scr.WorkingArea;
			var sidebar = App.Current.MainWindow;
			var left = wa.Left + wa.Width;
			if (App.CurrentUserConfig.Direction == SidebarDirection.Right)
				left = Math.Min (sidebar.GetPixelLeft (), left);
			hwnd.Move (left - this.GetPixelWidth (), wa.Top + wa.Height - this.GetPixelHeight ());
		}
		private void Root_Closed (object sender, EventArgs e)
		{
			ScreenChangeNotifier.WorkingAreaChanged -= ScreenChangeNotifier_WorkingAreaChanged;
			Utilities.ReleaseLargeResourcesAsync ();
			lock (popuplock)
			{
				try
				{
					if ((noticeQueue?.Count ?? 0) > 0)
					{
						noticeQueue.Dequeue ();
						PopupNotificationQueue ();
					}
				}
				catch { }
			}
			try
			{
				foreach (var h in tipClosed) h?.Invoke (sender, e);
			}
			catch { }
			tipClicked?.Clear ();
			tipClosed?.Clear ();
			tipCloseClick?.Clear ();
		}
		private void Root_SourceInitialized (object sender, EventArgs e)
		{
			bool isaero = false;
			var aerpcontains = Application.Current.Resources.Contains ("EnableNotificationBlur");
			if (aerpcontains) isaero = (bool)Application.Current.Resources ["EnableNotificationBlur"] && !DWMAPI.IsElderWindows ();
			ChangeAeroStatus (isaero);
		}
		private bool isdown = false;
		private void ScrollViewer_MouseDown (object sender, MouseButtonEventArgs e)
		{
			isdown = true;
		}
		private void ScrollViewer_MouseUp (object sender, MouseButtonEventArgs e)
		{
			if (isdown)
			{
				isdown = false;
				OnNotificationClick ();
			}
		}
		private void ScrollViewer_TouchDown (object sender, TouchEventArgs e)
		{
			isdown = true;
		}
		private void ScrollViewer_TouchUp (object sender, TouchEventArgs e)
		{
			if (isdown)
			{
				isdown = false;
				OnNotificationClick ();
			}
		}
		private void OnNotificationClick ()
		{
			var e = new EventArgs ();
			try
			{
				foreach (var h in tipClicked) h?.Invoke (this, e);
			}
			finally
			{
				try { PauseTimeoutAnimation (); Close (); } catch { }
			}
		}
		private HashSet<EventHandler> tipClicked = new HashSet<EventHandler> ();
		private HashSet<EventHandler> tipClosed = new HashSet<EventHandler> ();
		private HashSet<EventHandler> tipCloseClick = new HashSet<EventHandler> ();
		public event EventHandler BalloonTipClicked
		{
			add { tipClicked?.Add (value); }
			remove { tipClicked?.Remove (value); }
		}
		public event EventHandler BalloonTipClosed
		{
			add { tipClosed?.Add (value); }
			remove { tipClosed?.Remove (value); }
		}
		public event EventHandler BalloonTipCloseClicked
		{
			add { tipCloseClick?.Add (value); }
			remove { tipCloseClick?.Remove (value); }
		}
		private void Root_MouseEnter (object sender, MouseEventArgs e)
		{
			PauseTimeoutAnimation ();
		}
		private void Root_MouseLeave (object sender, MouseEventArgs e)
		{
			ResumeTimeoutAnimation ();
		}
		private void Root_TouchEnter (object sender, TouchEventArgs e)
		{
			PauseTimeoutAnimation ();
		}
		private void Root_TouchLeave (object sender, TouchEventArgs e)
		{
			ResumeTimeoutAnimation ();
		}
		private bool? _lastAeroState = null;
		public void ChangeAeroStatus (bool state)
		{
			if (!DWMAPI.IsDwmAvailable ()) return;
			if (_lastAeroState.HasValue && _lastAeroState.Value == state) return;
			_lastAeroState = state;
			IntPtr hwnd = Handle;
			if (state)
			{
				DWMAPI.EnableBlur (ref hwnd, IntPtr.Zero);
				try { if (!AllowsTransparency) AllowsTransparency = true; } catch { }
			}
			else
			{
				DWMAPI.DisableBlur (ref hwnd);
			}
			try { if (Environment.OSVersion.Version.Major > 6) WindowAccent.SetAccentPolicy (hwnd, state); } catch { }
		}
		static Notification ()
		{
			poptimer.Tick -= PopupNotification_Tick;
			poptimer.Tick += PopupNotification_Tick;
		}
		private static void PopupNotification_Tick (object sender, EventArgs e)
		{
			poptimer.Stop ();
			if (noticeQueue.Count > 0)
			{
				try
				{
					var poped = noticeQueue.Peek ();
					poped.Show ();
				}
				catch { }
			}
		}
		private static Queue<Notification> noticeQueue = new Queue<Notification> ();
		private static DispatcherTimer poptimer = new DispatcherTimer () {
			Interval = TimeSpan.FromSeconds (0.01)
		};
		private static object popuplock = new object ();
		private static void PopupNotificationQueue ()
		{
			//poptimer.Stop (); poptimer.Start ();
			PopupNotification_Tick (poptimer, new EventArgs ());
		}
		public static Notification ShowNotification (NotifyIconNotification nin, ImageSource icon = null)
		{
			lock (popuplock)
			{
				Notification wnd = null;
				wnd = new Notification ();
				wnd.MessageTitle.Text = nin?.Title;
				wnd.MessageContent.Text = nin?.Content;
				wnd.Icon.Source = wnd.IconRef.Source = icon ?? wnd.Icon.Source;
				wnd.UpdateTimeout (TimeSpan.FromSeconds ((nin?.Timeout ?? 30000) * 0.001));
				noticeQueue.Enqueue (wnd);
				if (noticeQueue.Count == 1) PopupNotificationQueue ();
				return wnd;
			}
		}
		public static Notification ShowNotification (string title, string content, int timeout = 300000, ImageSource icon = null)
			=> ShowNotification (new NotifyIconNotification {
				Timeout = timeout,
				Title = title,
				Content = content,
				Icon = System.Windows.Forms.ToolTipIcon.None
			}, icon);
		public static void ClearAllNotificationWnd ()
		{
			var newlist = noticeQueue.ToList ();
			noticeQueue.Clear ();
			foreach (var wnd in newlist)
			{
				try { wnd?.Close (); } catch { }
			}
		}
		private void OptionsButton_Click (object sender, RoutedEventArgs e)
		{
			try
			{
				if (OptionsButton.ContextMenu != null)
				{
					OptionsButton.ContextMenu.IsOpen = true;
				}
			}
			catch { }
		}
		private void ItemOpenCtrlRule_Click (object sender, RoutedEventArgs e)
		{
			try
			{
				Process.Start ("explorer.exe", "shell:::{05d7b0f4-2121-4eff-bf6b-ed3f69b894d9}");
			}
			catch
			{
				try
				{
					Process.Start ("rundll32.exe", "shell32.dll,Options_RunDLL 1");
				}
				catch { }
			}
		}

	}
	internal static class SoundHelper
	{
		[DllImport ("winmm.dll", SetLastError = true)]
		private static extern bool PlaySound (string pszSound, IntPtr hmod, uint fdwSound);
		private const uint SND_ASYNC = 0x0001;
		private const uint SND_FILENAME = 0x00020000;
		public static int PlayNotificationAndGetDuration ()
		{
			string path = GetRegistrySoundPath (@"AppEvents\Schemes\Apps\.Default\Notification.Default\.Current");
			if (string.IsNullOrEmpty (path))
			{
				path = GetRegistrySoundPath (@"AppEvents\Schemes\Apps\.Default\SystemNotification\.Current");
			}
			if (!string.IsNullOrEmpty (path))
			{
				path = Environment.ExpandEnvironmentVariables (path);
				int ms = GetWavDuration (path);
				PlaySound (path, IntPtr.Zero, SND_FILENAME | SND_ASYNC);
				return ms;
			}
			SystemSounds.Asterisk.Play ();
			return 0;
		}
		private static string GetRegistrySoundPath (string keyPath)
		{
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey (keyPath))
				{
					return key?.GetValue (null) as string;
				}
			}
			catch
			{
				return null;
			}
		}
		private static int GetWavDuration (string filePath)
		{
			try
			{
				using (var stream = new FileStream (filePath, FileMode.Open, FileAccess.Read))
				using (var reader = new BinaryReader (stream))
				{
					if (new string (reader.ReadChars (4)) != "RIFF") return 0;
					reader.ReadInt32 ();
					if (new string (reader.ReadChars (4)) != "WAVE") return 0;
					int byteRate = 0;
					int dataSize = 0;
					while (stream.Position < stream.Length - 8)
					{
						string chunkId = new string (reader.ReadChars (4));
						int chunkSize = reader.ReadInt32 ();
						if (chunkId == "fmt ")
						{
							reader.ReadBytes (8);
							byteRate = reader.ReadInt32 ();
							if (chunkSize > 12) reader.ReadBytes (chunkSize - 12);
						}
						else if (chunkId == "data")
						{
							dataSize = chunkSize;
							break;
						}
						else
						{
							reader.ReadBytes (chunkSize);
						}
					}
					return (byteRate > 0 && dataSize > 0) ? (int)(dataSize * 1000.0 / byteRate) : 0;
				}
			}
			catch
			{
				return 0;
			}
		}
	}
}
