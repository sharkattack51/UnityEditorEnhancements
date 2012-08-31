using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Tenebrous.EditorEnhancements
{
	[InitializeOnLoad]
	public static class TeneHeirarchyWindow
	{
		private static Dictionary<string, Color> _colorMap;
		private static string _basePath;

		private static bool _showAll;

		static TeneHeirarchyWindow()
		{
			EditorApplication.hierarchyWindowItemOnGUI += Draw;
			SceneView.onSceneGUIDelegate += Updated;
			ReadSettings();
		}

		static void Updated( SceneView pScene )
		{
			EditorApplication.RepaintHierarchyWindow();
		}

		private static EditorWindow _heirarchyWindow = null;
		private static EditorWindow HeirarchyWindow
		{
			get
			{
				if (_heirarchyWindow == null)
					_heirarchyWindow = EditorWindow.GetWindow<EditorWindow>("UnityEditor.HeirarchyWindow");

				return (_heirarchyWindow);
			}
		}

		private static void Draw(int instanceID, Rect selectionRect)
		{
			// EditorUtility.GetMiniThumbnail()
			GameObject gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
			if (gameObject == null)
				return;

			EditorWindow heirarchyWindow = HeirarchyWindow;
			Texture tex;

			Color originalColor = GUI.color;

			//Color cc = Common.DefaultBackgroundColor;
			//cc.a = 1;
			//GUI.color = cc;
			//GUI.DrawTexture(selectionRect, EditorGUIUtility.whiteTexture);

			//GUI.color = originalColor;

			//Rect labelRect = selectionRect;

			//tex = GetMainIcon(gameObject);
			//if (tex != null)
			//{
			//    Rect iconLabelRect = new Rect(labelRect.x, labelRect.y, 16, 16 );
			//    GUI.Label( iconLabelRect, new GUIContent(tex), EditorStyles.label );
			//    labelRect.x += iconLabelRect.width;
			//    labelRect.width -= iconLabelRect.width;
			//}

			//GUI.Label(labelRect, gameObject.name);
			
			Rect iconRect = selectionRect;
			iconRect.x = selectionRect.x + selectionRect.width - 16;
			iconRect.y--;
			iconRect.width = 16;
			iconRect.height = 16;

			foreach( Component c in gameObject.GetComponents<Component>() )
			{
				if( c is Transform )
					continue;

				GUI.color = c.GetEnabled() ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);

				tex = null;

				if (c is MonoBehaviour)
				{
					MonoScript ms = MonoScript.FromMonoBehaviour(c as MonoBehaviour);
					tex = AssetDatabase.GetCachedIcon(AssetDatabase.GetAssetPath(ms));
				}

				if( tex == null )
					tex = EditorUtility.GetMiniThumbnail(c);

				if (tex != null)
				{
					if (GUI.Button(iconRect, new GUIContent(tex,c.GetType().ToString().Replace("UnityEngine.","")),EditorStyles.label))
					{
						c.SetEnabled(!c.GetEnabled());
						heirarchyWindow.Focus();
						EditorApplication.RepaintHierarchyWindow();
						return;
					}
					iconRect.x -= iconRect.width;
				}
			}

			GUI.color = originalColor;

			//{
			//    Rect rect = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y - heirarchyWindow.position.y, 0, 100);

			//    EditorUtility.DisplayCustomMenu(
			//        rect,
			//        new GUIContent[] { new GUIContent("hello") }, 
			//        -1, null, gameObject
			//    );
			//}
		}

		public static Texture GetMainIcon( GameObject gameObject )
		{
			Texture tex = null;
			foreach (Component c in gameObject.GetComponents<Component>())
			{
				tex = EditorUtility.GetMiniThumbnail(c);
				if( c is Camera || c is Light || c is MeshRenderer )
					break;
				tex = null;
			}
			return( tex );
		}

		public static bool GetEnabled( this Component pComponent )
		{
			PropertyInfo p = pComponent.GetType().GetProperty("enabled", typeof (bool));

			if (p != null)
				return( (bool)p.GetValue(pComponent,null) );

			return( true );
		}
		public static void SetEnabled( this Component pComponent, bool bNewValue )
		{
			PropertyInfo p = pComponent.GetType().GetProperty("enabled", typeof(bool));

			if (p != null)
				p.SetValue(pComponent, bNewValue, null);
		}

		//////////////////////////////////////////////////////////////////////

		private static Vector2 _scroll;
		private static string _editingName = "";
		private static Color _editingColor;

		[PreferenceItem("Heirarchy Window")]
		public static void DrawPrefs()
		{
			if (GUI.changed)
			{
				SaveSettings();
				EditorApplication.RepaintHierarchyWindow();
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
}