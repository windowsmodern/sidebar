using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using HtmlAgilityPack;
using Sidebar;

namespace WindowsModern.FeedTile
{
	/// <summary>
	/// FlyoutFeedItem.xaml 的交互逻辑
	/// </summary>
	public partial class FlyoutFeedItem: UserControl
	{
		public FlyoutFeedItem ()
		{
			InitializeComponent ();
		}
		private FeedItemData feedItem = null;
		public void SetFeedItem (FeedItemData fid)
		{
			feedItem = fid;
		}
		public void RefreshFeed ()
		{
			Title.Text = feedItem?.Title ?? "";
			Desc.Text = HtmlToPlainText (feedItem?.Description ?? "");
			this.ToolTip = feedItem?.Title ?? "";
			if (!string.IsNullOrWhiteSpace (feedItem?.Parent?.Title))
			{
				this.ToolTip += "\n" + feedItem.Parent.Title;
			}
			if (feedItem != null)
			{
				this.ToolTip += "\n" + feedItem.PublishDate.ToString ();
			}
		}
		public static string HtmlToPlainText (string html)
		{
			if (string.IsNullOrWhiteSpace (html)) return string.Empty;
			try
			{
				var doc = new HtmlDocument ();
				doc.LoadHtml (html);
				var nodesToRemove = doc.DocumentNode.SelectNodes ("//script|//style");
				if (nodesToRemove != null)
				{
					foreach (var node in nodesToRemove)
						node.Remove ();
				}
				string text = doc.DocumentNode.InnerText?.Replace ("\r\n", " ")?.Replace ("\n", " ")?.Replace ("\r", " ");
				//text = Regex.Replace (text, @"\s+", " ");
				//text = System.Net.WebUtility.HtmlDecode (text);
				return text.Trim ();
			}
			catch (Exception ex)
			{
				//System.Diagnostics.Debug.WriteLine ($"Html conversion error: {ex.Message}");
				return string.Empty;
			}
		}
		private void FeedItemContainer_MouseEnter (object sender, MouseEventArgs e)
		{
			Title.SetResourceReference (StyleProperty, "TileFeedItemTitleHover");
			Desc.SetResourceReference (StyleProperty, "TileFeedItemDescriptionHover");
		}
		private void FeedItemContainer_MouseLeave (object sender, MouseEventArgs e)
		{
			Title.SetResourceReference (StyleProperty, "TileFeedItemTitle");
			Desc.SetResourceReference (StyleProperty, "TileFeedItemDescription");
		}
		public static FeedArticlePanel DetailPanel { get; set; }
		private void RequireShowDetailPage ()
		{
			var currData = feedItem;
			if (currData == null) return;
			if (disabledClick) return;
			if (currData != null && !currData.IsRead)
			{
				currData.IsRead = true;
				if (currData.Parent != null && currData.Parent.UnreadCount > 0)
				{
					currData.Parent.UnreadCount--;
					Tile.MarkItemAsRead (currData.Parent?.Path, currData.LocalId);
				}
			}
			if (DetailPanel == null) DetailPanel = new FeedArticlePanel ();
			var page = DetailPanel;
			(page?.Parent as Panel)?.Children?.Clear ();
			page.SetFeedItem (currData);
			Tile.SidebarFeatures.Request (new SidebarRequest (Tile.TileInstance) {
				RequestName = "RequireExtraFlyout",
				TransferDatas = page
			});
		}
		private bool disabledClick = false;
		public bool DisableClick (bool ?value = null)
		{
			if (value != null) disabledClick = value ?? false;
			return disabledClick;
		}
		private void ClickContainer_Click (object sender, RoutedEventArgs e)
		{
			RequireShowDetailPage ();
		}
	}
}
