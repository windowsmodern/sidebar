using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Sidebar
{
	public interface IFlyoutToolMembers
	{
		System.Drawing.Rectangle TileRect { get; }
		System.Drawing.Size PixelSize { get; }
		void FixPosition ();
		Task TransToNewHeight (FrameworkElement component, double elderHeight, double? newHeight = null, TimeSpan? timeout = null);
		ContextMenu FlyoutContextMenu { get; set; }
	}
	public interface IPropertiesToolMembers
	{
		Button OKButton { get; }
		Button CancelButton { get; }
	}
}
