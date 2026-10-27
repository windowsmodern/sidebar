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

namespace ClockTile
{
	/// <summary>
	/// SidebarTileContainer.xaml 的交互逻辑
	/// </summary>
	public partial class SidebarTileContainer: UserControl
	{
		public SidebarTileContainer ()
		{
			InitializeComponent ();
		}
		public Panel Container => TileContent;
		public static readonly DependencyProperty ContainerContentProperty =
			   DependencyProperty.Register ("ContainerContent", typeof (UIElement), typeof (SidebarTileContainer),
				   new PropertyMetadata (null, OnContainerContentChanged));
		public UIElement ContainerContent
		{
			get { return (UIElement)GetValue (ContainerContentProperty); }
			set { SetValue (ContainerContentProperty, value); }
		}
		private static void OnContainerContentChanged (DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var control = d as SidebarTileContainer;
			var newContent = e.NewValue as UIElement;
			var fe = newContent as FrameworkElement;
			(fe?.Parent as Panel)?.Children?.Remove (newContent);
			control?.SetContainerContent (newContent);
		}
		private void SetContainerContent (UIElement content)
		{
			var fe = content as FrameworkElement;
			(fe?.Parent as Panel)?.Children?.Remove (content);
			TileContent.Children.Clear ();
			if (content != null)
				TileContent.Children.Add (content);
		}
	}
}
