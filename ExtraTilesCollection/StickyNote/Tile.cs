using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Sidebar;

namespace WindowsModern.StickyNote
{
	public class Tile: TileBaseEx
	{
		private TilePanel tilePanel = null;
		public override void Initialize ()
		{
			ContentArea.Children.Add (tilePanel = new TilePanel ());
		}
		public override void Destroy ()
		{
			(tilePanel?.Parent as Panel)?.Children?.Clear ();
		}
	}
}
