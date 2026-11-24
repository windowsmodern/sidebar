using System.Runtime.InteropServices;
using Microsoft.Win32.Init;

namespace Sidebar
{
	[ComVisible (true)]
	public interface IProgramFolder
	{
		string FolderPath { get; }
		bool Exists { get; }
		bool EnsureExists ();
		ILocaleResources StringResources { get; }
		IPathResources FileResources { get; }
		InitConfig InitConfig { get; }
		XmlConfig XmlConfig { get; }
	}
}
