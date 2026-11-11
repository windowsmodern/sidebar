using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sidebar;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace WindowsModern.FeedTile
{
	public enum FeedSource
	{
		All,
		Selected
	};
	public class TileOptions: INotifyPropertyChanged, IDisposable
	{
		public ITileConfig TileConfig { get; private set; }
		private FeedSource _srcType = FeedSource.All;
		private int _showCountLevel = 0;
		private ObservableCollection<string> _feedFiles;
		private bool _autoSizeTile = false;
		private int _showItemsWhenAutoSize = 2;
		private bool _useOwnUpdater = false;
		public event PropertyChangedEventHandler PropertyChanged;
		private void OnPropertyChanged (string propName)
		{
			PropertyChanged?.Invoke (null, new PropertyChangedEventArgs (propName));
		}
		public FeedSource SourceType
		{
			get { return _srcType; }
			set
			{
				var str = "all";
				switch (_srcType = value)
				{
					case FeedSource.All: str = "all"; break;
					case FeedSource.Selected: str = "selected"; break;
				}
				TileConfig.Ini ["Settings"] ["SourceType"] = str;
				OnPropertyChanged (nameof (SourceType));
			}
		}
		public int ShowCountLevel
		{
			get { return _showCountLevel; }
			set
			{
				_showCountLevel = EnsureRange (value, 0, 4);
				TileConfig.Ini ["Settings"] ["ShowCountLevel"] = _showCountLevel;
				OnPropertyChanged (nameof (ShowCountLevel));
				OnPropertyChanged (nameof (ShowCountLimit));
			}
		}
		public int ShowCountLimit => ((_showCountLevel + 1) % 20) * 20;
		public ObservableCollection<string> FeedList
		{
			get
			{
				if (_feedFiles == null)
				{
					InitList ();
					_feedFiles.CollectionChanged += OnListChanged;
				}
				return _feedFiles;
			}
		}
		public bool AutoSizeTile
		{
			get { return _autoSizeTile; }
			set
			{
				TileConfig.Ini ["Settings"] ["AutoSizeTile"] = _autoSizeTile = value;
				OnPropertyChanged (nameof (AutoSizeTile));
			}
		}
		public int ShowItemsCountWhenAutoSize
		{
			get { return _showItemsWhenAutoSize; }
			set
			{
				TileConfig.Ini ["Settings"] ["ShowItemsCountWhenAutoSize"] = _showItemsWhenAutoSize = EnsureRange (value, 1, 8);
				OnPropertyChanged (nameof (ShowItemsCountWhenAutoSize));
			}
		}
		public bool UseOwnUpdater
		{
			get { return _useOwnUpdater; }
			set { TileConfig.Ini ["Settings"] ["UseOwnUpdater"] = _useOwnUpdater = value; OnPropertyChanged (nameof (UseOwnUpdater)); }
		}
		private static T EnsureRange <T> (T value, T min, T max) where T : IComparable<T>
		{
			if (min.CompareTo (max) > 0)
			{
				T temp = min;
				min = max;
				max = temp;
			}
			if (value.CompareTo (min) < 0) return min;
			if (value.CompareTo (max) > 0) return max;
			return value;
		}
		private void InitList ()
		{
			var xmlConfig = TileConfig?.Xml;
			if (xmlConfig?.Global != null)
			{
				var list = xmlConfig.Global.Get<List<string>> ("FeedList", new List<string> ());
				_feedFiles = new ObservableCollection<string> (list ?? new List<string> ());
			}
			else
			{
				_feedFiles = new ObservableCollection<string> ();
			}
		}
		private void OnListChanged (object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			var xmlConfig = TileConfig?.Xml;
			if (xmlConfig?.Global != null)
			{
				var list = _feedFiles.ToList ();
				xmlConfig.Global ["FeedList"] = list;
			}
			OnPropertyChanged (nameof (FeedList));
		}
		private void InitValues ()
		{
			switch (TileConfig.Ini ["Settings"].GetKey ("SourceType").ReadString ("all") ?? "all")
			{
				case "All": case "all": case "ALL":
					_srcType = FeedSource.All; break;
				case "selected": case "Selected": case "SELECTED":
					_srcType = FeedSource.Selected; break;
			}
			_showCountLevel = TileConfig.Ini ["Settings"].GetKey ("ShowCountLevel").ReadInt (4);
			if (_showCountLevel < 0) _showCountLevel = 0;
			_showCountLevel = EnsureRange (_showCountLevel, 0, 4);
			_autoSizeTile = TileConfig.Ini ["Settings"].GetKey ("AutoSizeTile").ReadBool ();
			_showItemsWhenAutoSize =
				EnsureRange (TileConfig.Ini ["Settings"].GetKey ("ShowItemsCountWhenAutoSize").ReadInt (2), 1, 8);
			_useOwnUpdater = TileConfig.Ini ["Settings"].GetKey ("UseOwnUpdater").ReadBool ();
		}
		public void Dispose ()
		{
			if (_feedFiles != null)
				_feedFiles.CollectionChanged -= OnListChanged;
		}
		public TileOptions (ITileConfig itc) { TileConfig = itc; InitValues (); }
	}
}
