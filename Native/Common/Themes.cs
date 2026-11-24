using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace Sidebar
{
	[ComVisible (true)]
	public interface ITheme
	{
		/// <summary>主题文件夹的绝对路径。</summary>
		string FolderPath { get; }
		/// <summary>主题主文件（Theme.xaml）的绝对路径。</summary>
		string ThemeMainFile { get; }
		/// <summary>主题的文件夹名（不含路径）。</summary>
		string ThemeName { get; }
		/// <summary>已加载的资源字典。</summary>
		ResourceDictionary ResourceDictionary { get; }
		/// <summary>
		/// 主题显示名称。
		/// </summary>
		string DisplayName { get; }
		/// <summary>发布者显示名称</summary>
		string PublisherDisplayName { get; }
		/// <summary>主题描述</summary>
		string Description { get; }
		/// <summary>Logo 的绝对路径（已解析，文件存在）。可能为空。</summary>
		string Logo { get; }
		/// <summary>已加载的 Logo 图像，可能为空。</summary>
		ImageSource LogoImage { get; }
		/// <summary>Locale.xml 提供的本地化字符串资源。</summary>
		ILocaleResources StringResources { get; }
		/// <summary>Path.xml 提供的 DPI 相关文件资源。</summary>
		IPathResources FileResources { get; }
	}
}
