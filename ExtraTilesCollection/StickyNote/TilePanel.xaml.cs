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
using System.Windows.Threading;

namespace WindowsModern.StickyNote
{
	/// <summary>
	/// TilePanel.xaml 的交互逻辑
	/// </summary>
	public partial class TilePanel: UserControl
	{
		public TilePanel ()
		{
			InitializeComponent ();
		}
		private void UserControl_Loaded (object sender, RoutedEventArgs e)
		{
			
		}
		private void UserControl_Unloaded (object sender, RoutedEventArgs e)
		{
			
		}
		private void ControlContentContainer_SizeChanged (object sender, SizeChangedEventArgs e)
		{
			var actualHeight = ControlContentContainer.ActualHeight;
			double top = NewButton.Margin.Top + NewButton.ActualHeight + NewButton.Margin.Bottom;
			var mergeTop = top + SnipButton.ActualHeight + SnipButton.Margin.Top + SnipButton.Margin.Bottom + FolderButtonHidden.Margin.Top + FolderButtonHidden.Margin.Bottom + FolderButtonHidden.ActualHeight;
			FolderButton.Visibility = actualHeight >= mergeTop ? Visibility.Visible : Visibility.Collapsed;
		}
		private void NewButton_OptionsButtonClick (object sender, RoutedEventArgs e)
		{
			SnipButtonPopup.IsOpen = false;
			NewButtonPopup.IsOpen = true;
		}
		private void SnipButton_OptionsButtonClick (object sender, RoutedEventArgs e)
		{
			NewButtonPopup.IsOpen = false;
			SnipButtonPopup.IsOpen = true;
		}
		private void NewButton_CommandButtonClick (object sender, RoutedEventArgs e)
		{
			var snform = new StickyNoteForm ();
			snform.Show ();
		}
		private void SnipButton_CommandButtonClick (object sender, RoutedEventArgs e)
		{

		}
	}
}
