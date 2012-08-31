using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Tenebrous.EditorEnhancements
{
	[InitializeOnLoad]
	public static class TeneProjectWindow
	{
		private static Dictionary<string, Color> _colorMap;
		private static Dictionary<string, int> _fileCount = new Dictionary<string, int>();
		private static string _basePath;

		private static bool _showAll;

		static TeneProjectWindow()
		{
			EditorApplication.projectWindowItemOnGUI += Draw;

			_basePath = Application.dataPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

			_colorMap = new Dictionary<string, Color>()
				            {
					            {"png", new Color(0.8f, 0.8f, 1.0f)},
					            {"psd", new Color(0.5f, 0.8f, 1.0f)},

					            {"cs", new Color(0.5f, 1.0f, 0.5f)},
					            {"js", new Color(0.8f, 1.0f, 0.8f)},

					            {"mat", new Color(1.0f, 0.8f, 0.8f)},
					            {"shader", new Color(1.0f, 0.5f, 0.5f)},

					            {"wav", new Color(0.8f, 0.4f, 1.0f)},
					            {"mp3", new Color(0.8f, 0.4f, 1.0f)},
					            {"ogg", new Color(0.8f, 0.4f, 1.0f)},
				            };

			ReadSettings();
		}


		private static void Draw(string guid, Rect selectionRect)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			string extension = Path.GetExtension(path);
			string filename = Path.GetFileNameWithoutExtension(path);

			bool icons = selectionRect.height > 20;
			GUIStyle labelstyle = icons ? EditorStyles.miniLabel : EditorStyles.label;

			if (path.Length == 0)
				return;

			path = Path.GetDirectoryName(path);

			if (extension.Length == 0 || filename.Length == 0)
				return;

			string searchpath = _basePath +
			                    path.Substring(6).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

			if (!_showAll)
			{
				int files;
				string pathnoext = path + Path.AltDirectorySeparatorChar + filename;
				if (!_fileCount.TryGetValue(pathnoext, out files))
				{
					files = Directory.GetFiles(searchpath, filename + ".*").Length;
					_fileCount[pathnoext] = files;
				}

				if (files == 1)
					return;

				if (EditorSettings.externalVersionControl == ExternalVersionControl.Generic)
					if (files == 2)
						return;
			}

			extension = extension.Substring(1);
			string drawextension = extension;

			Rect newRect = selectionRect;
			Vector2 labelSize = labelstyle.CalcSize(new GUIContent(drawextension));

			if (icons)
			{
				labelSize = labelstyle.CalcSize(new GUIContent(drawextension));
				//newRect.y += newRect.height - 25;
				newRect.x += newRect.width - labelSize.x;
				newRect.width = labelSize.x;
				newRect.height = labelSize.y;
			}
			else
			{
#if UNITY_4_0
					newRect.width += selectionRect.x - 16;
			newRect.x = 0;
#else
				newRect.width += selectionRect.x;
				newRect.x = 0;
#endif

				newRect.x = newRect.width - labelSize.x - 4;

				drawextension = "." + drawextension;
				labelSize = labelstyle.CalcSize(new GUIContent(drawextension));

				newRect.width = labelSize.x + 1;
			}

			Color color = GUI.color;
			Color newColor;

			// fill background
			newColor = Common.DefaultBackgroundColor;
			newColor.a = 1;
			GUI.color = newColor;
			GUI.DrawTexture(newRect, EditorGUIUtility.whiteTexture);

			if (!_colorMap.TryGetValue(extension.ToLower(), out newColor))
				newColor = Color.grey;

			GUI.color = newColor;
			GUI.Label(newRect, drawextension, labelstyle);
			GUI.color = color;
		}

		public static void ClearCache(string sAsset)
		{
			_fileCount.Remove(Path.GetDirectoryName(sAsset) + Path.AltDirectorySeparatorChar +
			                  Path.GetFileNameWithoutExtension(sAsset));
		}


		//////////////////////////////////////////////////////////////////////

		private static Vector2 _scroll;
		private static string _editingName = "";
		private static Color _editingColor;

		[PreferenceItem("Project Window")]
		public static void DrawPrefs()
		{
			_showAll = EditorGUILayout.Toggle("Show all", _showAll);

			string removeExtension = null;
			string changeExtension = null;
			Color changeColor = Color.black;

			EditorGUILayout.Space();
			foreach (KeyValuePair<string, Color> ext in _colorMap)
			{
				EditorGUILayout.BeginHorizontal(GUILayout.Width(300));
				EditorGUILayout.SelectableLabel(ext.Key, GUILayout.Width(80), GUILayout.Height(16));

				Color c = EditorGUILayout.ColorField(ext.Value);
				if (c != ext.Value)
				{
					changeExtension = ext.Key;
					changeColor = c;
				}

				if (GUILayout.Button("del", GUILayout.Width(42)))
				{
					_editingName = ext.Key;
					_editingColor = ext.Value;
					removeExtension = ext.Key;
				}

				EditorGUILayout.EndHorizontal();
			}
			//GUILayout.Label("", GUILayout.Width(32));
			//EditorGUILayout.EndScrollView();

			if (removeExtension != null)
				_colorMap.Remove(removeExtension);

			if (changeExtension != null)
				_colorMap[changeExtension] = changeColor;

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("", GUILayout.Width(32));
			_editingName = EditorGUILayout.TextField(_editingName, GUILayout.Width(80));
			_editingColor = EditorGUILayout.ColorField(_editingColor);
			if (GUILayout.Button("add", GUILayout.Width(42)))
			{
			}
			EditorGUILayout.EndHorizontal();

			if (GUI.changed)
			{
				SaveSettings();

				// this doesn't appear to work in Unity 4b7
				EditorApplication.RepaintProjectWindow();
			}
		}

		private static void ReadSettings()
		{
			string colourinfo;

			_showAll = EditorPrefs.GetBool("TeneProjectWindow_All", false);

			string colormap = Common.GetLongPref("TeneProjectWindow_ColorMap");
		}

		private static void SaveSettings()
		{
			EditorPrefs.SetBool("TeneProjectWindow_All", _showAll);

			string colormap = "";
			foreach (KeyValuePair<string, Color> entry in _colorMap)
				colormap += entry.Key + ":" + Common.ColorToString(entry.Value) + "|";

			Common.SetLongPref("TeneProjectWindow_ColorMap", colormap);
		}

	}

	public class ProjectWindowExtensionsClass : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets(string[] pImported, string[] pDeleted, string[] pMoved, string[] pMoveFrom)
		{
			foreach (string file in pImported)
				TeneProjectWindow.ClearCache(file);

			foreach (string file in pDeleted)
				TeneProjectWindow.ClearCache(file);

			foreach (string file in pMoved)
				TeneProjectWindow.ClearCache(file);

			foreach (string file in pMoveFrom)
				TeneProjectWindow.ClearCache(file);
		}
	}
}