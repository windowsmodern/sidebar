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

namespace WindowsModern.StickyNote
{
	/// <summary>
	/// NoteContent.xaml 的交互逻辑
	/// </summary>
	public partial class NoteContent: UserControl
	{
		public NoteContent ()
		{
			InitializeComponent ();
			ContentMode = NoteContentMode.Text;
		}
		public event EventHandler ContentChanged;
		private void OnContentChanged () { ContentChanged?.Invoke (this, new EventArgs ()); }
		private StickyNoteContent stickyNoteContent;
		public StickyNoteContent ContentData
		{
			get { return stickyNoteContent; }
			set
			{
				stickyNoteContent = value;
				ScreenshotContent.Source = stickyNoteContent?.Screenshot;
				TextContent.Document = stickyNoteContent?.RickTextNote ?? new FlowDocument ();
				InkContent.Strokes = stickyNoteContent?.Ink ?? new System.Windows.Ink.StrokeCollection ();
			}
		}
		private new object Content { get; set; }
		private NoteContentMode contentMode;
		public NoteContentMode ContentMode
		{
			get { return contentMode; }
			set
			{
				contentMode = value;
				InkContent.IsHitTestVisible = contentMode == NoteContentMode.Ink;
				TextContent.IsHitTestVisible = contentMode == NoteContentMode.Text;
			}
		}
		private void TextContent_TextChanged (object sender, TextChangedEventArgs e)
		{
			if (stickyNoteContent == null) stickyNoteContent = new StickyNoteContent ();
			stickyNoteContent.RickTextNote = TextContent.Document;
			OnContentChanged ();
		}
		private void InkContent_StrokeCollected (object sender, InkCanvasStrokeCollectedEventArgs e)
		{
			if (stickyNoteContent == null) stickyNoteContent = new StickyNoteContent ();
			stickyNoteContent.Ink = InkContent.Strokes;
			OnContentChanged ();
		}
	}
	public enum NoteContentMode
	{
		Text,
		Ink
	}
}
