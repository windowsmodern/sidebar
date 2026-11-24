using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Sidebar
{
	[ComVisible (true)]
	public interface ILocaleResource: IDictionary<string, string>
	{
		/// <summary>
		/// Get the most suitable language; it will always return a value unless there is no value available.
		/// </summary>
		/// <param name="fallback">The value returned when there is no value available.</param>
		/// <param name="localeName">The desired language to retrieve. Setting it to null indicates that the language used by the program is being retrieved.</param>
		/// <returns></returns>
		string SuitableValue (string fallback = "", string localeName = null);
		/// <summary>
		/// Compared to the SuitableValue method, this function cleans up unused languages, reducing memory usage. It only retains the following languages:
		/// - The expected language you set (optional)
		/// - The language the program is currently using
		/// - English (only en-US will be retained; if not found, en will be retained; otherwise, other English languages ​​will be retained)
		///If there is only one resource, that resource will not be cleaned up.
		///The effects of this function are irreversible.The resource object will only be recreated by reopening the file.
		/// </summary>
		/// <param name="localeName">The language you hope to retain</param>
		void CleanRedundantValues (string localeName = null);
	}
	[ComVisible (true)]
	public interface ILocaleResources: IDictionary<string, ILocaleResource>
	{
		IDictionary<string, ILocaleResource> AllResources { get; }
		ILocaleResource AllValues (string resourceName);
		/// <summary>
		/// Get the most suitable language; it will always return a value unless there is no value available.
		/// </summary>
		/// <param name="resName">The name of the resource to query.</param>
		/// <param name="fallback">The value returned when there is no value available.</param>
		/// <param name="localeName">The desired language to retrieve. Setting it to null indicates that the language used by the program is being retrieved.</param>
		/// <returns></returns>
		string SuitableResource (string resName, string fallback = "", string localeName = null);
		/// <summary>
		/// Compared to the SuitableValue method, this function cleans up unused languages, reducing memory usage. It only retains the following languages:
		/// - The expected language you set (optional).
		/// - The language the program is currently using.
		/// - English (only en-US will be retained; if not found, en will be retained; otherwise, other English languages ​​will be retained).
		///If there is only one resource, that resource will not be cleaned up.
		///The effects of this function are irreversible.The resource object will only be recreated by reopening the file.
		/// </summary>
		/// <param name="localeName">The language you hope to retain</param>
		void CleanRedundantValues (string localeName = null);
	}
	[ComVisible (true)]
	public interface IPathResource: IDictionary<int, string>
	{
		/// <summary>
		/// Gets the most suitable path string for the current DPI context.
		/// This method always returns a value unless no resources are available.
		/// </summary>
		/// <param name="fallback">
		/// The string to return if no matching DPI scale is found. Defaults to an empty string.
		/// </param>
		/// <param name="dpiScale">
		/// The DPI scaling percentage to retrieve (e.g., 125 for 125%).
		/// If set to <c>-1</c>, the current system DPI scale is used automatically.
		/// </param>
		/// <returns>
		/// The path string that best matches the requested DPI scale, or the fallback value.
		/// </returns>
		string SuitableValue (string fallback = "", int dpiScale = -1);
		/// <summary>
		/// Removes all DPI-scale entries except those that are likely to be needed,
		/// in order to reduce memory usage. The retained scales are:
		/// - The DPI scale explicitly specified by the <paramref name="dpiScale"/> parameter
		///   (if it is not -1).
		/// - The current system DPI scale (if <paramref name="dpiScale"/> is -1, this is
		///   the scale that will be used to select which entry to keep; otherwise, it is kept separately).
		/// - The 100 (100%) scale, which serves as a baseline fallback.
		/// If the resource contains only one entry, it will never be cleaned, regardless of the above rules.
		/// This operation is irreversible. To restore removed entries, you must reload the resource from its source.
		/// </summary>
		/// <param name="dpiScale">
		/// The DPI scale you explicitly want to preserve.
		/// If -1, no extra scale is forced to be kept beyond the current system scale and 100%.
		/// </param>
		void CleanRedundantValues (int dpiScale = -1);
	}
	[ComVisible (true)]
	public interface IPathResources: IDictionary<string, IPathResource>
	{
		IDictionary<string, IPathResource> AllResources { get; }
		IPathResource AllValues (string resourceName);
		/// <summary>
		/// Gets the most suitable path string from the named resource, based on the current DPI context.
		/// This method always returns a value unless no resources are available for that name.
		/// </summary>
		/// <param name="resName">The name of the resource to query.</param>
		/// <param name="fallback">
		/// The string to return if no matching DPI scale is found. Defaults to an empty string.
		/// </param>
		/// <param name="scale">
		/// The DPI scaling percentage to retrieve (e.g., 125). If set to -1, the current system scale is used.
		/// </param>
		/// <returns>The best matching path string, or the fallback value.</returns>
		string SuitableResource (string resName, string fallback = "", int scale = -1);
		/// <summary>
		/// Cleans up redundant DPI-scale entries across all resources, retaining only the most relevant scales.
		/// The retention rules are the same as those defined in <see cref="IPathResource.CleanRedundantValues"/>.
		/// If a resource has only one entry, it will not be cleaned.
		/// This operation is irreversible. Removed entries can only be restored by reloading the source files.
		/// </summary>
		/// <param name="dpiScale">
		/// The DPI scale to explicitly preserve. If -1, only the current system scale and 100% are kept.
		/// </param>
		void CleanRedundantValues (int dpiScale = -1);
	}
}
