using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Media;
using WindowsModern.StickyNote;
namespace WindowsModern.StickyNote
{
	public class NoteContentPanel: Grid
	{
		public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty = DependencyProperty.Register (nameof (HorizontalScrollBarVisibility), typeof (ScrollBarVisibility), typeof (NoteContentPanel), new PropertyMetadata (ScrollBarVisibility.Disabled, OnScrollBarVisibilityChanged));
		public static readonly DependencyProperty VerticalScrollBarVisibilityProperty = DependencyProperty.Register (nameof (VerticalScrollBarVisibility), typeof (ScrollBarVisibility), typeof (NoteContentPanel), new PropertyMetadata (ScrollBarVisibility.Auto, OnScrollBarVisibilityChanged));
		private readonly Image ScreenshotContent;
		private readonly RichTextBox TextContent;
		private readonly InkCanvas InkContent;
		private StickyNoteContent stickyNoteContent;
		private NoteContentMode contentMode;
		private bool isSyncing;
		private bool isInitializing = true;
		public NoteContentPanel ()
		{
			ScreenshotContent = new Image { Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, IsHitTestVisible = false };
			InkContent = new InkCanvas { Background = Brushes.Transparent, EditingMode = InkCanvasEditingMode.Ink };
			TextContent = new RichTextBox {
				Background = Brushes.Transparent,
				BorderThickness = new Thickness (0),
				AcceptsReturn = true,
				AcceptsTab = true,
				Document = CreateDefaultDocument ()
			};
			TextContent.HorizontalScrollBarVisibility = HorizontalScrollBarVisibility;
			TextContent.VerticalScrollBarVisibility = VerticalScrollBarVisibility;
			Children.Add (ScreenshotContent);
			Children.Add (InkContent);
			Children.Add (TextContent);
			TextContent.TextChanged += TextContent_TextChanged;
			InkContent.StrokeCollected += InkContent_StrokeCollected;
			ContentMode = NoteContentMode.Text;
			isInitializing = false;
		}
		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get { return (ScrollBarVisibility)GetValue (HorizontalScrollBarVisibilityProperty); }
			set { SetValue (HorizontalScrollBarVisibilityProperty, value); }
		}
		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get { return (ScrollBarVisibility)GetValue (VerticalScrollBarVisibilityProperty); }
			set { SetValue (VerticalScrollBarVisibilityProperty, value); }
		}
		public event EventHandler ContentChanged;
		public StickyNoteContent ContentData
		{
			get { return stickyNoteContent; }
			set
			{
				if (ReferenceEquals (stickyNoteContent, value)) return;
				if (stickyNoteContent != null) stickyNoteContent.PropertyChanged -= StickyNoteContent_PropertyChanged;
				stickyNoteContent = value;
				if (stickyNoteContent != null) stickyNoteContent.PropertyChanged += StickyNoteContent_PropertyChanged;
				SyncFromContentData ();
			}
		}
		public object Content { get; set; }
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
		private static void OnScrollBarVisibilityChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var panel = d as NoteContentPanel;
			if (panel == null) return;
			if (e.Property == HorizontalScrollBarVisibilityProperty)
			{
				panel.TextContent.HorizontalScrollBarVisibility = (ScrollBarVisibility)e.NewValue;
			}
			else if (e.Property == VerticalScrollBarVisibilityProperty)
			{
				panel.TextContent.VerticalScrollBarVisibility = (ScrollBarVisibility)e.NewValue;
			}
		}
		private FlowDocument CreateDefaultDocument ()
		{
			var doc = new FlowDocument ();
			var docstyle = new Style (typeof (Paragraph));
			docstyle.Setters.Add (new Setter (Block.MarginProperty, new Thickness (0)));
			doc.Resources.Add (typeof (Paragraph), docstyle);
			return doc;
		}
		private void SyncFromContentData ()
		{
			isSyncing = true;
			try
			{
				if (stickyNoteContent == null)
				{
					ScreenshotContent.Source = null;
					TextContent.Document = CreateDefaultDocument ();
					SetInk (new StrokeCollection ());
				}
				else
				{
					ScreenshotContent.Source = stickyNoteContent.Screenshot;
					if (!ReferenceEquals (TextContent.Document, stickyNoteContent.RickTextNote))
					{
						var doc = stickyNoteContent.RickTextNote ?? CreateDefaultDocument ();
						if (stickyNoteContent.RickTextNote == null) stickyNoteContent.RickTextNote = doc;
						TextContent.Document = doc;
					}
					if (stickyNoteContent.Ink == null)
					{
						var ink = new StrokeCollection ();
						stickyNoteContent.Ink = ink;
						SetInk (ink);
					}
					else if (!ReferenceEquals (InkContent.Strokes, stickyNoteContent.Ink))
					{
						SetInk (stickyNoteContent.Ink);
					}
				}
			}
			finally
			{
				isSyncing = false;
			}
		}
		private void SetInk (StrokeCollection ink)
		{
			if (ink == null) ink = new StrokeCollection ();
			if (ReferenceEquals (InkContent.Strokes, ink)) return;
			InkContent.Strokes = ink;
		}
		private StickyNoteContent EnsureContentData ()
		{
			if (stickyNoteContent == null)
			{
				stickyNoteContent = new StickyNoteContent ();
				stickyNoteContent.PropertyChanged += StickyNoteContent_PropertyChanged;
			}
			return stickyNoteContent;
		}
		private void TextContent_TextChanged (object sender, TextChangedEventArgs e)
		{
			if (isInitializing || isSyncing) return;
			var data = EnsureContentData ();
			isSyncing = true;
			try
			{
				data.RickTextNote = TextContent.Document;
				if (data.Ink == null) data.Ink = InkContent.Strokes;
			}
			finally
			{
				isSyncing = false;
			}
			OnContentChanged ();
		}
		private void InkContent_StrokeCollected (object sender, InkCanvasStrokeCollectedEventArgs e)
		{
			if (isInitializing || isSyncing) return;
			var data = EnsureContentData ();
			isSyncing = true;
			try
			{
				data.Ink = InkContent.Strokes;
				if (data.RickTextNote == null) data.RickTextNote = TextContent.Document;
			}
			finally
			{
				isSyncing = false;
			}
			OnContentChanged ();
		}
		private void StickyNoteContent_PropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (isInitializing || isSyncing) return;
			isSyncing = true;
			try
			{
				if (e.PropertyName == nameof (StickyNoteContent.Screenshot))
				{
					ScreenshotContent.Source = stickyNoteContent.Screenshot;
				}
				else if (e.PropertyName == nameof (StickyNoteContent.RickTextNote))
				{
					if (!ReferenceEquals (TextContent.Document, stickyNoteContent.RickTextNote))
						TextContent.Document = stickyNoteContent.RickTextNote ?? CreateDefaultDocument ();
				}
				else if (e.PropertyName == nameof (StickyNoteContent.Ink))
				{
					if (!ReferenceEquals (InkContent.Strokes, stickyNoteContent.Ink))
						SetInk (stickyNoteContent.Ink ?? new StrokeCollection ());
				}
			}
			finally
			{
				isSyncing = false;
			}
		}
		private void OnContentChanged ()
		{
			ContentChanged?.Invoke (this, EventArgs.Empty);
		}
	}
	public enum NoteContentMode
	{
		Text,
		Ink
	}
}