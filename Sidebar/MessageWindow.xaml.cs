using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Sidebar
{
	/// <summary>
	/// MessageWindow.xaml 的交互逻辑
	/// </summary>
	public partial class MessageWindow: Window
	{
		public MessageWindow ()
		{
			InitializeComponent ();
		}
		public static MessageWindow ShowException (Exception e, string title = "", string msgtitle = null, bool showdialog = false)
		{
			var msgwnd = new MessageWindow ();
			msgwnd.Title = title;
			msgwnd.MessageTitle.Text = msgtitle ?? e.GetType ().ToString ();
			msgwnd.MessageDescription.Text = e.Message;
			if (string.IsNullOrWhiteSpace (msgwnd.MessageDescription.Text))
				msgwnd.MessageDescription.Visibility = Visibility.Collapsed;
			msgwnd.MessageDetail.Text = GetDetailException (e);
			if (showdialog) msgwnd.ShowDialog ();
			else msgwnd.Show ();
			SystemSounds.Hand.Play ();
			return msgwnd;
		}
		private static string GetDetailException (Exception e, int depth = 0)
		{
			if (e == null) return string.Empty;
			var sb = new StringBuilder ();
			sb.Append (GetFormatException (e, depth));
			if (e.InnerException != null)
			{
				string blank = new string ('\t', depth);
				sb.AppendLine ($"{blank}    InnerException:");
				sb.Append (GetDetailException (e.InnerException, depth + 1));
			}
			return sb.ToString ();
		}
		private static string GetFormatException (Exception e, int blankcnt = 0)
		{
			string blank = "";
			for (var i = 0; i < blankcnt; i++)
			{
				blank += "\t";
			}
			var format = App.ProgramFolder.StringResources.SuitableResource ("SIDEBAR_MESSAGE_ERROR_EXCEPTIONFORMAT");
			if (string.IsNullOrEmpty (format))
			{
				format = "{0}{1}\n{0}    Message: {2}\n{0}    Source: {3}\n{0}    Target Site: {4}\n{0}    Help Link: {5}\n{0}    Stack Trace: \n{0}    ====================\n{6}\n{0}    ====================\n\n";
			}
			return string.Format (
				format,
				blank,
				e.GetType ().ToString (),
				e.Message,
				e.Source,
				e.TargetSite,
				e.HelpLink,
				e.StackTrace);
		}
		private void OkButton_Click (object sender, RoutedEventArgs e)
		{
			try { Close (); } catch { }
		}
		private void CopyInfoButton_Click (object sender, RoutedEventArgs e)
		{
			try
			{
				if (string.IsNullOrWhiteSpace (MessageDetail.Text)) return;
				Clipboard.SetText (MessageDetail.Text ?? "");
			}
			catch { }
		}
	}
}
