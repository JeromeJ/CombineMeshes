using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
///     Auto-create an architecture from a directory template.
///     Default template brings extra goodies.
/// </summary>
/// <remarks>
///     Naming convetion:
///         PublicMemberVariable
///         _PrivateMemberVariable
///         ...
///         localVariable
///         _parameter
/// </remarks>

namespace ImportNinja
{
	public class ImportNinja : ScriptableObject
	{
		public string SceneFolder;

		public bool EnableDebug = true;

		public Dictionary<string, string> Vars = new Dictionary<string, string>();

		// STATIC //

		/// <summary>
		///     Name of config asset
		/// </summary>
		static public string NinjaData { get { return _NinjaData; } }

		/// <summary>
		///     Hardcoded path where to auto-save first scene
		/// </summary>
		static public string SceneFolderName { get { return _SceneFolderName; } }

		/// <summary>
		///     Name matching to find EditorTemplate
		/// </summary>
		static public string EditorTemplate { get { return _EditorTemplate; } }

		/// <summary>
		///     Hardcoded path to ImportNinja directory template
		/// </summary>
		static public string ImportNinjaDirectory { get { return _ImportNinjaDirectory; } }

		/// <summary>
		///     Singleton (pulled from resources)
		/// </summary>
		static public ImportNinja Data
		{
			set { _Data = value; }
			get
			{
				if (_Data == null)
					_Data = Resources.Load<ImportNinja>(_NinjaData);

				return _Data;
			}
		}

		static private ImportNinja _Data;
		static protected string _NinjaData = "_ninjaData";
		static protected string _SceneFolderName = @"Assets\__#PROJECTNAME#\Scenes\";
		static protected string _EditorTemplate = "Editor.ninjatemplate.cs.txt";
		static protected string _ImportNinjaDirectory = @"#UNITYDIR#\Data\Resources\ScriptTemplates\ImportNinja";

#if UNITY_EDITOR
		[MenuItem("Window/ImportNinja config")]
		public static void ShowConfig()
		{
			Selection.activeObject = Resources.Load(_NinjaData);
		}
#endif
    }

#if UNITY_EDITOR

	[InitializeOnLoad]
	public class ImportNinjaInitializer : ImportNinja
	{
		static ImportNinjaInitializer()
		{
			NinjaDebug("Your Personal Ninja");

			if (ImportNinja.Data != null)
				return;

			ImportNinja.Data = ScriptableObject.CreateInstance<ImportNinja>();
			ImportNinja data = ImportNinja.Data;

			DirectoryInfo projectDirectory     = Directory.GetParent(Application.dataPath);

			data.Vars.Add("PROJECTNAME",    projectDirectory.Name);
			data.Vars.Add("PROJECTDIR",     projectDirectory.FullName);
			data.Vars.Add("UNITYDIR",       Directory.GetParent(EditorApplication.applicationPath).FullName);

			ParseStatics(data.Vars);

			// Save to ScriptableObject
			data.SceneFolder = ImportNinja.SceneFolderName;

			CopyDirectoryFromTemplate(
				_from:  ImportNinja.ImportNinjaDirectory + @"\Template",
				_to:    projectDirectory.FullName,
				_vars:  data.Vars
			);

			SaveScenes();
            ImportPackage();
		}

        private static void ImportPackage()
        {
            AssetDatabase.importPackageCompleted += AssetDatabase_importPackageCompleted;
            AssetDatabase.ImportPackage(ImportNinja.ImportNinjaDirectory + @"\DefaultAssets.unitypackage", interactive: false);
        }

        private static void AssetDatabase_importPackageCompleted(string packageName)
        {
            Debug.Log("Package imported! " + packageName);
            //Resources.Load<ImportNinja.AssetFile>

            AssetDatabase.importPackageCompleted -= AssetDatabase_importPackageCompleted;
        }

        private static void SaveScenes()
		{
			// You can only have one untilted scene opened at once
			// So no need to handle multiple scenes (undesired behaviours occur)
			EditorSceneManager.SaveScene(
				SceneManager.GetSceneAt(0),
				Path.Combine(ImportNinja.SceneFolderName, "Main.unity")
			);

			AssetDatabase.Refresh();
		}

		/// <summary>
		///     You can disable these messages from showing in the console in Window > ImportNinja Config > EnableDebug
		/// </summary>
		private static void NinjaDebug(string _msg)
		{
			if (ImportNinja.Data != null && ImportNinja.Data.EnableDebug)
				Debug.Log(_msg);
		}

		private static void ParseStatics(Dictionary<string, string> vars)
		{
			_SceneFolderName        = Parse(SceneFolderName,      vars);
			_EditorTemplate         = Parse(EditorTemplate,       vars);
			_NinjaData              = Parse(NinjaData,            vars);
			_ImportNinjaDirectory   = Parse(ImportNinjaDirectory, vars);
		}

		private static string Parse(string _str, Dictionary<string, string> _vars)
		{
			foreach (KeyValuePair<string, string> rr in _vars)
				_str = _str.Replace("#" + rr.Key + "#", rr.Value);

			return _str;
		}

		private static void CopyDirectoryFromTemplate(string _from, string _to, Dictionary<string, string> _vars)
		{
			if (ImportNinja.Data == null)
			{
				Debug.LogError("Tried calling \"" + MethodBase.GetCurrentMethod().Name + "\" without ImportNinja.Data being initialized!");
				return;
			}

			foreach (EditablePathInfo node in DirectoryCopy(_from, _to, _copySubDirs: true))
			{
				node.NodeName = Parse(node.NodeName, _vars);

				string nodeName = node.NodeName;

                if (nodeName == ImportNinja.NinjaData)
                {
                    AssetDatabase.CreateAsset(
                        ImportNinja.Data,
                        node.RelativePath + ".asset"
                    );

                    node.DontSave = true;
                }
                else if (nodeName.EndsWith(".dist"))
                    node.NodeName = node.NodeName.Substring(0, node.NodeName.Length - ".dist".Length);
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		/// <summary>
		///     Source: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
		///     Modified by JeromeJ.
		/// </summary>
		/// <param name="vars"></param>

		private static IEnumerable<EditablePathInfo> DirectoryCopy(string _sourceDirName, string _destDirName, bool _copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(_sourceDirName);

			if (!dir.Exists)
				// throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
				yield break;

			DirectoryInfo[] dirs = dir.GetDirectories();
        
			// If the destination directory doesn't exist, create it.
			Directory.CreateDirectory(_destDirName);

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				EditablePathInfo path = new EditablePathInfo(Path.Combine(_destDirName, file.Name));

				yield return path; // By reference

				if (path.DontSave)
					continue;

				try
				{
					file.CopyTo(path.FullPath);
				}
				catch (IOException)
				{

				}
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (_copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					EditablePathInfo path = new EditablePathInfo(Path.Combine(_destDirName, subdir.Name));

					yield return path; // By reference

					if (path.DontSave)
						continue;

					foreach (EditablePathInfo node in DirectoryCopy(subdir.FullName, path.FullPath, _copySubDirs))
					{
						// Can be modified as well before execution resumes
						yield return node;
					}
				}
			}
		}
	}

	public class EditablePathInfo
	{
		public bool DontSave = false;
		public string FullPath;

		public string NodeName
		{
			get { return Path.GetFileName(FullPath); }
			set {
				FullPath = Path.Combine(Path.GetDirectoryName(FullPath), value);
			}
		}

		public string RelativePath
		{
			get { return new Uri(Application.dataPath).MakeRelativeUri(new Uri(FullPath)).ToString(); }
		}

		public EditablePathInfo(string _fullPath)
		{
			FullPath = _fullPath;
		}
	}

	#endif
}