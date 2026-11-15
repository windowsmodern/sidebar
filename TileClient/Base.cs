using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Sidebar
{
	/// <summary>
	/// Defines the contract implemented by all Sidebar tiles.
	/// <para xml:lang="zh-Hans">定义所有边栏磁贴所实现的契约。</para>
	/// <para xml:lang="zh-Hant">定義所有邊欄磁貼所實作的契約。</para>
	/// </summary>
	[ComVisible (true)]
	public interface ITileBase
	{
		/// <summary>
		/// Gets the tile manifest. This property is read-only.
		/// <para xml:lang="zh-Hans">获取磁贴清单。此属性为只读。</para>
		/// <para xml:lang="zh-Hant">取得磁貼資訊清單。此屬性為唯讀。</para>
		/// </summary>
		ITileManifest Manifest { get; }
		/// <summary>
		/// Gets the folder where the tile files are located (the root directory containing the manifest and the tile class library).
		/// <para xml:lang="zh-Hans">获取磁贴文件所在文件夹（包含清单和磁贴类库的根目录）。</para>
		/// <para xml:lang="zh-Hant">取得磁貼檔案所在資料夾（包含資訊清單和磁貼類別庫的根目錄）。</para>
		/// </summary>
		IProgramFolder Region { get; }
		/// <summary>
		/// Gets the per-user configuration for this tile instance.
		/// <para xml:lang="zh-Hans">获取此磁贴实例的用户个人配置。</para>
		/// <para xml:lang="zh-Hant">取得此磁貼執行個體的使用者個人設定。</para>
		/// </summary>
		ITileConfig Config { get; }
		/// <summary>
		/// Gets the feature interface used to communicate with Sidebar and other tiles.
		/// <para xml:lang="zh-Hans">获取用于与边栏和其他磁贴通信的功能接口。</para>
		/// <para xml:lang="zh-Hant">取得用於與邊欄和其他磁貼通訊的功能介面。</para>
		/// </summary>
		ISidebarFeatures Features { get; }
		/// <summary>
		/// Gets the per-user folder for this tile instance, used to store user-specific settings and data.
		/// <para xml:lang="zh-Hans">获取此磁贴实例的用户个人文件夹，用于存储与用户相关的设置和数据。</para>
		/// <para xml:lang="zh-Hant">取得此磁貼執行個體的使用者個人資料夾，用於儲存與使用者相關的設定和資料。</para>
		/// </summary>
		IProgramFolder UserRegion { get; }
		/// <summary>
		/// Gets the host container for the tile content. Developers should add custom UI controls to this element (as a <see cref="Panel"/>).
		/// <para xml:lang="zh-Hans">获取磁贴内容的宿主容器。开发者应将自定义 UI 控件添加到此元素（作为 <see cref="Panel"/>）。</para>
		/// <para xml:lang="zh-Hant">取得磁貼內容的宿主容器。開發人員應將自訂 UI 控制項加入此元素（作為 <see cref="Panel"/>）。</para>
		/// </summary>
		UIElement TileUI { get; }
		/// <summary>
		/// Gets the tile application information, including the application ID and metadata.
		/// <para xml:lang="zh-Hans">获取磁贴应用信息，包括应用 ID 和元数据。</para>
		/// <para xml:lang="zh-Hant">取得磁貼應用程式資訊，包括應用程式 ID 和中繼資料。</para>
		/// </summary>
		ITileApplication Application { get; }
		/// <summary>
		/// Gets the unique user model ID of this tile instance, in the form <c>FamilyName!Id</c>.
		/// <para xml:lang="zh-Hans">获取此磁贴实例的唯一用户模型 ID，格式为 <c>FamilyName!Id</c>。</para>
		/// <para xml:lang="zh-Hant">取得此磁貼執行個體的唯一使用者模型 ID，格式為 <c>FamilyName!Id</c>。</para>
		/// </summary>
		string UserModelId { get; }
		/// <summary>
		/// Called by the host when the tile UI is loaded. This method is not intended to be called by tile code.
		/// <para xml:lang="zh-Hans">由宿主在磁贴 UI 载入时调用。此方法不应由磁贴代码直接调用。</para>
		/// <para xml:lang="zh-Hant">由宿主在磁貼 UI 載入時呼叫。此方法不應由磁貼程式碼直接呼叫。</para>
		/// </summary>
		void OnLoad ();
		/// <summary>
		/// Called by the host when the tile UI is unloaded. This method is not intended to be called by tile code.
		/// <para xml:lang="zh-Hans">由宿主在磁贴 UI 卸载时调用。此方法不应由磁贴代码直接调用。</para>
		/// <para xml:lang="zh-Hant">由宿主在磁貼 UI 卸載時呼叫。此方法不應由磁貼程式碼直接呼叫。</para>
		/// </summary>
		void OnUnload ();
		/// <summary>
		/// Called by the host to notify the tile of a host event. This method is not intended to be called by tile code.
		/// <para xml:lang="zh-Hans">由宿主调用，用于通知磁贴发生了宿主事件。此方法不应由磁贴代码直接调用。</para>
		/// <para xml:lang="zh-Hant">由宿主呼叫，用於通知磁貼發生了宿主事件。此方法不應由磁貼程式碼直接呼叫。</para>
		/// </summary>
		/// <param name="eventName">The host event identifier.
		/// <para xml:lang="zh-Hans">宿主事件标识。</para>
		/// <para xml:lang="zh-Hant">宿主事件識別碼。</para>
		/// </param>
		/// <param name="context">The context object containing tile or sidebar information.
		/// <para xml:lang="zh-Hans">包含磁贴或边栏信息的上下文对象。</para>
		/// <para xml:lang="zh-Hant">包含磁貼或邊欄資訊的內容物件。</para>
		/// </param>
		/// <param name="sender">The optional sender object.
		/// <para xml:lang="zh-Hans">可选的事件源对象。</para>
		/// <para xml:lang="zh-Hant">選擇性的事件來源物件。</para>
		/// </param>
		void OnHostChanged (TileHostEvent eventName, object context, object sender = null);
		/// <summary>
		/// Called when a request is received from Sidebar or another tile. Override this method to handle requests.
		/// <para xml:lang="zh-Hans">当收到来自边栏或其他磁贴的请求时调用。可重写此方法以处理请求。</para>
		/// <para xml:lang="zh-Hant">當收到來自邊欄或其他磁貼的要求時呼叫。可覆寫此方法以處理要求。</para>
		/// </summary>
		/// <param name="req">The request object.
		/// <para xml:lang="zh-Hans">请求对象。</para>
		/// <para xml:lang="zh-Hant">要求物件。</para>
		/// </param>
		/// <returns><c>true</c> if the request was handled; otherwise, <c>false</c>.
		/// <para xml:lang="zh-Hans">如果已处理请求则返回 <c>true</c>；否则返回 <c>false</c>。</para>
		/// <para xml:lang="zh-Hant">如果已處理要求則傳回 <c>true</c>；否則傳回 <c>false</c>。</para>
		/// </returns>
		bool OnRequest (ITileRequest req);
		/// <summary>
		/// Called when a response is received from Sidebar or another tile. Override this method to handle responses.
		/// <para xml:lang="zh-Hans">当收到来自边栏或其他磁贴的响应时调用。可重写此方法以处理响应。</para>
		/// <para xml:lang="zh-Hant">當收到來自邊欄或其他磁貼的回應時呼叫。可覆寫此方法以處理回應。</para>
		/// </summary>
		/// <param name="resp">The response object.
		/// <para xml:lang="zh-Hans">响应对象。</para>
		/// <para xml:lang="zh-Hant">回應物件。</para>
		/// </param>
		/// <returns><c>true</c> if the response was handled; otherwise, <c>false</c>.
		/// <para xml:lang="zh-Hans">如果已处理响应则返回 <c>true</c>；否则返回 <c>false</c>。</para>
		/// <para xml:lang="zh-Hant">如果已處理回應則傳回 <c>true</c>；否則傳回 <c>false</c>。</para>
		/// </returns>
		bool OnResponse (ITileResponse resp);
	}
	/// <summary>
	/// Provides context information about the tile and its associated windows, such as sizes and UI elements.
	/// <para xml:lang="zh-Hans">提供磁贴及其关联窗口的上下文信息，例如尺寸和 UI 元素。</para>
	/// <para xml:lang="zh-Hant">提供磁貼及其關聯視窗的內容資訊，例如尺寸和 UI 元素。</para>
	/// </summary>
	[ComVisible (true)]
	public interface ITileContext
	{
		/// <summary>
		/// Gets the overall size of the tile in pixels.
		/// <para xml:lang="zh-Hans">获取磁贴的整体尺寸（像素）。</para>
		/// <para xml:lang="zh-Hant">取得磁貼的整體尺寸（像素）。</para>
		/// </summary>
		System.Drawing.Size TileSize { get; }
		/// <summary>
		/// Gets the size of the tile content area in pixels.
		/// <para xml:lang="zh-Hans">获取磁贴内容区的尺寸（像素）。</para>
		/// <para xml:lang="zh-Hant">取得磁貼內容區的尺寸（像素）。</para>
		/// </summary>
		System.Drawing.Size TileClientSize { get; }
		/// <summary>
		/// Gets the overall size of the flyout window in pixels.
		/// <para xml:lang="zh-Hans">获取浮出窗口的整体尺寸（像素）。</para>
		/// <para xml:lang="zh-Hant">取得浮出視窗的整體尺寸（像素）。</para>
		/// </summary>
		System.Drawing.Size TileFlyoutSize { get; }
		/// <summary>
		/// Gets the size of the flyout content area in pixels.
		/// <para xml:lang="zh-Hans">获取浮出窗口内容区的尺寸（像素）。</para>
		/// <para xml:lang="zh-Hant">取得浮出視窗內容區的尺寸（像素）。</para>
		/// </summary>
		System.Drawing.Size TileFlyoutClientSize { get; }
		/// <summary>
		/// Gets the overall size of the tile in WPF units.
		/// <para xml:lang="zh-Hans">获取磁贴的整体尺寸（WPF 单位）。</para>
		/// <para xml:lang="zh-Hant">取得磁貼的整體尺寸（WPF 單位）。</para>
		/// </summary>
		System.Windows.Size WpfTileSize { get; }
		/// <summary>
		/// Gets the size of the tile content area in WPF units.
		/// <para xml:lang="zh-Hans">获取磁贴内容区的尺寸（WPF 单位）。</para>
		/// <para xml:lang="zh-Hant">取得磁貼內容區的尺寸（WPF 單位）。</para>
		/// </summary>
		System.Windows.Size WpfTileClientSize { get; }
		/// <summary>
		/// Gets the overall size of the flyout window in WPF units.
		/// <para xml:lang="zh-Hans">获取浮出窗口的整体尺寸（WPF 单位）。</para>
		/// <para xml:lang="zh-Hant">取得浮出視窗的整體尺寸（WPF 單位）。</para>
		/// </summary>
		System.Windows.Size WpfTileFlyoutSize { get; }
		/// <summary>
		/// Gets the size of the flyout content area in WPF units.
		/// <para xml:lang="zh-Hans">获取浮出窗口内容区的尺寸（WPF 单位）。</para>
		/// <para xml:lang="zh-Hant">取得浮出視窗內容區的尺寸（WPF 單位）。</para>
		/// </summary>
		System.Windows.Size WpfTileFlyoutClientSize { get; }
		/// <summary>
		/// Gets the flyout window object.
		/// <para xml:lang="zh-Hans">获取浮出窗口对象。</para>
		/// <para xml:lang="zh-Hant">取得浮出視窗物件。</para>
		/// </summary>
		UIElement FlyoutWindow { get; }
		/// <summary>
		/// Gets the content panel of the flyout window.
		/// <para xml:lang="zh-Hans">获取浮出窗口的内容面板。</para>
		/// <para xml:lang="zh-Hant">取得浮出視窗的內容面板。</para>
		/// </summary>
		UIElement FlyoutUI { get; }
		/// <summary>
		/// Gets the properties window object.
		/// <para xml:lang="zh-Hans">获取属性窗口对象。</para>
		/// <para xml:lang="zh-Hant">取得屬性視窗物件。</para>
		/// </summary>
		UIElement PropertiesWindow { get; }
		/// <summary>
		/// Gets the content panel of the properties window.
		/// <para xml:lang="zh-Hans">获取属性窗口的内容面板。</para>
		/// <para xml:lang="zh-Hant">取得屬性視窗的內容面板。</para>
		/// </summary>
		UIElement PropertiesContent { get; }
	}
	/// <summary>
	/// Provides information about the current Sidebar context, such as its direction and theme.
	/// <para xml:lang="zh-Hans">提供当前边栏上下文的信息，例如方向与主题。</para>
	/// <para xml:lang="zh-Hant">提供目前邊欄內容的資訊，例如方向與主題。</para>
	/// </summary>
	[ComVisible (true)]
	public interface ISidebarContext
	{
		/// <summary>
		/// Gets the current docking direction of the Sidebar.
		/// <para xml:lang="zh-Hans">获取边栏当前的停靠方向。</para>
		/// <para xml:lang="zh-Hant">取得邊欄目前的停駐方向。</para>
		/// </summary>
		SidebarDirection Direction { get; }
		/// <summary>
		/// Gets the name of the current theme (the folder name under the Themes directory, not the display name).
		/// <para xml:lang="zh-Hans">获取当前主题的名称（Themes 目录下的文件夹名，而非显示名称）。</para>
		/// <para xml:lang="zh-Hant">取得目前主題的名稱（Themes 目錄下的資料夾名稱，而非顯示名稱）。</para>
		/// </summary>
		string ThemeName { get; }
		/// <summary>
		/// Gets the current theme object.
		/// <para xml:lang="zh-Hans">获取当前主题对象。</para>
		/// <para xml:lang="zh-Hant">取得目前主題物件。</para>
		/// </summary>
		ITheme Theme { get; }
	}
	[ComVisible (true)]
	public interface ITileRuntime
	{
		ITileManifest Manifest { get; }
		IProgramFolder Region { get; }
		ITileConfig Config { get; }
		IProgramFolder UserRegion { get; }
		UIElement TileUI { get; }
		ISidebarFeatures Features { get; }
		ITileApplication Application { get; }
	}
	/// <summary>
	/// Base class for all Sidebar tiles. A tile class must inherit from this class and is instantiated once by the host.
	/// <para xml:lang="zh-Hans">所有边栏磁贴的基类。磁贴类必须继承此类，并由宿主创建唯一实例。</para>
	/// <para xml:lang="zh-Hant">所有邊欄磁貼的基底類別。磁貼類別必須繼承此類別，並由宿主建立唯一執行個體。</para>
	/// </summary>
	[ComVisible (true)]
	public abstract class TileBase: ITileBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TileBase"/> class.
		/// <para xml:lang="zh-Hans">初始化 <see cref="TileBase"/> 类的新实例。</para>
		/// <para xml:lang="zh-Hant">初始化 <see cref="TileBase"/> 類別的新執行個體。</para>
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public TileBase () { }
		/// <summary>
		/// Injects the runtime services into the tile instance. This method is called by the host before <see cref="OnInitialize"/>.
		/// <para xml:lang="zh-Hans">将运行时服务注入磁贴实例。宿主会在调用 <see cref="OnInitialize"/> 之前调用此方法。</para>
		/// <para xml:lang="zh-Hant">將執行階段服務注入磁貼執行個體。宿主會在呼叫 <see cref="OnInitialize"/> 之前呼叫此方法。</para>
		/// </summary>
		/// <param name="runtime">The runtime context that provides the manifest, folders, UI container, configuration, and features.
		/// <para xml:lang="zh-Hans">运行时上下文，提供清单、文件夹、UI 容器、配置和功能接口。</para>
		/// <para xml:lang="zh-Hant">執行階段內容，提供資訊清單、資料夾、UI 容器、設定和功能介面。</para>
		/// </param>
		[EditorBrowsable (EditorBrowsableState.Never)]
		internal void InitTileInstance (ITileRuntime runtime)
		{
			Manifest = runtime.Manifest;
			Region = runtime.Region;
			TileUI = runtime.TileUI;
			Config = runtime.Config;
			Features = runtime.Features;
			UserRegion = runtime.UserRegion;
			Application = runtime.Application;
		}
		/// <summary>
		/// Gets the tile manifest. This property is read-only.
		/// <para xml:lang="zh-Hans">获取磁贴清单。此属性为只读。</para>
		/// <para xml:lang="zh-Hant">取得磁貼資訊清單。此屬性為唯讀。</para>
		/// </summary>
		public ITileManifest Manifest { get; private set; } = null;
		/// <summary>
		/// Gets the folder where the tile files are located (the root directory containing the manifest and the tile class library).
		/// <para xml:lang="zh-Hans">获取磁贴文件所在文件夹（包含清单和磁贴类库的根目录）。</para>
		/// <para xml:lang="zh-Hant">取得磁貼檔案所在資料夾（包含資訊清單和磁貼類別庫的根目錄）。</para>
		/// </summary>
		public IProgramFolder Region { get; private set; } = null;
		/// <summary>
		/// Gets the per-user folder for this tile instance, used to store user-specific settings and data.
		/// <para xml:lang="zh-Hans">获取此磁贴实例的用户个人文件夹，用于存储与用户相关的设置和数据。</para>
		/// <para xml:lang="zh-Hant">取得此磁貼執行個體的使用者個人資料夾，用於儲存與使用者相關的設定和資料。</para>
		/// </summary>
		public IProgramFolder UserRegion { get; private set; } = null;
		/// <summary>
		/// Gets the host container for the tile content. Developers should add custom UI controls to this element (as a <see cref="Panel"/>).
		/// <para xml:lang="zh-Hans">获取磁贴内容的宿主容器。开发者应将自定义 UI 控件添加到此元素（作为 <see cref="Panel"/>）。</para>
		/// <para xml:lang="zh-Hant">取得磁貼內容的宿主容器。開發人員應將自訂 UI 控制項加入此元素（作為 <see cref="Panel"/>）。</para>
		/// </summary>
		public UIElement TileUI { get; private set; } = null;
		/// <summary>
		/// Gets the tile application information, including the application ID and metadata.
		/// <para xml:lang="zh-Hans">获取磁贴应用信息，包括应用 ID 和元数据。</para>
		/// <para xml:lang="zh-Hant">取得磁貼應用程式資訊，包括應用程式 ID 和中繼資料。</para>
		/// </summary>
		public ITileApplication Application { get; private set; } = null;
		/// <summary>
		/// Gets the unique user model ID of this tile instance, in the form <c>FamilyName!Id</c>.
		/// <para xml:lang="zh-Hans">获取此磁贴实例的唯一用户模型 ID，格式为 <c>FamilyName!Id</c>。</para>
		/// <para xml:lang="zh-Hant">取得此磁貼執行個體的唯一使用者模型 ID，格式為 <c>FamilyName!Id</c>。</para>
		/// </summary>
		public string UserModelId => $"{Manifest?.Identity?.FamilyName}!{Application?.Id}";
		/// <summary>
		/// Called when the tile instance is initialized. This is the first method called in the tile lifetime.
		/// <para xml:lang="zh-Hans">当磁贴实例初始化时调用。这是磁贴生命周期中第一个被调用的方法。</para>
		/// <para xml:lang="zh-Hant">當磁貼執行個體初始化時呼叫。這是磁貼生命週期中第一個被呼叫的方法。</para>
		/// </summary>
		public virtual void OnInitialize () { }
		/// <summary>
		/// Called when the tile instance is destroyed. This is the last method called in the tile lifetime. Use it for cleanup.
		/// <para xml:lang="zh-Hans">当磁贴实例销毁时调用。这是磁贴生命周期中最后一个被调用的方法，用于清理工作。</para>
		/// <para xml:lang="zh-Hant">當磁貼執行個體銷毀時呼叫。這是磁貼生命週期中最後一個被呼叫的方法，用於清理工作。</para>
		/// </summary>
		public virtual void OnDestroy () { }
		/// <summary>
		/// Called by the host to notify the tile of a host event. This method is not intended to be called by tile code.
		/// <para xml:lang="zh-Hans">由宿主调用，用于通知磁贴发生了宿主事件。此方法不应由磁贴代码直接调用。</para>
		/// <para xml:lang="zh-Hant">由宿主呼叫，用於通知磁貼發生了宿主事件。此方法不應由磁貼程式碼直接呼叫。</para>
		/// </summary>
		/// <param name="eventName">The host event identifier.
		/// <para xml:lang="zh-Hans">宿主事件标识。</para>
		/// <para xml:lang="zh-Hant">宿主事件識別碼。</para>
		/// </param>
		/// <param name="context">The context object containing tile or sidebar information.
		/// <para xml:lang="zh-Hans">包含磁贴或边栏信息的上下文对象。</para>
		/// <para xml:lang="zh-Hant">包含磁貼或邊欄資訊的內容物件。</para>
		/// </param>
		/// <param name="sender">The optional sender object.
		/// <para xml:lang="zh-Hans">可选的事件源对象。</para>
		/// <para xml:lang="zh-Hant">選擇性的事件來源物件。</para>
		/// </param>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public void OnHostChanged (TileHostEvent eventName, object context, object sender = null)
		{
			var tileContext = context as ITileContext;
			var sidebarContext = context as ISidebarContext;
			switch (eventName)
			{
				case TileHostEvent.FlyoutClosed: FlyoutClosed?.Invoke (sender, null); break;
				case TileHostEvent.FlyoutClosing: FlyoutClosing?.Invoke (sender, new FlyoutAboutEventArgs (tileContext.FlyoutUI, tileContext.FlyoutWindow)); break;
				case TileHostEvent.FlyoutInit: FlyoutInit?.Invoke (this, new FlyoutAboutEventArgs (tileContext.FlyoutUI, tileContext.FlyoutWindow)); break;
				case TileHostEvent.FlyoutResize: FlyoutResize?.Invoke (sender, new SizeChangedEventArgs (tileContext.TileFlyoutSize, tileContext.TileFlyoutClientSize, tileContext.WpfTileFlyoutSize, tileContext.WpfTileFlyoutClientSize)); break;
				case TileHostEvent.FlyoutShow: FlyoutShow?.Invoke (sender, new FlyoutAboutEventArgs (tileContext.FlyoutUI, tileContext.FlyoutWindow)); break;
				case TileHostEvent.Resize: Resize?.Invoke (sender, new SizeChangedEventArgs (tileContext.TileSize, tileContext.TileClientSize, tileContext.WpfTileSize, tileContext.WpfTileClientSize)); break;
				case TileHostEvent.SidebarDirectionChanged: SidebarDirectionChanged?.Invoke (this, new SidebarDirectionChangedEventArgs (sidebarContext.Direction)); break;
				case TileHostEvent.ThemeChanged: ThemeChanged?.Invoke (this, new ThemeChangedEventArgs (sidebarContext.ThemeName, sidebarContext.Theme)); break;
				case TileHostEvent.PropertiesInit: PropertiesInit?.Invoke (this, new PropertiesAboutEventArgs (tileContext.PropertiesWindow, tileContext.PropertiesContent)); break;
				case TileHostEvent.PropertiesLoad: PropertiesShow?.Invoke (sender, new PropertiesAboutEventArgs (tileContext.PropertiesWindow, tileContext.PropertiesContent)); break;
				case TileHostEvent.PropertiesClosing: PropertiesClosing?.Invoke (sender, new PropertiesAboutEventArgs (tileContext.PropertiesWindow, tileContext.PropertiesContent)); break;
				case TileHostEvent.PropertiesClickOkButton: PropertiesClickOK?.Invoke (sender, new PropertiesAboutEventArgs (tileContext.PropertiesWindow, tileContext.PropertiesContent)); break;
				case TileHostEvent.PropertiesClickCancelButton: PropertiesClickCancel?.Invoke (sender, new PropertiesAboutEventArgs (tileContext.PropertiesWindow, tileContext.PropertiesContent)); break;
				case TileHostEvent.PropertiesClosed: PropertiesClosed?.Invoke (sender, null); break;
			}
		}
		/// <summary>
		/// Called by the host when the tile UI is loaded. This method is not intended to be called by tile code.
		/// <para xml:lang="zh-Hans">由宿主在磁贴 UI 载入时调用。此方法不应由磁贴代码直接调用。</para>
		/// <para xml:lang="zh-Hant">由宿主在磁貼 UI 載入時呼叫。此方法不應由磁貼程式碼直接呼叫。</para>
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public void OnLoad () { Load?.Invoke (this, null); }
		/// <summary>
		/// Called by the host when the tile UI is unloaded. This method is not intended to be called by tile code.
		/// <para xml:lang="zh-Hans">由宿主在磁贴 UI 卸载时调用。此方法不应由磁贴代码直接调用。</para>
		/// <para xml:lang="zh-Hant">由宿主在磁貼 UI 卸載時呼叫。此方法不應由磁貼程式碼直接呼叫。</para>
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public void OnUnload () { Unload?.Invoke (this, null); }
		/// <summary>
		/// Called when a response is received from Sidebar or another tile. Override this method to handle responses.
		/// <para xml:lang="zh-Hans">当收到来自边栏或其他磁贴的响应时调用。可重写此方法以处理响应。</para>
		/// <para xml:lang="zh-Hant">當收到來自邊欄或其他磁貼的回應時呼叫。可覆寫此方法以處理回應。</para>
		/// </summary>
		/// <param name="resp">The response object.
		/// <para xml:lang="zh-Hans">响应对象。</para>
		/// <para xml:lang="zh-Hant">回應物件。</para>
		/// </param>
		/// <returns><c>true</c> if the response was handled; otherwise, <c>false</c>.
		/// <para xml:lang="zh-Hans">如果已处理响应则返回 <c>true</c>；否则返回 <c>false</c>。</para>
		/// <para xml:lang="zh-Hant">如果已處理回應則傳回 <c>true</c>；否則傳回 <c>false</c>。</para>
		/// </returns>
		public virtual bool OnResponse (ITileResponse resp) { return false; }
		/// <summary>
		/// Called when a request is received from Sidebar or another tile. Override this method to handle requests.
		/// <para xml:lang="zh-Hans">当收到来自边栏或其他磁贴的请求时调用。可重写此方法以处理请求。</para>
		/// <para xml:lang="zh-Hant">當收到來自邊欄或其他磁貼的要求時呼叫。可覆寫此方法以處理要求。</para>
		/// </summary>
		/// <param name="req">The request object.
		/// <para xml:lang="zh-Hans">请求对象。</para>
		/// <para xml:lang="zh-Hant">要求物件。</para>
		/// </param>
		/// <returns><c>true</c> if the request was handled; otherwise, <c>false</c>.
		/// <para xml:lang="zh-Hans">如果已处理请求则返回 <c>true</c>；否则返回 <c>false</c>。</para>
		/// <para xml:lang="zh-Hant">如果已處理要求則傳回 <c>true</c>；否則傳回 <c>false</c>。</para>
		/// </returns>
		public virtual bool OnRequest (ITileRequest req) { return false; }
		/// <summary>
		/// Occurs when the tile UI element raises the WPF <c>Loaded</c> event.
		/// <para xml:lang="zh-Hans">当磁贴 UI 元素触发 WPF 的 <c>Loaded</c> 事件时发生。</para>
		/// <para xml:lang="zh-Hant">當磁貼 UI 元素觸發 WPF 的 <c>Loaded</c> 事件時發生。</para>
		/// </summary>
		public event EventHandler Load;
		/// <summary>
		/// Occurs when the tile UI element raises the WPF <c>Unloaded</c> event. Do not clean up UI here, because this event may also be triggered when dragging the tile.
		/// <para xml:lang="zh-Hans">当磁贴 UI 元素触发 WPF 的 <c>Unloaded</c> 事件时发生。请勿在此处清理 UI，因为拖拽磁贴时也可能会触发此事件。</para>
		/// <para xml:lang="zh-Hant">當磁貼 UI 元素觸發 WPF 的 <c>Unloaded</c> 事件時發生。請勿在此處清理 UI，因為拖曳磁貼時也可能會觸發此事件。</para>
		/// </summary>
		public event EventHandler Unload;
		/// <summary>
		/// Occurs when the tile size changes, including the overall size and the content area size.
		/// <para xml:lang="zh-Hans">当磁贴尺寸改变时发生，包括整体尺寸和内容区尺寸。</para>
		/// <para xml:lang="zh-Hant">當磁貼尺寸變更時發生，包括整體尺寸和內容區尺寸。</para>
		/// </summary>
		public event EventHandler<SizeChangedEventArgs> Resize;
		/// <summary>
		/// Occurs when the flyout size changes.
		/// <para xml:lang="zh-Hans">当浮出窗口尺寸改变时发生。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗尺寸變更時發生。</para>
		/// </summary>
		public event EventHandler<SizeChangedEventArgs> FlyoutResize;
		/// <summary>
		/// Occurs when the flyout is initialized. Use this event to populate the flyout content.
		/// <para xml:lang="zh-Hans">当浮出窗口初始化时发生。可使用此事件填充浮出窗口内容。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗初始化時發生。可使用此事件填入浮出視窗內容。</para>
		/// </summary>
		public event EventHandler<FlyoutAboutEventArgs> FlyoutInit;
		/// <summary>
		/// Occurs when the flyout is shown (its <c>Loaded</c> event).
		/// <para xml:lang="zh-Hans">当浮出窗口显示时发生（即其 <c>Loaded</c> 事件）。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗顯示時發生（即其 <c>Loaded</c> 事件）。</para>
		/// </summary>
		public event EventHandler<FlyoutAboutEventArgs> FlyoutShow;
		/// <summary>
		/// Occurs when the flyout is about to close.
		/// <para xml:lang="zh-Hans">当浮出窗口即将关闭时发生。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗即將關閉時發生。</para>
		/// </summary>
		public event EventHandler<FlyoutAboutEventArgs> FlyoutClosing;
		/// <summary>
		/// Occurs when the flyout has closed.
		/// <para xml:lang="zh-Hans">当浮出窗口已关闭时发生。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗已關閉時發生。</para>
		/// </summary>
		public event EventHandler FlyoutClosed;
		/// <summary>
		/// Occurs when the Sidebar direction changes (left or right).
		/// <para xml:lang="zh-Hans">当边栏方向（靠左或靠右）改变时发生。</para>
		/// <para xml:lang="zh-Hant">當邊欄方向（靠左或靠右）變更時發生。</para>
		/// </summary>
		public event EventHandler<SidebarDirectionChangedEventArgs> SidebarDirectionChanged;
		/// <summary>
		/// Occurs when the Sidebar theme changes.
		/// <para xml:lang="zh-Hans">当边栏主题改变时发生。</para>
		/// <para xml:lang="zh-Hant">當邊欄主題變更時發生。</para>
		/// </summary>
		public event EventHandler<ThemeChangedEventArgs> ThemeChanged;
		/// <summary>
		/// Occurs when the properties window is initialized. Use this event to populate the properties content.
		/// <para xml:lang="zh-Hans">当属性窗口初始化时发生。可使用此事件填充属性内容。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗初始化時發生。可使用此事件填入屬性內容。</para>
		/// </summary>
		public event EventHandler<PropertiesAboutEventArgs> PropertiesInit;
		/// <summary>
		/// Occurs when the properties window is shown (its <c>Loaded</c> event).
		/// <para xml:lang="zh-Hans">当属性窗口显示时发生（即其 <c>Loaded</c> 事件）。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗顯示時發生（即其 <c>Loaded</c> 事件）。</para>
		/// </summary>
		public event EventHandler<PropertiesAboutEventArgs> PropertiesShow;
		/// <summary>
		/// Occurs when the properties window is about to close.
		/// <para xml:lang="zh-Hans">当属性窗口即将关闭时发生。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗即將關閉時發生。</para>
		/// </summary>
		public event EventHandler<PropertiesAboutEventArgs> PropertiesClosing;
		/// <summary>
		/// Occurs when the OK button of the properties window is clicked.
		/// <para xml:lang="zh-Hans">当属性窗口的确认按钮被点击时发生。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗的確認按鈕被點擊時發生。</para>
		/// </summary>
		public event EventHandler<PropertiesAboutEventArgs> PropertiesClickOK;
		/// <summary>
		/// Occurs when the Cancel button of the properties window is clicked.
		/// <para xml:lang="zh-Hans">当属性窗口的取消按钮被点击时发生。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗的取消按鈕被點擊時發生。</para>
		/// </summary>
		public event EventHandler<PropertiesAboutEventArgs> PropertiesClickCancel;
		/// <summary>
		/// Occurs when the properties window has closed.
		/// <para xml:lang="zh-Hans">当属性窗口已关闭时发生。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗已關閉時發生。</para>
		/// </summary>
		public event EventHandler PropertiesClosed;
		/// <summary>
		/// Gets the per-user configuration for this tile instance.
		/// <para xml:lang="zh-Hans">获取此磁贴实例的用户个人配置。</para>
		/// <para xml:lang="zh-Hant">取得此磁貼執行個體的使用者個人設定。</para>
		/// </summary>
		public ITileConfig Config { get; private set; } = null;
		/// <summary>
		/// Gets the feature interface used to communicate with Sidebar and other tiles.
		/// <para xml:lang="zh-Hans">获取用于与边栏和其他磁贴通信的功能接口。</para>
		/// <para xml:lang="zh-Hant">取得用於與邊欄和其他磁貼通訊的功能介面。</para>
		/// </summary>
		public ISidebarFeatures Features { get; private set; }
		/// <summary>
		/// Gets or sets the context menu for the tile. The host borrows the menu items when the context menu is opened.
		/// <para xml:lang="zh-Hans">获取或设置磁贴的上下文菜单。宿主在打开上下文菜单时会借用菜单项。</para>
		/// <para xml:lang="zh-Hant">取得或設定磁貼的內容功能表。宿主在開啟內容功能表時會借用功能表項目。</para>
		/// </summary>
		public virtual ContextMenu ContextMenu { get; set; } = null;
	}
	/// <summary>
	/// A helper class that automatically subscribes to all events of a <see cref="TileBase"/> instance and exposes overridable virtual methods, simplifying event-handling code.
	/// <para xml:lang="zh-Hans">一个辅助类，自动订阅 <see cref="TileBase"/> 实例的所有事件并提供可重写的虚方法，简化事件处理代码。</para>
	/// <para xml:lang="zh-Hant">一個輔助類別，自動訂閱 <see cref="TileBase"/> 執行個體的所有事件並提供可覆寫的虛擬方法，簡化事件處理程式碼。</para>
	/// </summary>
	[ComVisible (true)]
	public class TileBaseEventRouter: IDisposable
	{
		/// <summary>
		/// The <see cref="TileBase"/> instance whose events are routed.
		/// <para xml:lang="zh-Hans">被路由事件的 <see cref="TileBase"/> 实例。</para>
		/// <para xml:lang="zh-Hant">被路由事件的 <see cref="TileBase"/> 執行個體。</para>
		/// </summary>
		private TileBase tbase = null;
		/// <summary>
		/// Gets the <see cref="TileBase"/> instance whose events are routed.
		/// <para xml:lang="zh-Hans">获取被路由事件的 <see cref="TileBase"/> 实例。</para>
		/// <para xml:lang="zh-Hant">取得被路由事件的 <see cref="TileBase"/> 執行個體。</para>
		/// </summary>
		protected TileBase Instance => tbase;
		/// <summary>
		/// Initializes a new instance of the <see cref="TileBaseEventRouter"/> class and subscribes to the events of the specified <see cref="TileBase"/>.
		/// <para xml:lang="zh-Hans">初始化 <see cref="TileBaseEventRouter"/> 类的新实例，并订阅指定 <see cref="TileBase"/> 的事件。</para>
		/// <para xml:lang="zh-Hant">初始化 <see cref="TileBaseEventRouter"/> 類別的新執行個體，並訂閱指定 <see cref="TileBase"/> 的事件。</para>
		/// </summary>
		/// <param name="tileBase">The <see cref="TileBase"/> instance to route events from.
		/// <para xml:lang="zh-Hans">要从中路由事件的 <see cref="TileBase"/> 实例。</para>
		/// <para xml:lang="zh-Hant">要從中路由事件的 <see cref="TileBase"/> 執行個體。</para>
		/// </param>
		public TileBaseEventRouter (TileBase tileBase)
		{
			if (tileBase == null)
				throw new ArgumentNullException ("Error: cannot init TileEventRouter because the arg TileBase is null.");
			tbase = tileBase;
			FreeEventHandlers ();
			InitEventHandlers ();
		}
		/// <summary>
		/// Subscribes to all events of the routed <see cref="TileBase"/>.
		/// <para xml:lang="zh-Hans">订阅被路由 <see cref="TileBase"/> 的所有事件。</para>
		/// <para xml:lang="zh-Hant">訂閱被路由 <see cref="TileBase"/> 的所有事件。</para>
		/// </summary>
		private void InitEventHandlers ()
		{
			if (tbase == null) return;
			tbase.FlyoutClosed += FlyoutForm_Closed;
			tbase.FlyoutClosing += FlyoutForm_Closing;
			tbase.FlyoutInit += FlyoutForm_Init;
			tbase.FlyoutResize += FlyoutForm_Resize;
			tbase.FlyoutShow += FlyoutForm_Loaded;
			tbase.PropertiesClickCancel += PropertiesForm_ClickCancel;
			tbase.PropertiesClickOK += PropertiesForm_ClickOK;
			tbase.PropertiesClosed += PropertiesForm_Closed;
			tbase.PropertiesClosing += PropertiesForm_Closing;
			tbase.PropertiesInit += PropertiesForm_Init;
			tbase.PropertiesShow += PropertiesForm_Loaded;
			tbase.Resize += Tile_Resize;
			tbase.SidebarDirectionChanged += Sidebar_DirectionChanged;
			tbase.ThemeChanged += Sidebar_ThemeChanged;
		}
		/// <summary>
		/// Unsubscribes from all events of the routed <see cref="TileBase"/>.
		/// <para xml:lang="zh-Hans">取消订阅被路由 <see cref="TileBase"/> 的所有事件。</para>
		/// <para xml:lang="zh-Hant">取消訂閱被路由 <see cref="TileBase"/> 的所有事件。</para>
		/// </summary>
		private void FreeEventHandlers ()
		{
			if (tbase == null) return;
			tbase.FlyoutClosed -= FlyoutForm_Closed;
			tbase.FlyoutClosing -= FlyoutForm_Closing;
			tbase.FlyoutInit -= FlyoutForm_Init;
			tbase.FlyoutResize -= FlyoutForm_Resize;
			tbase.FlyoutShow -= FlyoutForm_Loaded;
			tbase.PropertiesClickCancel -= PropertiesForm_ClickCancel;
			tbase.PropertiesClickOK -= PropertiesForm_ClickOK;
			tbase.PropertiesClosed -= PropertiesForm_Closed;
			tbase.PropertiesClosing -= PropertiesForm_Closing;
			tbase.PropertiesInit -= PropertiesForm_Init;
			tbase.PropertiesShow -= PropertiesForm_Loaded;
			tbase.Resize -= Tile_Resize;
			tbase.SidebarDirectionChanged -= Sidebar_DirectionChanged;
			tbase.ThemeChanged -= Sidebar_ThemeChanged;
		}
		/// <summary>
		/// Called when the Sidebar theme changes.
		/// <para xml:lang="zh-Hans">当边栏主题改变时调用。</para>
		/// <para xml:lang="zh-Hant">當邊欄主題變更時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The theme change event arguments.
		/// <para xml:lang="zh-Hans">主题改变事件参数。</para>
		/// <para xml:lang="zh-Hant">主題變更事件參數。</para>
		/// </param>
		public virtual void Sidebar_ThemeChanged (object sender, ThemeChangedEventArgs e) { }
		/// <summary>
		/// Called when the Sidebar direction changes.
		/// <para xml:lang="zh-Hans">当边栏方向改变时调用。</para>
		/// <para xml:lang="zh-Hant">當邊欄方向變更時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The direction change event arguments.
		/// <para xml:lang="zh-Hans">方向改变事件参数。</para>
		/// <para xml:lang="zh-Hant">方向變更事件參數。</para>
		/// </param>
		public virtual void Sidebar_DirectionChanged (object sender, SidebarDirectionChangedEventArgs e) { }
		/// <summary>
		/// Called when the tile size changes.
		/// <para xml:lang="zh-Hans">当磁贴尺寸改变时调用。</para>
		/// <para xml:lang="zh-Hant">當磁貼尺寸變更時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The size change event arguments.
		/// <para xml:lang="zh-Hans">尺寸改变事件参数。</para>
		/// <para xml:lang="zh-Hant">尺寸變更事件參數。</para>
		/// </param>
		public virtual void Tile_Resize (object sender, SizeChangedEventArgs e) { }
		/// <summary>
		/// Called when the properties window is loaded.
		/// <para xml:lang="zh-Hans">当属性窗口载入时调用。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗載入時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The properties window event arguments.
		/// <para xml:lang="zh-Hans">属性窗口事件参数。</para>
		/// <para xml:lang="zh-Hant">屬性視窗事件參數。</para>
		/// </param>
		public virtual void PropertiesForm_Loaded (object sender, PropertiesAboutEventArgs e) { }
		/// <summary>
		/// Called when the properties window is initialized.
		/// <para xml:lang="zh-Hans">当属性窗口初始化时调用。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗初始化時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The properties window event arguments.
		/// <para xml:lang="zh-Hans">属性窗口事件参数。</para>
		/// <para xml:lang="zh-Hant">屬性視窗事件參數。</para>
		/// </param>
		public virtual void PropertiesForm_Init (object sender, PropertiesAboutEventArgs e) { }
		/// <summary>
		/// Called when the properties window is about to close.
		/// <para xml:lang="zh-Hans">当属性窗口即将关闭时调用。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗即將關閉時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The properties window event arguments.
		/// <para xml:lang="zh-Hans">属性窗口事件参数。</para>
		/// <para xml:lang="zh-Hant">屬性視窗事件參數。</para>
		/// </param>
		public virtual void PropertiesForm_Closing (object sender, PropertiesAboutEventArgs e) { }
		/// <summary>
		/// Called when the properties window has closed.
		/// <para xml:lang="zh-Hans">当属性窗口已关闭时调用。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗已關閉時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The event arguments.
		/// <para xml:lang="zh-Hans">事件参数。</para>
		/// <para xml:lang="zh-Hant">事件參數。</para>
		/// </param>
		public virtual void PropertiesForm_Closed (object sender, EventArgs e) { }
		/// <summary>
		/// Called when the OK button of the properties window is clicked.
		/// <para xml:lang="zh-Hans">当属性窗口的确认按钮被点击时调用。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗的確認按鈕被點擊時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The properties window event arguments.
		/// <para xml:lang="zh-Hans">属性窗口事件参数。</para>
		/// <para xml:lang="zh-Hant">屬性視窗事件參數。</para>
		/// </param>
		public virtual void PropertiesForm_ClickOK (object sender, PropertiesAboutEventArgs e) { }
		/// <summary>
		/// Called when the Cancel button of the properties window is clicked.
		/// <para xml:lang="zh-Hans">当属性窗口的取消按钮被点击时调用。</para>
		/// <para xml:lang="zh-Hant">當屬性視窗的取消按鈕被點擊時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The properties window event arguments.
		/// <para xml:lang="zh-Hans">属性窗口事件参数。</para>
		/// <para xml:lang="zh-Hant">屬性視窗事件參數。</para>
		/// </param>
		public virtual void PropertiesForm_ClickCancel (object sender, PropertiesAboutEventArgs e) { }
		/// <summary>
		/// Called when the flyout is loaded.
		/// <para xml:lang="zh-Hans">当浮出窗口载入时调用。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗載入時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The flyout event arguments.
		/// <para xml:lang="zh-Hans">浮出窗口事件参数。</para>
		/// <para xml:lang="zh-Hant">浮出視窗事件參數。</para>
		/// </param>
		public virtual void FlyoutForm_Loaded (object sender, FlyoutAboutEventArgs e) { }
		/// <summary>
		/// Called when the flyout size changes.
		/// <para xml:lang="zh-Hans">当浮出窗口尺寸改变时调用。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗尺寸變更時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The size change event arguments.
		/// <para xml:lang="zh-Hans">尺寸改变事件参数。</para>
		/// <para xml:lang="zh-Hant">尺寸變更事件參數。</para>
		/// </param>
		public virtual void FlyoutForm_Resize (object sender, SizeChangedEventArgs e) { }
		/// <summary>
		/// Called when the flyout is initialized.
		/// <para xml:lang="zh-Hans">当浮出窗口初始化时调用。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗初始化時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The flyout event arguments.
		/// <para xml:lang="zh-Hans">浮出窗口事件参数。</para>
		/// <para xml:lang="zh-Hant">浮出視窗事件參數。</para>
		/// </param>
		public virtual void FlyoutForm_Init (object sender, FlyoutAboutEventArgs e) { }
		/// <summary>
		/// Called when the flyout is about to close.
		/// <para xml:lang="zh-Hans">当浮出窗口即将关闭时调用。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗即將關閉時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The flyout event arguments.
		/// <para xml:lang="zh-Hans">浮出窗口事件参数。</para>
		/// <para xml:lang="zh-Hant">浮出視窗事件參數。</para>
		/// </param>
		public virtual void FlyoutForm_Closing (object sender, FlyoutAboutEventArgs e) { }
		/// <summary>
		/// Called when the flyout has closed.
		/// <para xml:lang="zh-Hans">当浮出窗口已关闭时调用。</para>
		/// <para xml:lang="zh-Hant">當浮出視窗已關閉時呼叫。</para>
		/// </summary>
		/// <param name="sender">The event source.
		/// <para xml:lang="zh-Hans">事件源。</para>
		/// <para xml:lang="zh-Hant">事件來源。</para>
		/// </param>
		/// <param name="e">The event arguments.
		/// <para xml:lang="zh-Hans">事件参数。</para>
		/// <para xml:lang="zh-Hant">事件參數。</para>
		/// </param>
		public virtual void FlyoutForm_Closed (object sender, EventArgs e) { }
		/// <summary>
		/// Indicates whether <see cref="Router_WillDestroy"/> has already run.
		/// <para xml:lang="zh-Hans">指示 <see cref="Router_WillDestroy"/> 是否已运行。</para>
		/// <para xml:lang="zh-Hant">指示 <see cref="Router_WillDestroy"/> 是否已執行。</para>
		/// </summary>
		private bool isWillDestroyRun = false;
		/// <summary>
		/// Indicates whether <see cref="Router_AlreadyDestroy"/> has already run.
		/// <para xml:lang="zh-Hans">指示 <see cref="Router_AlreadyDestroy"/> 是否已运行。</para>
		/// <para xml:lang="zh-Hant">指示 <see cref="Router_AlreadyDestroy"/> 是否已執行。</para>
		/// </summary>
		private bool isAlreadyDestroyRun = false;
		/// <summary>
		/// Called before the router releases its event handlers during disposal.
		/// <para xml:lang="zh-Hans">在释放期间取消事件订阅之前调用。</para>
		/// <para xml:lang="zh-Hant">在處置期間取消事件訂閱之前呼叫。</para>
		/// </summary>
		public virtual void Router_WillDestroy () { }
		/// <summary>
		/// Called after the router has released its event handlers during disposal.
		/// <para xml:lang="zh-Hans">在释放期间取消事件订阅之后调用。</para>
		/// <para xml:lang="zh-Hant">在處置期間取消事件訂閱之後呼叫。</para>
		/// </summary>
		public virtual void Router_AlreadyDestroy () { }
		/// <summary>
		/// Releases the router, unsubscribes from all events, and clears the routed <see cref="TileBase"/> reference. This method is idempotent.
		/// <para xml:lang="zh-Hans">释放路由器，取消所有事件订阅，并清除被路由的 <see cref="TileBase"/> 引用。此方法是幂等的。</para>
		/// <para xml:lang="zh-Hant">釋放路由器，取消所有事件訂閱，並清除被路由的 <see cref="TileBase"/> 參考。此方法是冪等的。</para>
		/// </summary>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public void Dispose ()
		{
			if (!isWillDestroyRun)
			{
				isWillDestroyRun = true;
				try { Router_WillDestroy (); } catch { }
			}
			FreeEventHandlers ();
			if (!isAlreadyDestroyRun)
			{
				isAlreadyDestroyRun = true;
				try { Router_AlreadyDestroy (); } catch { }
			}
			tbase = null;
		}
		/// <summary>
		/// Finalizes the router and calls <see cref="Dispose"/> to release event handlers.
		/// <para xml:lang="zh-Hans">终结路由器并调用 <see cref="Dispose"/> 释放事件处理器。</para>
		/// <para xml:lang="zh-Hant">完成路由器並呼叫 <see cref="Dispose"/> 釋放事件處理常式。</para>
		/// </summary>
		~TileBaseEventRouter ()
		{
			try { Dispose (); } catch { }
		}
	}
}
