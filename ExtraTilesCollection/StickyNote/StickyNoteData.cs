using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Media;
using DrawingColor = System.Drawing.Color;
namespace WindowsModern.StickyNote
{
	public class StickyNoteContent: INotifyPropertyChanged, IDisposable
	{
		private ImageSource _screenshot = null;
		private FlowDocument _docwpf = null;
		private StrokeCollection _inkwpf = null;
		public ImageSource Screenshot
		{
			get { return _screenshot; }
			set { _screenshot = value; OnPropertyChanged (nameof (Screenshot)); }
		}
		public FlowDocument RickTextNote
		{
			get { return _docwpf; }
			set { _docwpf = value; OnPropertyChanged (nameof (RickTextNote)); }
		}
		public StrokeCollection Ink
		{
			get { return _inkwpf; }
			set { _inkwpf = value; OnPropertyChanged (nameof (Ink)); }
		}
		private HashSet<PropertyChangedEventHandler> changedHandlers = new HashSet<PropertyChangedEventHandler> ();
		private object changedEventLock = new object ();
		private void OnPropertyChanged (string propName)
		{
			var e = new PropertyChangedEventArgs (propName);
			lock (changedEventLock)
			{
				foreach (var h in changedHandlers)
					h?.Invoke (this, e);
			}
		}
		public void Dispose ()
		{
			changedHandlers?.Clear ();
		}
		public event PropertyChangedEventHandler PropertyChanged
		{
			add { changedHandlers?.Add (value); }
			remove { changedHandlers?.Remove (value); }
		}
	} 
	public class StickyNoteStoreData: INotifyPropertyChanged, IDisposable
	{
		private string _filePath = null;
		private StickyNoteContent _noteContent = null;
		private DateTime _dateTime = DateTime.UtcNow;
		private DrawingColor _background = DrawingColor.FromArgb (0xFB, 0xF7, 0xCB);
		private bool _isFlat = false;
		public string FilePath
		{
			get { return _filePath; }
			set
			{
				_filePath = value;
				OnPropertyChanged (nameof (FilePath));
			}
		}
		public StickyNoteContent Content
		{
			get { return _noteContent; }
			set
			{
				if (_noteContent != null) _noteContent.PropertyChanged -= NoteContent_PropertyChanged;
				_noteContent = value;
				if (_noteContent != null) _noteContent.PropertyChanged += NoteContent_PropertyChanged;
				OnPropertyChanged (nameof (Content));
			}
		}
		private void NoteContent_PropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			OnPropertyChanged (nameof (Content));
		}
		public DateTime NoteDateTime
		{
			get { return _dateTime; }
			set
			{
				_dateTime = value;
				OnPropertyChanged (nameof (DateTime));
			}
		}
		public DrawingColor BackgroundColor
		{
			get { return _background; }
			set
			{
				_background = value;
				OnPropertyChanged (nameof (BackgroundColor));
			}
		}
		public bool IsFlat
		{
			get { return _isFlat; }
			set
			{
				_isFlat = value;
				OnPropertyChanged (nameof (IsFlat));
			}
		}
		private HashSet<PropertyChangedEventHandler> changedHandlers = new HashSet<PropertyChangedEventHandler> ();
		private object changedEventLock = new object ();
		private void OnPropertyChanged (string propName)
		{
			var e = new PropertyChangedEventArgs (propName);
			lock (changedEventLock)
			{
				foreach (var h in changedHandlers)
					h?.Invoke (this, e);
			}
		}
		public void Dispose ()
		{
			changedHandlers?.Clear ();
		}
		public event PropertyChangedEventHandler PropertyChanged
		{
			add { changedHandlers?.Add (value); }
			remove { changedHandlers?.Remove (value); }
		}
	}
}
