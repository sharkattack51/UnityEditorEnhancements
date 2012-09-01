/*
 * Copyright (c) 2012 Tenebrous
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 *
 * Latest version: http://hg.tenebrous.co.uk/unityeditorenhancements
*/

using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Tenebrous.EditorEnhancements
{
	[InitializeOnLoad]
	public static class TeneHierarchyWindow
	{
		private static Dictionary<string, Color> _colorMap;
		private static string _basePath;

		private static bool _showAll;
		private static int _shownLayers;

		private static Camera _sceneCam;

		static TeneHierarchyWindow()
		{
			EditorApplication.hierarchyWindowItemOnGUI += Draw;
			SceneView.onSceneGUIDelegate += Updated;
			ReadSettings();
		}

		static void Updated( SceneView pScene )
		{
			if( Event.current.type == EventType.Repaint )
			{
				if( _sceneCam == null )
					if( SceneView.GetAllSceneCameras().Length > 0 )
						_sceneCam = SceneView.GetAllSceneCameras()[0]; 
				
				Common.HierarchyWindow.Repaint();
			}
		}

		private static void Draw( int pInstanceID, Rect pDrawingRect )
		{
			GameObject gameObject = EditorUtility.InstanceIDToObject( pInstanceID ) as GameObject;
			if( gameObject == null )
				return;

			EditorWindow hierarchyWindow = Common.HierarchyWindow;
			Texture tex;

			Color originalColor = GUI.color;

			if( _sceneCam != null && ( ( 1 << gameObject.layer ) & _sceneCam.cullingMask) == 0 )
			{
				Rect labelRect = pDrawingRect;
				labelRect.width = EditorStyles.label.CalcSize( new GUIContent( gameObject.name ) ).x;
				labelRect.x-=2;
				labelRect.y-=4;
				GUI.Label( labelRect, "".PadRight( gameObject.name.Length, '_' ) );
			}

			Rect iconRect = pDrawingRect;
			iconRect.x = pDrawingRect.x + pDrawingRect.width - 16;
			iconRect.y--;
			iconRect.width = 16;
			iconRect.height = 16;

			foreach( Component c in gameObject.GetComponents<Component>() )
			{
				if( c is Transform )
					continue;

				string tooltip = "";

				GUI.color = c.GetEnabled() ? Color.white : new Color( 0.5f, 0.5f, 0.5f, 0.5f );

				tex = null;

				if( c == null )
				{
					Rect rectX = new Rect( iconRect.x + 4, iconRect.y + 1, 14, iconRect.height );
					GUI.color = new Color( 1.0f, 0.35f, 0.35f, 1.0f );
					GUI.Label( rectX, new GUIContent( "X", "Missing Script" ), EditorStyles.boldLabel );
					iconRect.x -= 9;
					continue;
				}

				tooltip = c.GetType().ToString().Replace( "UnityEngine.", "" );
				if( c is MonoBehaviour )
				{
					MonoScript ms = MonoScript.FromMonoBehaviour( c as MonoBehaviour );
					tex = AssetDatabase.GetCachedIcon( AssetDatabase.GetAssetPath( ms ) );
				}

				if( tex == null )
					tex = Common.GetMiniThumbnail( c );

				if( tex != null )
				{
					GUI.DrawTexture( iconRect, tex, ScaleMode.ScaleToFit );
					if( GUI.Button( iconRect, new GUIContent( "", tooltip ), EditorStyles.label ) )
					{
						c.SetEnabled( !c.GetEnabled() );
						hierarchyWindow.Focus();
						Common.HierarchyWindow.Repaint();
						return;
					}
					iconRect.x -= iconRect.width;
				}
			}

			GUI.color = originalColor;

			//{
			//    Rect rect = new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y - hierarchyWindow.position.y, 0, 100);

			//    EditorUtility.DisplayCustomMenu(
			//        rect,
			//        new GUIContent[] { new GUIContent("hello") }, 
			//        -1, null, gameObject
			//    );
			//}
		}

		public static bool GetEnabled( this Component pComponent )
		{
			if( pComponent == null )
				return ( true );

			PropertyInfo p = pComponent.GetType().GetProperty( "enabled", typeof( bool ) );

			if( p != null )
				return ( (bool)p.GetValue( pComponent, null ) );

			return ( true );
		}
		public static void SetEnabled( this Component pComponent, bool bNewValue )
		{
			if( pComponent == null )
				return;

			Undo.RegisterUndo( pComponent, bNewValue ? "Enable Component" : "Disable Component" );

			PropertyInfo p = pComponent.GetType().GetProperty( "enabled", typeof( bool ) );

			if( p != null )
			{
				p.SetValue( pComponent, bNewValue, null );
				EditorUtility.SetDirty( pComponent.gameObject );
			}
		}

		//////////////////////////////////////////////////////////////////////

		[PreferenceItem( "Hierarchy Pane" )]
		public static void DrawPrefs()
		{
			if( GUI.changed )
			{
				SaveSettings();
				Common.HierarchyWindow.Repaint();
			}
		}

		private static void ReadSettings()
		{
		}

		private static void SaveSettings()
		{
		}
	}
}