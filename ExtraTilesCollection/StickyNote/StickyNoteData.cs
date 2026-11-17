using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Media;

namespace WindowsModern.StickyNote
{
	public class StickyNoteContent
	{
		public ImageSource Screenshot { get; set; }
		public FlowDocument RickTextNote { get; set; }
		public StrokeCollection Ink { get; set; }
	} 
	public class StickyNoteStoreData
	{
		public string FilePath { get; set; }
		public ImageSource Screenshot { get; set; }
		public StickyNoteContent Content { get; set; }
		public DateTime NoteDateTime { get; set; }
		public System.Drawing.Color BackgroundColor { get; set; }
	}
}
