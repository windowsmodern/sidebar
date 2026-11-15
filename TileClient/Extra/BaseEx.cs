using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sidebar
{
	/// <summary>
	/// An extended tile base class that wraps the events of <see cref="TileBase"/> into overridable virtual methods and hides direct event subscription.
	/// <para xml:lang="zh-Hans">扩展的磁贴基类，将 <see cref="TileBase"/> 的事件封装为可重写的虚方法，并隐藏直接的事件订阅。</para>
	/// <para xml:lang="zh-Hant">擴充的磁貼基底類別，將 <see cref="TileBase"/> 的事件封裝為可覆寫的虛擬方法，並隱藏直接的事件訂閱。</para>
	/// </summary>
	[ComVisible (true)]
	public abstract class TileBaseEx: TileBase
	{
		/// <summary>
		/// Called when the tile is unloaded. Do not clean up UI elements here, as this event may also be triggered when dragging the tile.
		/// <para xml:lang="zh-Hans">磁贴控件触发 Unloaded 事件。请勿在此处清理 UI 元素，因为拖拽磁贴时也可能会触发此事件。</para>
		/// <para xml:lang="zh-Hant">磁貼控制項觸發 Unloaded 事件。請勿在此處清理 UI 元素，因為拖曳磁貼時也可能會觸發此事件。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event data. (zh-Hans: 包含事件数据的对象。 zh-Hant: 包含事件資料的物件。)</param>
		public virtual void Tile_Unloaded (object sender, EventArgs e) { }
		/// <summary>
		/// Called when the tile is loaded. Initialization is not recommended here, as this event may also be triggered when dragging the tile.
		/// <para xml:lang="zh-Hans">磁贴载入时调用。不建议在此处进行初始化操作，因为拖拽磁贴时也可能会触发此事件。</para>
		/// <para xml:lang="zh-Hant">當磁貼載入時調用。不建議在此處進行初始化操作，因為拖曳磁貼時也可能會觸發此事件。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event data. (zh-Hans: 包含事件数据的对象。 zh-Hant: 包含事件資料的物件。)</param>
		public virtual void Tile_Loaded (object sender, EventArgs e) { }
		/// <summary>
		/// Called when the theme of the Sidebar changes. Adjust the tile display according to this event to adapt to the new theme.
		/// <para xml:lang="zh-Hans">当边栏的主题改变时调用。可根据此事件调整磁贴的显示以适应新主题。</para>
		/// <para xml:lang="zh-Hant">當邊欄的主題改變時調用。可根據此事件調整磁貼的顯示以適應新主題。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing the theme name and theme object. (zh-Hans: 包含主题名称和主题对象的事件参数。 zh-Hant: 包含主題名稱和主題物件的事件參數。)</param>
		public virtual void Sidebar_ThemeChanged (object sender, ThemeChangedEventArgs e) { }
		/// <summary>
		/// Called when the position (left or right) of the Sidebar changes. Adjust the tile display according to this event.
		/// <para xml:lang="zh-Hans">当边栏的位置（靠左或靠右）改变时调用。可根据此事件调整磁贴的显示。</para>
		/// <para xml:lang="zh-Hant">當邊欄的位置（靠左或靠右）改變時調用。可根據此事件調整磁貼的顯示。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing the new Sidebar direction. (zh-Hans: 包含新的边栏方向的事件参数。 zh-Hant: 包含新的邊欄方向的事件參數。)</param>
		public virtual void Sidebar_DirectionChanged (object sender, SidebarDirectionChangedEventArgs e) { }
		/// <summary>
		/// Called when the tile size changes, including changes to the overall tile size and the content area size.
		/// <para xml:lang="zh-Hans">当磁贴尺寸改变时调用，包含磁贴整体尺寸和内容区尺寸的变化。</para>
		/// <para xml:lang="zh-Hant">當磁貼尺寸改變時調用，包含磁貼整體尺寸和內容區尺寸的變化。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing pixel sizes and WPF sizes. (zh-Hans: 包含像素尺寸和 WPF 尺寸的事件参数。 zh-Hant: 包含像素尺寸和 WPF 尺寸的事件參數。)</param>
		public virtual void Tile_SizeChanged (object sender, SizeChangedEventArgs e) { }
		/// <summary>
		/// Called when the properties window has closed. Cleanup can be done here.
		/// <para xml:lang="zh-Hans">当属性窗口已关闭时调用。可在此处进行清理工作。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗已關閉時調用。可在此處進行清理工作。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event data. (zh-Hans: 包含事件数据的对象。 zh-Hant: 包含事件資料的物件。)</param>
		public virtual void Properties_Closed (object sender, EventArgs e) { }
		/// <summary>
		/// Called when the properties window is about to close. The sender object from the window's Closing event is passed.
		/// <para xml:lang="zh-Hans">当属性窗口即将关闭时调用。传递了窗口 Closing 事件的 sender 对象。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗即將關閉時調用。傳遞了視窗 Closing 事件的 sender 物件。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing the properties window and content panel. (zh-Hans: 包含属性窗口和内容面板的事件参数。 zh-Hant: 包含屬性視窗和內容面板的事件參數。)</param>
		public virtual void Properties_Closing (object sender, PropertiesAboutEventArgs e) { }
		/// <summary>
		/// Called when the "Cancel" button of the properties window is clicked. The window closes automatically after pressing the button; throw an exception to prevent it.
		/// <para xml:lang="zh-Hans">当属性窗口的“取消”按钮被点击时调用。该按钮按下后会自动关闭窗口，若想阻止关闭，可抛出异常。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗的「取消」按鈕被點擊時調用。該按鈕按下後會自動關閉視窗，若想阻止關閉，可拋出異常。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing the properties window and content panel. (zh-Hans: 包含属性窗口和内容面板的事件参数。 zh-Hant: 包含屬性視窗和內容面板的事件參數。)</param>
		public virtual void Properties_ClickCancel (object sender, PropertiesAboutEventArgs e) { }
		/// <summary>
		/// Called when the "OK" button of the properties window is clicked. Modified configuration items can be checked and saved here. The window closes automatically after pressing the button; throw an exception to prevent it.
		/// <para xml:lang="zh-Hans">当属性窗口的“确认”按钮被点击时调用。可在此处检查和保存修改后的配置项。该按钮按下后会自动关闭窗口，若想阻止关闭，可抛出异常。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗的「確認」按鈕被點擊時調用。可在此處檢查和保存修改後的配置項。該按鈕按下後會自動關閉視窗，若想阻止關閉，可拋出異常。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing the properties window and content panel. (zh-Hans: 包含属性窗口和内容面板的事件参数。 zh-Hant: 包含屬性視窗和內容面板的事件參數。)</param>
		public virtual void Properties_ClickOK (object sender, PropertiesAboutEventArgs e) { }
		/// <summary>
		/// Called when the properties window is loaded. Effectively the Loaded event of the properties window.
		/// <para xml:lang="zh-Hans">当属性窗口加载完成时调用。实际上等同于属性窗口的 Loaded 事件。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗載入完成時調用。實際上等同於屬性視窗的 Loaded 事件。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing the properties window and content panel. (zh-Hans: 包含属性窗口和内容面板的事件参数。 zh-Hant: 包含屬性視窗和內容面板的事件參數。)</param>
		public virtual void Properties_Loaded (object sender, PropertiesAboutEventArgs e) { }
		/// <summary>
		/// Called when the user opens the tile properties window. Initialization of the properties window can be done here.
		/// <para xml:lang="zh-Hans">当用户打开磁贴属性窗口时调用。可在此处完成属性窗口的初始化操作。</para>
		/// <para xml:lang="zh-Hant">當用戶打開磁貼屬性視窗時調用。可在此處完成屬性視窗的初始化操作。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing the properties window and content panel. (zh-Hans: 包含属性窗口和内容面板的事件参数。 zh-Hant: 包含屬性視窗和內容面板的事件參數。)</param>
		public virtual void Properties_Initialize (object sender, PropertiesAboutEventArgs e) { }
		/// <summary>
		/// Called when the flyout has closed. Cleanup can be done here.
		/// <para xml:lang="zh-Hans">当浮出窗口已关闭时调用。可在此处进行清理工作。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗已關閉時調用。可在此處進行清理工作。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event data. (zh-Hans: 包含事件数据的对象。 zh-Hant: 包含事件資料的物件。)</param>
		public virtual void Flyout_Closed (object sender, EventArgs e) { }
		/// <summary>
		/// Called when the flyout is about to close. The sender object from the window's Closing event is passed.
		/// <para xml:lang="zh-Hans">当浮出窗口即将关闭时调用。传递了窗口 Closing 事件的 sender 对象。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗即將關閉時調用。傳遞了視窗 Closing 事件的 sender 物件。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing the flyout window and content panel. (zh-Hans: 包含浮出窗口和内容面板的事件参数。 zh-Hant: 包含浮出視窗和內容面板的事件參數。)</param>
		public virtual void Flyout_Closing (object sender, FlyoutAboutEventArgs e) { }
		/// <summary>
		/// Called when the flyout size changes.
		/// <para xml:lang="zh-Hans">当浮出窗口尺寸改变时调用。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗尺寸改變時調用。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing pixel sizes and WPF sizes. (zh-Hans: 包含像素尺寸和 WPF 尺寸的事件参数。 zh-Hant: 包含像素尺寸和 WPF 尺寸的事件參數。)</param>
		public virtual void Flyout_SizeChanged (object sender, SizeChangedEventArgs e) { }
		/// <summary>
		/// Called when the flyout is loaded. Effectively the Loaded event of the flyout.
		/// <para xml:lang="zh-Hans">当浮出窗口加载完成时调用。实际上等同于浮出窗口的 Loaded 事件。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗載入完成時調用。實際上等同於浮出視窗的 Loaded 事件。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing the flyout window and content panel. (zh-Hans: 包含浮出窗口和内容面板的事件参数。 zh-Hant: 包含浮出視窗和內容面板的事件參數。)</param>
		public virtual void Flyout_Loaded (object sender, FlyoutAboutEventArgs e) { }
		/// <summary>
		/// Called when the user invokes and initializes the flyout. Initialization of the flyout can be done here.
		/// <para xml:lang="zh-Hans">当用户调用浮出窗口并初始化浮出窗口时调用。可在此处完成对浮出窗口的初始化工作。</para>
		/// <para xml:lang="zh-Hant">當用戶調用浮出視窗並初始化浮出視窗時調用。可在此處完成對浮出視窗的初始化工作。</para>
		/// </summary>
		/// <param name="sender">The source of the event. (zh-Hans: 事件源。 zh-Hant: 事件來源。)</param>
		/// <param name="e">The event arguments containing the flyout window and content panel. (zh-Hans: 包含浮出窗口和内容面板的事件参数。 zh-Hant: 包含浮出視窗和內容面板的事件參數。)</param>
		public virtual void Flyout_Initialize (object sender, FlyoutAboutEventArgs e) { }
		/// <summary>
		/// Used to receive requests from other tiles or the Sidebar. The return value indicates whether the request was handled successfully.
		/// <para xml:lang="zh-Hans">用于接收其他磁贴或边栏发来的请求。返回值表示是否成功处理。</para>
		/// <para xml:lang="zh-Hant">用於接收其他磁貼或邊欄發來的請求。返回值表示是否成功處理。</para>
		/// </summary>
		/// <param name="req">The request body. (zh-Hans: 请求体。 zh-Hant: 請求體。)</param>
		/// <returns>Returns true if successfully handled, otherwise false. (zh-Hans: 如果成功处理则返回 true，否则返回 false。 zh-Hant: 如果成功處理則返回 true，否則返回 false。)</returns>
		public virtual bool Tile_ReceivedRequest (ITileRequest req) { return false; }
		/// <summary>
		/// Used to receive responses from other tiles or the Sidebar. The return value indicates whether the response was handled successfully.
		/// <para xml:lang="zh-Hans">用于接收其他磁贴或边栏发来的响应。返回值表示是否成功处理。</para>
		/// <para xml:lang="zh-Hant">用於接收其他磁貼或邊欄發來的響應。返回值表示是否成功處理。</para>
		/// </summary>
		/// <param name="resp">The response body. (zh-Hans: 响应体。 zh-Hant: 響應體。)</param>
		/// <returns>Returns true if successfully handled, otherwise false. (zh-Hans: 如果成功处理则返回 true，否则返回 false。 zh-Hant: 如果成功處理則返回 true，否則返回 false。)</returns>
		public virtual bool Tile_ReceivedResponse (ITileResponse resp) { return false; }
		/// <summary>
		/// Gets the host container for the tile content area (casts <see cref="TileBase.TileUI"/> to <see cref="Panel"/>). Developers should add custom UI controls to this container.
		/// <para xml:lang="zh-Hans">获取磁贴内容区的宿主容器（将 <see cref="TileBase.TileUI"/> 转换为 <see cref="Panel"/>）。开发者应将自定义 UI 控件添加到此容器中。</para>
		/// <para xml:lang="zh-Hant">獲取磁貼內容區的宿主容器（將 <see cref="TileBase.TileUI"/> 轉換為 <see cref="Panel"/>）。開發者應將自訂 UI 控制項添加到此容器中。</para>
		/// </summary>
		public Panel ContentArea => TileUI as Panel;
		/// <summary>
		/// Used to initialize the tile instance. This is the first method called in the tile's lifetime.
		/// <para xml:lang="zh-Hans">用于初始化磁贴实例。这是在磁贴生命周期中第一个被调用的方法。</para>
		/// <para xml:lang="zh-Hant">用於初始化磁貼執行個體。這是在磁貼生命週期中第一個被調用的方法。</para>
		/// </summary>
		public virtual void Initialize () { }
		/// <summary>
		/// Used for cleanup when destroying the tile instance. This is the last method called in the complete lifetime of the tile.
		/// <para xml:lang="zh-Hans">用于销毁磁贴实例时的清理工作。这是在磁贴完整生命周期中最后一个被调用的方法。</para>
		/// <para xml:lang="zh-Hant">用於銷毀磁貼執行個體時的清理工作。這是在磁貼完整生命週期中最後一個被調用的方法。</para>
		/// </summary>
		public virtual void Destroy () { }
		#region New Members
		/// <summary>
		/// The cached display name of the tile.
		/// <para xml:lang="zh-Hans">缓存的磁贴显示名。</para>
		/// <para xml:lang="zh-Hant">快取的磁貼顯示名稱。</para>
		/// </summary>
		private string _title;
		/// <summary>
		/// The cached icon of the tile.
		/// <para xml:lang="zh-Hans">缓存的磁贴图标。</para>
		/// <para xml:lang="zh-Hant">快取的磁貼圖示。</para>
		/// </summary>
		private ImageSource _icon;
		/// <summary>
		/// Gets or sets the display name of the tile.
		/// <para xml:lang="zh-Hans">获取或设置磁贴的显示名。</para>
		/// <para xml:lang="zh-Hant">取得或設定磁貼的顯示名稱。</para>
		/// </summary>
		public string Title
		{
			get
			{
				Features?.Request (new SidebarRequest (this) { RequestName = "GetTitle" });
				return _title;
			}
			set
			{
				_title = value;
				Features?.Request (new SidebarRequest (this) { RequestName = "SetTitle", RequestDatas = value });
			}
		}
		/// <summary>
		/// Gets or sets the icon of the tile.
		/// <para xml:lang="zh-Hans">获取或设置磁贴的图标。</para>
		/// <para xml:lang="zh-Hant">取得或設定磁貼的圖示。</para>
		/// </summary>
		public ImageSource Icon
		{
			get
			{
				Features?.Request (new SidebarRequest (this) { RequestName = "GetIcon" });
				return _icon;
			}
			set
			{
				_icon = value;
				Features?.Request (new SidebarRequest (this) { RequestName = "SetIcon", RequestDatas = value });
			}
		}
		#endregion
		#region Hide some members
		/// <summary>
		/// Sealed override that calls <see cref="Initialize"/> and wires the base events to the virtual methods. This method is called by the host and is not intended to be overridden again.
		/// <para xml:lang="zh-Hans">密封重写，调用 <see cref="Initialize"/> 并将基类事件连接到虚方法。此方法由宿主调用，不打算再次重写。</para>
		/// <para xml:lang="zh-Hant">密封覆寫，呼叫 <see cref="Initialize"/> 並將基底類別事件連線到虛擬方法。此方法由宿主呼叫，不打算再次覆寫。</para>
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public sealed override void OnInitialize ()
		{
			Initialize ();
			FlyoutInit += Flyout_Initialize;
			FlyoutShow += Flyout_Loaded;
			FlyoutResize += Flyout_SizeChanged;
			FlyoutClosing += Flyout_Closing;
			FlyoutClosed += Flyout_Closed;
			PropertiesInit += Properties_Initialize;
			PropertiesShow += Properties_Loaded;
			PropertiesClickOK += Properties_ClickOK;
			PropertiesClickCancel += Properties_ClickCancel;
			PropertiesClosing += Properties_Closing;
			PropertiesClosed += Properties_Closed;
			this.Resize += Tile_SizeChanged;
			this.SidebarDirectionChanged += Sidebar_DirectionChanged;
			this.ThemeChanged += Sidebar_ThemeChanged;
			this.Load += Tile_Loaded;
			this.Unload += Tile_Unloaded;
		}
		/// <summary>
		/// Sealed override that unwires the base events and calls <see cref="Destroy"/>. This method is called by the host and is not intended to be overridden again.
		/// <para xml:lang="zh-Hans">密封重写，断开基类事件并调用 <see cref="Destroy"/>。此方法由宿主调用，不打算再次重写。</para>
		/// <para xml:lang="zh-Hant">密封覆寫，中斷基底類別事件並呼叫 <see cref="Destroy"/>。此方法由宿主呼叫，不打算再次覆寫。</para>
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public sealed override void OnDestroy ()
		{
			FlyoutInit -= Flyout_Initialize;
			FlyoutShow -= Flyout_Loaded;
			FlyoutResize -= Flyout_SizeChanged;
			FlyoutClosing -= Flyout_Closing;
			FlyoutClosed -= Flyout_Closed;
			PropertiesInit -= Properties_Initialize;
			PropertiesShow -= Properties_Loaded;
			PropertiesClickOK -= Properties_ClickOK;
			PropertiesClickCancel -= Properties_ClickCancel;
			PropertiesClosing -= Properties_Closing;
			PropertiesClosed -= Properties_Closed;
			this.Resize -= Tile_SizeChanged;
			this.SidebarDirectionChanged -= Sidebar_DirectionChanged;
			this.ThemeChanged -= Sidebar_ThemeChanged;
			this.Load -= Tile_Loaded;
			this.Unload -= Tile_Unloaded;
			Destroy ();
		}
		/// <summary>
		/// Sealed override that dispatches incoming requests to <see cref="Tile_ReceivedRequest"/>.
		/// <para xml:lang="zh-Hans">密封重写，将传入的请求分派给 <see cref="Tile_ReceivedRequest"/>。</para>
		/// <para xml:lang="zh-Hant">密封覆寫，將傳入的要求分派給 <see cref="Tile_ReceivedRequest"/>。</para>
		/// </summary>
		/// <param name="req">The request object.
		/// <para xml:lang="zh-Hans">请求对象。</para>
		/// <para xml:lang="zh-Hant">要求物件。</para>
		/// </param>
		/// <returns><c>true</c> if the request was handled; otherwise, <c>false</c>.
		/// <para xml:lang="zh-Hans">如果已处理请求则返回 <c>true</c>；否则返回 <c>false</c>。</para>
		/// <para xml:lang="zh-Hant">如果已處理要求則傳回 <c>true</c>；否則傳回 <c>false</c>。</para>
		/// </returns>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public sealed override bool OnRequest (ITileRequest req)
		{
			return Tile_ReceivedRequest (req);
			return base.OnRequest (req);
		}
		/// <summary>
		/// Sealed override that updates the cached title and icon from Sidebar responses and dispatches incoming responses to <see cref="Tile_ReceivedResponse"/>.
		/// <para xml:lang="zh-Hans">密封重写，根据边栏响应更新缓存的标题和图标，并将传入的响应分派给 <see cref="Tile_ReceivedResponse"/>。</para>
		/// <para xml:lang="zh-Hant">密封覆寫，根據邊欄回應更新快取的標題和圖示，並將傳入的回應分派給 <see cref="Tile_ReceivedResponse"/>。</para>
		/// </summary>
		/// <param name="resp">The response object.
		/// <para xml:lang="zh-Hans">响应对象。</para>
		/// <para xml:lang="zh-Hant">回應物件。</para>
		/// </param>
		/// <returns><c>true</c> if the response was handled; otherwise, <c>false</c>.
		/// <para xml:lang="zh-Hans">如果已处理响应则返回 <c>true</c>；否则返回 <c>false</c>。</para>
		/// <para xml:lang="zh-Hant">如果已處理回應則傳回 <c>true</c>；否則傳回 <c>false</c>。</para>
		/// </returns>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public sealed override bool OnResponse (ITileResponse resp)
		{
			if (resp.ResponseSource.NEquals ("Sidebar"))
			{
				if (resp.ResponseName.NEquals ("ReturnGetTitle"))
				{
					_title = resp.ResponseData as string;
				}
				else if (resp.ResponseName.NEquals ("ReturnGetIcon"))
				{
					_icon = resp.ResponseData as ImageSource;
				}
			}
			return Tile_ReceivedResponse (resp);
			return base.OnResponse (resp);
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.OnHostChanged"/> method so that derived classes must use the corresponding virtual methods instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.OnHostChanged"/> 方法，使派生类必须改用相应的虚方法。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.OnHostChanged"/> 方法，使衍生類別必須改用對應的虛擬方法。</para>
		/// </summary>
		private new void OnHostChanged (TileHostEvent eventName, object context, object sender = null)
		{
			base.OnHostChanged (eventName, context, sender);
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.OnLoad"/> method so that derived classes must use <see cref="Tile_Loaded"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.OnLoad"/> 方法，使派生类必须改用 <see cref="Tile_Loaded"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.OnLoad"/> 方法，使衍生類別必須改用 <see cref="Tile_Loaded"/>。</para>
		/// </summary>
		private new void OnLoad ()
		{
			base.OnLoad ();
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.OnUnload"/> method so that derived classes must use <see cref="Tile_Unloaded"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.OnUnload"/> 方法，使派生类必须改用 <see cref="Tile_Unloaded"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.OnUnload"/> 方法，使衍生類別必須改用 <see cref="Tile_Unloaded"/>。</para>
		/// </summary>
		private new void OnUnload ()
		{
			base.OnUnload ();
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.FlyoutInit"/> event so that derived classes must override <see cref="Flyout_Initialize"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.FlyoutInit"/> 事件，使派生类必须改用 <see cref="Flyout_Initialize"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.FlyoutInit"/> 事件，使衍生類別必須改用 <see cref="Flyout_Initialize"/>。</para>
		/// </summary>
		private new event EventHandler<FlyoutAboutEventArgs> FlyoutInit
		{
			add { base.FlyoutInit += value; }
			remove { base.FlyoutInit -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.FlyoutShow"/> event so that derived classes must override <see cref="Flyout_Loaded"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.FlyoutShow"/> 事件，使派生类必须改用 <see cref="Flyout_Loaded"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.FlyoutShow"/> 事件，使衍生類別必須改用 <see cref="Flyout_Loaded"/>。</para>
		/// </summary>
		private new event EventHandler<FlyoutAboutEventArgs> FlyoutShow
		{
			add { base.FlyoutShow += value; }
			remove { base.FlyoutShow -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.FlyoutResize"/> event so that derived classes must override <see cref="Flyout_SizeChanged"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.FlyoutResize"/> 事件，使派生类必须改用 <see cref="Flyout_SizeChanged"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.FlyoutResize"/> 事件，使衍生類別必須改用 <see cref="Flyout_SizeChanged"/>。</para>
		/// </summary>
		private new event EventHandler<SizeChangedEventArgs> FlyoutResize
		{
			add { base.FlyoutResize += value; }
			remove { base.FlyoutResize -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.FlyoutClosing"/> event so that derived classes must override <see cref="Flyout_Closing"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.FlyoutClosing"/> 事件，使派生类必须改用 <see cref="Flyout_Closing"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.FlyoutClosing"/> 事件，使衍生類別必須改用 <see cref="Flyout_Closing"/>。</para>
		/// </summary>
		private new event EventHandler<FlyoutAboutEventArgs> FlyoutClosing
		{
			add { base.FlyoutClosing += value; }
			remove { base.FlyoutClosing -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.FlyoutClosed"/> event so that derived classes must override <see cref="Flyout_Closed"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.FlyoutClosed"/> 事件，使派生类必须改用 <see cref="Flyout_Closed"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.FlyoutClosed"/> 事件，使衍生類別必須改用 <see cref="Flyout_Closed"/>。</para>
		/// </summary>
		private new event EventHandler FlyoutClosed
		{
			add { base.FlyoutClosed += value; }
			remove { base.FlyoutClosed -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.PropertiesInit"/> event so that derived classes must override <see cref="Properties_Initialize"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.PropertiesInit"/> 事件，使派生类必须改用 <see cref="Properties_Initialize"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.PropertiesInit"/> 事件，使衍生類別必須改用 <see cref="Properties_Initialize"/>。</para>
		/// </summary>
		private new event EventHandler<PropertiesAboutEventArgs> PropertiesInit
		{
			add { base.PropertiesInit += value; }
			remove { base.PropertiesInit -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.PropertiesShow"/> event so that derived classes must override <see cref="Properties_Loaded"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.PropertiesShow"/> 事件，使派生类必须改用 <see cref="Properties_Loaded"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.PropertiesShow"/> 事件，使衍生類別必須改用 <see cref="Properties_Loaded"/>。</para>
		/// </summary>
		private new event EventHandler<PropertiesAboutEventArgs> PropertiesShow
		{
			add { base.PropertiesShow += value; }
			remove { base.PropertiesShow -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.PropertiesClickOK"/> event so that derived classes must override <see cref="Properties_ClickOK"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.PropertiesClickOK"/> 事件，使派生类必须改用 <see cref="Properties_ClickOK"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.PropertiesClickOK"/> 事件，使衍生類別必須改用 <see cref="Properties_ClickOK"/>。</para>
		/// </summary>
		private new event EventHandler<PropertiesAboutEventArgs> PropertiesClickOK
		{
			add { base.PropertiesClickOK += value; }
			remove { base.PropertiesClickOK -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.PropertiesClickCancel"/> event so that derived classes must override <see cref="Properties_ClickCancel"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.PropertiesClickCancel"/> 事件，使派生类必须改用 <see cref="Properties_ClickCancel"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.PropertiesClickCancel"/> 事件，使衍生類別必須改用 <see cref="Properties_ClickCancel"/>。</para>
		/// </summary>
		private new event EventHandler<PropertiesAboutEventArgs> PropertiesClickCancel
		{
			add { base.PropertiesClickCancel += value; }
			remove { base.PropertiesClickCancel -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.PropertiesClosing"/> event so that derived classes must override <see cref="Properties_Closing"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.PropertiesClosing"/> 事件，使派生类必须改用 <see cref="Properties_Closing"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.PropertiesClosing"/> 事件，使衍生類別必須改用 <see cref="Properties_Closing"/>。</para>
		/// </summary>
		private new event EventHandler<PropertiesAboutEventArgs> PropertiesClosing
		{
			add { base.PropertiesClosing += value; }
			remove { base.PropertiesClosing -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.PropertiesClosed"/> event so that derived classes must override <see cref="Properties_Closed"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.PropertiesClosed"/> 事件，使派生类必须改用 <see cref="Properties_Closed"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.PropertiesClosed"/> 事件，使衍生類別必須改用 <see cref="Properties_Closed"/>。</para>
		/// </summary>
		private new event EventHandler PropertiesClosed
		{
			add { base.PropertiesClosed += value; }
			remove { base.PropertiesClosed -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.Resize"/> event so that derived classes must override <see cref="Tile_SizeChanged"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.Resize"/> 事件，使派生类必须改用 <see cref="Tile_SizeChanged"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.Resize"/> 事件，使衍生類別必須改用 <see cref="Tile_SizeChanged"/>。</para>
		/// </summary>
		private new event EventHandler<SizeChangedEventArgs> Resize
		{
			add { base.Resize += value; }
			remove { base.Resize -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.SidebarDirectionChanged"/> event so that derived classes must override <see cref="Sidebar_DirectionChanged"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.SidebarDirectionChanged"/> 事件，使派生类必须改用 <see cref="Sidebar_DirectionChanged"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.SidebarDirectionChanged"/> 事件，使衍生類別必須改用 <see cref="Sidebar_DirectionChanged"/>。</para>
		/// </summary>
		private new event EventHandler<SidebarDirectionChangedEventArgs> SidebarDirectionChanged
		{
			add { base.SidebarDirectionChanged += value; }
			remove { base.SidebarDirectionChanged -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.ThemeChanged"/> event so that derived classes must override <see cref="Sidebar_ThemeChanged"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.ThemeChanged"/> 事件，使派生类必须改用 <see cref="Sidebar_ThemeChanged"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.ThemeChanged"/> 事件，使衍生類別必須改用 <see cref="Sidebar_ThemeChanged"/>。</para>
		/// </summary>
		private new event EventHandler<ThemeChangedEventArgs> ThemeChanged
		{
			add { base.ThemeChanged += value; }
			remove { base.ThemeChanged -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.Load"/> event so that derived classes must override <see cref="Tile_Loaded"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.Load"/> 事件，使派生类必须改用 <see cref="Tile_Loaded"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.Load"/> 事件，使衍生類別必須改用 <see cref="Tile_Loaded"/>。</para>
		/// </summary>
		private new event EventHandler Load
		{
			add { base.Load += value; }
			remove { base.Load -= value; }
		}
		/// <summary>
		/// Hides the base <see cref="TileBase.Unload"/> event so that derived classes must override <see cref="Tile_Unloaded"/> instead.
		/// <para xml:lang="zh-Hans">隐藏基类 <see cref="TileBase.Unload"/> 事件，使派生类必须改用 <see cref="Tile_Unloaded"/>。</para>
		/// <para xml:lang="zh-Hant">隱藏基底類別 <see cref="TileBase.Unload"/> 事件，使衍生類別必須改用 <see cref="Tile_Unloaded"/>。</para>
		/// </summary>
		private new event EventHandler Unload
		{
			add { base.Unload += value; }
			remove { base.Unload -= value; }
		}
		#endregion
	}
}