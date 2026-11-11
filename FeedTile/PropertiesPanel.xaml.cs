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

namespace WindowsModern.FeedTile
{
	/// <summary>
	/// PropertiesPanel.xaml 的交互逻辑
	/// </summary>
	public partial class PropertiesPanel: UserControl
	{
		private DispatcherTimer _refreshStatusTimer;
		public PropertiesPanel ()
		{
			InitializeComponent ();
			InitStrings ();
			SetFeedSourceSelectedItems (Tile.Options.FeedList);
			SelectShowItemsCount.ItemsSource = new int [] { 20, 40, 60, 80, 100 };
			SelectShowItemsWhenAutoSize.ItemsSource = new int [] { 1, 2, 3, 4, 5, 6, 7, 8 };

		}
		private void InitStrings ()
		{
			var sr = Tile.TileFolder.StringResources;
			FeedSourceGroup.Header = sr.SuitableResource ("OPTIONS_FEEDSRC");
			InputUseAll.Content = sr.SuitableResource ("OPTIONS_FEEDSRC_ALL");
			InputUseSelected.Content = sr.SuitableResource ("OPTIONS_FEEDSRC_SELECTED");
			LabelShowItemsCount.Text = sr.SuitableResource ("OPTIONS_SHOWITEMS");
			InputAutoSize.Content = sr.SuitableResource ("OPTIONS_AUTOSIZE");
			LabelShowItemsWhenAutoSize.Text = sr.SuitableResource ("OPTIONS_TITLELIMIT");
			ListFeedSources.ItemsSource = Tile.Feeds;
			ListFeedSources.SelectionMode = SelectionMode.Extended;
			RefreshButton.Content = sr.SuitableResource ("OPTIONS_REFRESH");
			UpdateButton.Content = sr.SuitableResource ("OPTIONS_NETREFRESH");
			InputUseOwnUpdater.Content = sr.SuitableResource ("OPTIONS_USEOWNUPDATER");
		}
		private void InitValues ()
		{
			var opt = Tile.Options;
			InputUseAll.IsChecked = opt.SourceType == FeedSource.All;
			InputUseSelected.IsChecked = opt.SourceType == FeedSource.Selected;
			SelectShowItemsCount.SelectedItem = opt.ShowCountLimit;
			InputAutoSize.IsChecked = opt.AutoSizeTile;
			SelectShowItemsWhenAutoSize.SelectedItem = opt.ShowItemsCountWhenAutoSize;
			InputUseOwnUpdater.IsChecked = opt.UseOwnUpdater;
		}
		private void SetFeedSourceSelectedItems (IEnumerable <string> paths)
		{
			if (ListFeedSources.ItemsSource == null) return;
			var pathSet = new HashSet<string> (paths ?? Enumerable.Empty<string> ());
			ListFeedSources.SelectedItems.Clear ();
			foreach (FeedData feed in ListFeedSources.ItemsSource)
			{
				if (pathSet.Contains (feed.Path))
				{
					ListFeedSources.SelectedItems.Add (feed);
				}
			}
		}
		private IEnumerable <string> GetFeedSourceSelectedItems ()
		{
			return ListFeedSources.SelectedItems
				.Cast<FeedData> ()
				.Select (feed => feed.Path)
				.ToList ();
		}
		public void OnSave ()
		{
			var list = GetFeedSourceSelectedItems ();
			if (InputUseSelected.IsChecked == true && list.Count () < 1 && Tile.Feeds.Count >= 1)
			{
				throw new Exception (Tile.TileFolder.StringResources.SuitableResource ("ERROR_MOREITEMS"));
			}
			if (
				(InputUseAll.IsChecked ?? false) == (InputUseSelected.IsChecked ?? false) ||
				SelectShowItemsCount.SelectedItem == null ||
				SelectShowItemsWhenAutoSize.SelectedItem == null
				)
			{
				throw new InvalidOperationException (Tile.TileFolder.StringResources.SuitableResource ("ERROR_VALUES"));
			}
			var opt = Tile.Options;
			FeedSource fs;
			if (InputUseAll.IsChecked ?? false) fs = FeedSource.All;
			else fs = FeedSource.Selected;
			if (opt.SourceType != fs) opt.SourceType = fs;
			var newPaths = list.ToList ();
			opt.FeedList.Clear ();
			foreach (var path in newPaths) opt.FeedList.Add (path);
			var scl = (int)((int)SelectShowItemsCount.SelectedItem * 0.05 - 1);
			if (opt.ShowCountLevel != scl) opt.ShowCountLevel = scl;
			if (opt.AutoSizeTile != InputAutoSize.IsChecked) opt.AutoSizeTile = InputAutoSize.IsChecked ?? false;
			if (opt.ShowItemsCountWhenAutoSize != (int)SelectShowItemsWhenAutoSize.SelectedItem)
				opt.ShowItemsCountWhenAutoSize = (int)SelectShowItemsWhenAutoSize.SelectedItem;
			if (opt.UseOwnUpdater != (InputUseOwnUpdater.IsChecked ?? false))
				opt.UseOwnUpdater = InputUseOwnUpdater.IsChecked ?? false;
		}
		private void UserControl_Loaded (object sender, RoutedEventArgs e)
		{
			InitValues ();
			if (_refreshStatusTimer == null)
			{
				_refreshStatusTimer = new DispatcherTimer {
					Interval = TimeSpan.FromSeconds (1)
				};
				_refreshStatusTimer.Tick += RefreshStatusTimer_Tick;
			}
			_refreshStatusTimer.Start ();
			UpdateRefreshButtonState ();
		}
		private void UserControl_Unloaded (object sender, RoutedEventArgs e)
		{
			_refreshStatusTimer?.Stop ();
			_refreshStatusTimer = null;
		}
		private void RefreshStatusTimer_Tick (object sender, EventArgs e)
		{
			UpdateRefreshButtonState ();
		}
		private void UpdateRefreshButtonState ()
		{
			bool disabled = Tile.IsUpdating || Tile.IsRefreshing;
			RefreshButton.IsEnabled = !disabled;
			UpdateButton.IsEnabled = !disabled;
		}
		private void RefreshButton_Click (object sender, RoutedEventArgs e)
		{
			UpdateRefreshButtonState ();
			var tile = Tile.TileInstance as Tile;
			tile?.RefreshFeeds ();
		}
		private void UpdateButton_Click (object sender, RoutedEventArgs e)
		{
			UpdateRefreshButtonState ();
			Tile.UpdateFeedsAsync ();
		}
	}
}
