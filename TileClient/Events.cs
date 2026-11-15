using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Sidebar
{
	/// <summary>
	/// Provides data for events that report size changes, including both pixel sizes and WPF sizes.
	/// <para xml:lang="zh-Hans">为报告尺寸变化的事件提供数据，包括像素尺寸和 WPF 尺寸。</para>
	/// <para xml:lang="zh-Hant">為報告尺寸變更的事件提供資料，包括像素尺寸和 WPF 尺寸。</para>
	/// </summary>
	[ComVisible (true)]
	public class SizeChangedEventArgs: EventArgs
	{
		/// <summary>
		/// Gets the overall size in pixels.
		/// <para xml:lang="zh-Hans">获取整体尺寸（像素）。</para>
		/// <para xml:lang="zh-Hant">取得整體尺寸（像素）。</para>
		/// </summary>
		public System.Drawing.Size NowSize { get; }
		/// <summary>
		/// Gets the client (content) size in pixels.
		/// <para xml:lang="zh-Hans">获取客户区（内容区）尺寸（像素）。</para>
		/// <para xml:lang="zh-Hant">取得工作區（內容區）尺寸（像素）。</para>
		/// </summary>
		public System.Drawing.Size NowClientSize { get; }
		/// <summary>
		/// Gets the overall size in WPF units.
		/// <para xml:lang="zh-Hans">获取整体尺寸（WPF 单位）。</para>
		/// <para xml:lang="zh-Hant">取得整體尺寸（WPF 單位）。</para>
		/// </summary>
		public System.Windows.Size WpfNowSize { get; }
		/// <summary>
		/// Gets the client (content) size in WPF units.
		/// <para xml:lang="zh-Hans">获取客户区（内容区）尺寸（WPF 单位）。</para>
		/// <para xml:lang="zh-Hant">取得工作區（內容區）尺寸（WPF 單位）。</para>
		/// </summary>
		public System.Windows.Size WpfNowClientSize { get; }
		/// <summary>
		/// Initializes a new instance of the <see cref="SizeChangedEventArgs"/> class.
		/// <para xml:lang="zh-Hans">初始化 <see cref="SizeChangedEventArgs"/> 类的新实例。</para>
		/// <para xml:lang="zh-Hant">初始化 <see cref="SizeChangedEventArgs"/> 類別的新執行個體。</para>
		/// </summary>
		/// <param name="size">The overall size in pixels.
		/// <para xml:lang="zh-Hans">整体尺寸（像素）。</para>
		/// <para xml:lang="zh-Hant">整體尺寸（像素）。</para>
		/// </param>
		/// <param name="clientSize">The client size in pixels.
		/// <para xml:lang="zh-Hans">客户区尺寸（像素）。</para>
		/// <para xml:lang="zh-Hant">工作區尺寸（像素）。</para>
		/// </param>
		/// <param name="wpfNowSize">The overall size in WPF units.
		/// <para xml:lang="zh-Hans">整体尺寸（WPF 单位）。</para>
		/// <para xml:lang="zh-Hant">整體尺寸（WPF 單位）。</para>
		/// </param>
		/// <param name="wpfNowClientSize">The client size in WPF units.
		/// <para xml:lang="zh-Hans">客户区尺寸（WPF 单位）。</para>
		/// <para xml:lang="zh-Hant">工作區尺寸（WPF 單位）。</para>
		/// </param>
		public SizeChangedEventArgs (System.Drawing.Size size, System.Drawing.Size clientSize, System.Windows.Size wpfNowSize, System.Windows.Size wpfNowClientSize)
		{
			NowSize = size;
			NowClientSize = clientSize;
			WpfNowSize = wpfNowSize;
			WpfNowClientSize = wpfNowClientSize;
		}
	}
	/// <summary>
	/// Provides access to a window and its client area panel.
	/// <para xml:lang="zh-Hans">提供对窗口及其客户区面板的访问。</para>
	/// <para xml:lang="zh-Hant">提供對視窗及其工作區面板的存取。</para>
	/// </summary>
	[ComVisible (true)]
	public interface IUnionWindowProvider
	{
		/// <summary>
		/// Gets the window object.
		/// <para xml:lang="zh-Hans">获取窗口对象。</para>
		/// <para xml:lang="zh-Hant">取得視窗物件。</para>
		/// </summary>
		Window Window { get; }
		/// <summary>
		/// Gets the client area panel of the window.
		/// <para xml:lang="zh-Hans">获取窗口的客户区面板。</para>
		/// <para xml:lang="zh-Hant">取得視窗的工作區面板。</para>
		/// </summary>
		Panel ClientArea { get; }
	}
	/// <summary>
	/// Provides data for flyout-related events, including the flyout window and its content panel.
	/// <para xml:lang="zh-Hans">为浮出窗口相关事件提供数据，包括浮出窗口及其内容面板。</para>
	/// <para xml:lang="zh-Hant">為浮出視窗相關事件提供資料，包括浮出視窗及其內容面板。</para>
	/// </summary>
	[ComVisible (true)]
	public class FlyoutAboutEventArgs: EventArgs, IUnionWindowProvider
	{
		/// <summary>
		/// Gets the flyout window object.
		/// <para xml:lang="zh-Hans">获取浮出窗口对象。</para>
		/// <para xml:lang="zh-Hant">取得浮出視窗物件。</para>
		/// </summary>
		public UIElement FlyoutWindow { get; }
		/// <summary>
		/// Gets the content panel of the flyout window, to which developers add custom UI controls.
		/// <para xml:lang="zh-Hans">获取浮出窗口的内容面板，开发者可向其添加自定义 UI 控件。</para>
		/// <para xml:lang="zh-Hant">取得浮出視窗的內容面板，開發人員可向其新增自訂 UI 控制項。</para>
		/// </summary>
		public UIElement FlyoutUI { get; }
		/// <summary>
		/// Gets the flyout window as a <see cref="Window"/> instance, or <c>null</c> if it is not a window.
		/// <para xml:lang="zh-Hans">获取作为 <see cref="Window"/> 实例的浮出窗口；如果不是窗口则返回 <c>null</c>。</para>
		/// <para xml:lang="zh-Hant">取得作為 <see cref="Window"/> 執行個體的浮出視窗；如果不是視窗則傳回 <c>null</c>。</para>
		/// </summary>
		public Window Window => FlyoutWindow as Window;
		/// <summary>
		/// Gets the flyout content as a <see cref="Panel"/> instance, or <c>null</c> if it is not a panel.
		/// <para xml:lang="zh-Hans">获取作为 <see cref="Panel"/> 实例的浮出内容；如果不是面板则返回 <c>null</c>。</para>
		/// <para xml:lang="zh-Hant">取得作為 <see cref="Panel"/> 執行個體的浮出內容；如果不是面板則傳回 <c>null</c>。</para>
		/// </summary>
		public Panel ClientArea => FlyoutUI as Panel;
		/// <summary>
		/// Gets the flyout window features interface, or <c>null</c> if the flyout window does not implement <see cref="IFlyoutToolMembers"/>.
		/// <para xml:lang="zh-Hans">获取浮出窗口功能接口；如果浮出窗口未实现 <see cref="IFlyoutToolMembers"/> 则返回 <c>null</c>。</para>
		/// <para xml:lang="zh-Hant">取得浮出視窗功能介面；如果浮出視窗未實作 <see cref="IFlyoutToolMembers"/> 則傳回 <c>null</c>。</para>
		/// </summary>
		public IFlyoutToolMembers FlyoutWindowFeatures => FlyoutWindow as IFlyoutToolMembers;
		/// <summary>
		/// Initializes a new instance of the <see cref="FlyoutAboutEventArgs"/> class.
		/// <para xml:lang="zh-Hans">初始化 <see cref="FlyoutAboutEventArgs"/> 类的新实例。</para>
		/// <para xml:lang="zh-Hant">初始化 <see cref="FlyoutAboutEventArgs"/> 類別的新執行個體。</para>
		/// </summary>
		/// <param name="flyoutUI">The content panel of the flyout window.
		/// <para xml:lang="zh-Hans">浮出窗口的内容面板。</para>
		/// <para xml:lang="zh-Hant">浮出視窗的內容面板。</para>
		/// </param>
		/// <param name="flyoutWindow">The flyout window object.
		/// <para xml:lang="zh-Hans">浮出窗口对象。</para>
		/// <para xml:lang="zh-Hant">浮出視窗物件。</para>
		/// </param>
		public FlyoutAboutEventArgs (UIElement flyoutUI, UIElement flyoutWindow)
		{
			FlyoutUI = flyoutUI;
			FlyoutWindow = flyoutWindow;
		}
	}
	/// <summary>
	/// Provides data for the event that reports a change in the Sidebar docking direction.
	/// <para xml:lang="zh-Hans">为报告边栏停靠方向变化的事件提供数据。</para>
	/// <para xml:lang="zh-Hant">為報告邊欄停駐方向變更的事件提供資料。</para>
	/// </summary>
	[ComVisible (true)]
	public class SidebarDirectionChangedEventArgs: EventArgs
	{
		/// <summary>
		/// Gets the new docking direction of the Sidebar.
		/// <para xml:lang="zh-Hans">获取边栏新的停靠方向。</para>
		/// <para xml:lang="zh-Hant">取得邊欄新的停駐方向。</para>
		/// </summary>
		public SidebarDirection Direction { get; }
		/// <summary>
		/// Initializes a new instance of the <see cref="SidebarDirectionChangedEventArgs"/> class.
		/// <para xml:lang="zh-Hans">初始化 <see cref="SidebarDirectionChangedEventArgs"/> 类的新实例。</para>
		/// <para xml:lang="zh-Hant">初始化 <see cref="SidebarDirectionChangedEventArgs"/> 類別的新執行個體。</para>
		/// </summary>
		/// <param name="direct">The new docking direction of the Sidebar.
		/// <para xml:lang="zh-Hans">边栏新的停靠方向。</para>
		/// <para xml:lang="zh-Hant">邊欄新的停駐方向。</para>
		/// </param>
		public SidebarDirectionChangedEventArgs (SidebarDirection direct)
		{
			Direction = direct;
		}
	}
	/// <summary>
	/// Provides data for the event that reports a change in the Sidebar theme.
	/// <para xml:lang="zh-Hans">为报告边栏主题变化的事件提供数据。</para>
	/// <para xml:lang="zh-Hant">為報告邊欄主題變更的事件提供資料。</para>
	/// </summary>
	[ComVisible (true)]
	public class ThemeChangedEventArgs: EventArgs
	{
		/// <summary>
		/// Gets the name of the new theme (the folder name under the Themes directory, not the display name).
		/// <para xml:lang="zh-Hans">获取新主题的名称（Themes 目录下的文件夹名，而非显示名称）。</para>
		/// <para xml:lang="zh-Hant">取得新主題的名稱（Themes 目錄下的資料夾名稱，而非顯示名稱）。</para>
		/// </summary>
		public string ThemeName { get; }
		/// <summary>
		/// Gets the new theme object.
		/// <para xml:lang="zh-Hans">获取新的主题对象。</para>
		/// <para xml:lang="zh-Hant">取得新的主題物件。</para>
		/// </summary>
		public ITheme Theme { get; }
		/// <summary>
		/// Initializes a new instance of the <see cref="ThemeChangedEventArgs"/> class.
		/// <para xml:lang="zh-Hans">初始化 <see cref="ThemeChangedEventArgs"/> 类的新实例。</para>
		/// <para xml:lang="zh-Hant">初始化 <see cref="ThemeChangedEventArgs"/> 類別的新執行個體。</para>
		/// </summary>
		/// <param name="themeName">The name of the new theme.
		/// <para xml:lang="zh-Hans">新主题的名称。</para>
		/// <para xml:lang="zh-Hant">新主題的名稱。</para>
		/// </param>
		/// <param name="theme">The new theme object.
		/// <para xml:lang="zh-Hans">新的主题对象。</para>
		/// <para xml:lang="zh-Hant">新的主題物件。</para>
		/// </param>
		public ThemeChangedEventArgs (string themeName, ITheme theme)
		{
			ThemeName = themeName;
			Theme = theme;
		}
	}
	/// <summary>
	/// Provides data for properties window-related events, including the properties window and its content panel.
	/// <para xml:lang="zh-Hans">为属性窗口相关事件提供数据，包括属性窗口及其内容面板。</para>
	/// <para xml:lang="zh-Hant">為屬性視窗相關事件提供資料，包括屬性視窗及其內容面板。</para>
	/// </summary>
	[ComVisible (true)]
	public class PropertiesAboutEventArgs: EventArgs
	{
		/// <summary>
		/// Gets the properties window object.
		/// <para xml:lang="zh-Hans">获取属性窗口对象。</para>
		/// <para xml:lang="zh-Hant">取得屬性視窗物件。</para>
		/// </summary>
		public UIElement PropertiesWindow { get; }
		/// <summary>
		/// Gets the content panel of the properties window, to which developers add custom UI controls.
		/// <para xml:lang="zh-Hans">获取属性窗口的内容面板，开发者可向其添加自定义 UI 控件。</para>
		/// <para xml:lang="zh-Hant">取得屬性視窗的內容面板，開發人員可向其新增自訂 UI 控制項。</para>
		/// </summary>
		public UIElement PropertiesContent { get; }
		/// <summary>
		/// Gets the properties window as a <see cref="Window"/> instance, or <c>null</c> if it is not a window.
		/// <para xml:lang="zh-Hans">获取作为 <see cref="Window"/> 实例的属性窗口；如果不是窗口则返回 <c>null</c>。</para>
		/// <para xml:lang="zh-Hant">取得作為 <see cref="Window"/> 執行個體的屬性視窗；如果不是視窗則傳回 <c>null</c>。</para>
		/// </summary>
		public Window Window => PropertiesWindow as Window;
		/// <summary>
		/// Gets the properties content as a <see cref="Panel"/> instance, or <c>null</c> if it is not a panel.
		/// <para xml:lang="zh-Hans">获取作为 <see cref="Panel"/> 实例的属性内容；如果不是面板则返回 <c>null</c>。</para>
		/// <para xml:lang="zh-Hant">取得作為 <see cref="Panel"/> 執行個體的屬性內容；如果不是面板則傳回 <c>null</c>。</para>
		/// </summary>
		public Panel ClientArea => PropertiesContent as Panel;
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertiesAboutEventArgs"/> class.
		/// <para xml:lang="zh-Hans">初始化 <see cref="PropertiesAboutEventArgs"/> 类的新实例。</para>
		/// <para xml:lang="zh-Hant">初始化 <see cref="PropertiesAboutEventArgs"/> 類別的新執行個體。</para>
		/// </summary>
		/// <param name="propertiesWindow">The properties window object.
		/// <para xml:lang="zh-Hans">属性窗口对象。</para>
		/// <para xml:lang="zh-Hant">屬性視窗物件。</para>
		/// </param>
		/// <param name="propertiesContent">The content panel of the properties window.
		/// <para xml:lang="zh-Hans">属性窗口的内容面板。</para>
		/// <para xml:lang="zh-Hant">屬性視窗的內容面板。</para>
		/// </param>
		public PropertiesAboutEventArgs (UIElement propertiesWindow, UIElement propertiesContent)
		{
			PropertiesWindow = propertiesWindow;
			PropertiesContent = propertiesContent;
		}
	}
}
