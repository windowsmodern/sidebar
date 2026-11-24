using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Newtonsoft.Json;

namespace Sidebar
{
	[ComVisible (true)]
	public class ConfigSection
	{
		private XElement _root;
		private readonly object _lock;
		private readonly Action _saveHandler;

		internal ConfigSection (XElement root, object lockObj, Action saveHandler)
		{
			_root = root;
			_lock = lockObj;
			_saveHandler = saveHandler;
		}

		public XElement Element { get { return _root; } }

		// ---------------- Setting 访问 ----------------

		public object this [string key]
		{
			get
			{
				if (_root == null) return null;
				XElement elem = GetSettingElement (key);
				if (elem == null) return null;
				XAttribute typeAttr = elem.Attribute ("type");
				if (typeAttr == null || string.IsNullOrEmpty (typeAttr.Value)) return null;
				Type type = Type.GetType (typeAttr.Value);
				if (type == null) return null;
				string json = elem.Value;
				if (string.IsNullOrEmpty (json)) return null;
				return JsonConvert.DeserializeObject (json, type);
			}
			set
			{
				if (_root == null)
					throw new InvalidOperationException ("Configuration section is not available.");

				XElement elem = GetSettingElement (key);
				string typeName = value == null
					? typeof (object).AssemblyQualifiedName
					: value.GetType ().AssemblyQualifiedName;
				string json = value == null ? "null" : JsonConvert.SerializeObject (value);

				lock (_lock)
				{
					if (elem == null)
					{
						elem = new XElement ("Setting",
							new XAttribute ("name", key),
							new XAttribute ("type", typeName),
							json);
						_root.Add (elem);
					}
					else
					{
						elem.Attribute ("type").Value = typeName;
						elem.Value = json;
					}
					if (_saveHandler != null) _saveHandler ();
				}
			}
		}

		public T Get<T> (string key, T defaultValue = default (T))
		{
			object val = this [key];
			if (val == null) return defaultValue;
			try { return (T)val; } catch { return defaultValue; }
		}

		public void Set<T> (string key, T value)
		{
			this [key] = value;
		}

		public bool ContainsKey (string key)
		{
			return GetSettingElement (key) != null;
		}

		public bool Remove (string key)
		{
			XElement elem = GetSettingElement (key);
			if (elem == null) return false;
			lock (_lock)
			{
				elem.Remove ();
				if (_saveHandler != null) _saveHandler ();
			}
			return true;
		}

		// ---------------- Section 嵌套 ----------------

		/// <summary>
		/// 获取或创建当前节下的子节（单层）。
		/// </summary>
		public ConfigSection GetSection (string name)
		{
			if (_root == null)
				throw new InvalidOperationException ("Configuration section is not available.");
			if (string.IsNullOrEmpty (name)) return this;

			XElement child = FindChildSection (_root, name);
			if (child == null)
			{
				lock (_lock)
				{
					child = FindChildSection (_root, name);
					if (child == null)
					{
						child = new XElement ("Section", new XAttribute ("name", name));
						_root.Add (child);
						if (_saveHandler != null) _saveHandler ();
					}
				}
			}
			return new ConfigSection (child, _lock, _saveHandler);
		}

		/// <summary>
		/// 获取或创建多层级子节，路径用 / 或 \ 分隔。
		/// 例：GetSectionPath("Appearance/Colors/Theme")
		/// </summary>
		public ConfigSection GetSectionPath (string path)
		{
			if (string.IsNullOrEmpty (path)) return this;
			string [] parts = path.Split (new char [] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
			ConfigSection current = this;
			for (int i = 0; i < parts.Length; i++)
				current = current.GetSection (parts [i]);
			return current;
		}

		/// <summary>
		/// 返回当前节下直接的所有子节。
		/// </summary>
		public ConfigSection [] GetSections ()
		{
			if (_root == null) return new ConfigSection [0];
			List<ConfigSection> list = new List<ConfigSection> ();
			foreach (XElement e in _root.Elements ("Section"))
				list.Add (new ConfigSection (e, _lock, _saveHandler));
			return list.ToArray ();
		}

		/// <summary>
		/// 返回当前节下直接的所有子节名称。
		/// </summary>
		public string [] GetSectionNames ()
		{
			if (_root == null) return new string [0];
			List<string> list = new List<string> ();
			foreach (XElement e in _root.Elements ("Section"))
			{
				XAttribute nameAttr = e.Attribute ("name");
				if (nameAttr != null) list.Add (nameAttr.Value);
			}
			return list.ToArray ();
		}

		public bool HasSection (string name)
		{
			if (_root == null) return false;
			return FindChildSection (_root, name) != null;
		}

		public bool RemoveSection (string name)
		{
			XElement child = FindChildSection (_root, name);
			if (child == null) return false;
			lock (_lock)
			{
				child.Remove ();
				if (_saveHandler != null) _saveHandler ();
			}
			return true;
		}

		// ---------------- 内部辅助 ----------------

		private static XElement FindChildSection (XElement parent, string name)
		{
			if (parent == null) return null;
			foreach (XElement e in parent.Elements ("Section"))
			{
				XAttribute nameAttr = e.Attribute ("name");
				if (nameAttr != null && nameAttr.Value == name) return e;
			}
			return null;
		}

		private XElement GetSettingElement (string key)
		{
			if (_root == null) return null;
			foreach (XElement e in _root.Elements ("Setting"))
			{
				XAttribute nameAttr = e.Attribute ("name");
				if (nameAttr != null && nameAttr.Value == key) return e;
			}
			return null;
		}

		internal void SetRoot (XElement newRoot)
		{
			_root = newRoot;
		}
	}

	[ComVisible (true)]
	public class XmlConfig
	{
		private static readonly object _staticLock = new object ();
		private static XmlConfig _defaultInstance;
		private static string _currentUserName;

		private readonly object _instanceLock = new object ();
		private readonly string _filePath;
		private XDocument _doc;

		public string FilePath { get { return _filePath; } }

		// ---------------- 实例属性（兼容旧用法） ----------------

		public ConfigSection Global
		{
			get { return GetSection ("Global"); }
		}

		public ConfigSection CurrentUser
		{
			get
			{
				string user = _currentUserName;
				if (string.IsNullOrEmpty (user)) return null;
				return GetSection ("Users/" + user);
			}
		}

		// ---------------- 静态访问器 ----------------

		public static ConfigSection GlobalSettings
		{
			get
			{
				if (_defaultInstance == null)
					throw new InvalidOperationException ("XmlConfig not initialized. Call Initialize first.");
				return _defaultInstance.GetSection ("Global");
			}
		}

		public static ConfigSection CurrentUserSettings
		{
			get
			{
				if (_defaultInstance == null)
					throw new InvalidOperationException ("XmlConfig not initialized. Call Initialize first.");
				string user = _currentUserName;
				if (string.IsNullOrEmpty (user)) return null;
				return _defaultInstance.GetSection ("Users/" + user);
			}
		}

		public static string CurrentUserName
		{
			get { return _currentUserName; }
			set
			{
				if (_currentUserName == value) return;
				_currentUserName = value;
				if (_defaultInstance != null && !string.IsNullOrEmpty (value))
					_defaultInstance.GetSection ("Users/" + value);
			}
		}

		public static void Initialize (string path)
		{
			lock (_staticLock)
			{
				_defaultInstance = new XmlConfig (path);
				_currentUserName = Environment.UserName;
				_defaultInstance.GetSection ("Global");
				_defaultInstance.GetSection ("Users/" + _currentUserName);
			}
		}

		// ---------------- 构造与持久化 ----------------

		public XmlConfig (string path)
		{
			if (Directory.Exists (path) || (!File.Exists (path) && !Path.HasExtension (path)))
				_filePath = Path.Combine (path, "Config.xml");
			else
				_filePath = path;

			LoadOrCreate ();
		}

		private void LoadOrCreate ()
		{
			string dir = Path.GetDirectoryName (_filePath);
			if (!string.IsNullOrEmpty (dir) && !Directory.Exists (dir))
				Directory.CreateDirectory (dir);

			if (File.Exists (_filePath))
			{
				_doc = XDocument.Load (_filePath);
			}
			else
			{
				_doc = new XDocument (
					new XElement ("Config",
						new XElement ("Section", new XAttribute ("name", "Global")),
						new XElement ("Section", new XAttribute ("name", "Users"))
					)
				);
				Save ();
			}
		}

		private void Save ()
		{
			lock (_instanceLock)
			{
				_doc.Save (_filePath);
			}
		}

		// ---------------- GetSection 核心 ----------------

		/// <summary>
		/// 按路径获取或创建节。路径用 / 或 \ 分隔，例如：
		///   "Global"
		///   "Users/Bruce"
		///   "Global/Appearance/Colors"
		/// 空路径返回根节。
		/// </summary>
		public ConfigSection GetSection (string path)
		{
			if (_doc == null || _doc.Root == null)
				throw new InvalidOperationException ("XmlConfig is not initialized.");

			XElement current = _doc.Root;
			if (!string.IsNullOrEmpty (path))
			{
				string [] parts = path.Split (new char [] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < parts.Length; i++)
				{
					XElement child = FindChildSection (current, parts [i]);
					if (child == null)
					{
						lock (_instanceLock)
						{
							child = FindChildSection (current, parts [i]);
							if (child == null)
							{
								child = new XElement ("Section", new XAttribute ("name", parts [i]));
								current.Add (child);
								Save ();
							}
						}
					}
					current = child;
				}
			}
			return new ConfigSection (current, _instanceLock, Save);
		}

		/// <summary>
		/// 判断某个路径的节是否存在（不创建）。
		/// </summary>
		public bool HasSection (string path)
		{
			if (_doc == null || _doc.Root == null) return false;
			XElement current = _doc.Root;
			if (!string.IsNullOrEmpty (path))
			{
				string [] parts = path.Split (new char [] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < parts.Length; i++)
				{
					XElement child = FindChildSection (current, parts [i]);
					if (child == null) return false;
					current = child;
				}
			}
			return true;
		}

		/// <summary>
		/// 顶层节名称列表。
		/// </summary>
		public string [] GetSectionNames ()
		{
			if (_doc == null || _doc.Root == null) return new string [0];
			List<string> list = new List<string> ();
			foreach (XElement e in _doc.Root.Elements ("Section"))
			{
				XAttribute nameAttr = e.Attribute ("name");
				if (nameAttr != null) list.Add (nameAttr.Value);
			}
			return list.ToArray ();
		}

		public bool RemoveSection (string path)
		{
			if (_doc == null || _doc.Root == null || string.IsNullOrEmpty (path)) return false;
			string [] parts = path.Split (new char [] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length == 0) return false;

			XElement parent = _doc.Root;
			for (int i = 0; i < parts.Length - 1; i++)
			{
				XElement child = FindChildSection (parent, parts [i]);
				if (child == null) return false;
				parent = child;
			}

			XElement target = FindChildSection (parent, parts [parts.Length - 1]);
			if (target == null) return false;

			lock (_instanceLock)
			{
				target.Remove ();
				Save ();
			}
			return true;
		}

		private static XElement FindChildSection (XElement parent, string name)
		{
			if (parent == null) return null;
			foreach (XElement e in parent.Elements ("Section"))
			{
				XAttribute nameAttr = e.Attribute ("name");
				if (nameAttr != null && nameAttr.Value == name) return e;
			}
			return null;
		}
	}
}