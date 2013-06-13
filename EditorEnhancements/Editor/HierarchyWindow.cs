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

		private static bool _setting_showHoverPreview;
		private static bool _setting_showHoverPreviewShift;
		private static bool _setting_showHoverPreviewCtrl;
		private static bool _setting_showHoverPreviewAlt;
		private static bool _setting_showHoverTooltip;
		private static bool _setting_showHoverTooltipShift;

		private static Object _hoverObject;
		private static Object _lastHoverObject;

		private static int _updateThrottle;
		private static Vector2 _mousePosition;

		private static Dictionary<Object, string> _tooltips = new Dictionary<Object, string>();

		static TeneHierarchyWindow()
		{
			EditorApplication.hierarchyWindowItemOnGUI += Draw;
			EditorApplication.update += Update;
			SceneView.onSceneGUIDelegate += Updated;
			EditorApplication.hierarchyWindowChanged += ClearTooltipCache;
			ReadSettings();
		}

		private static TeneEnhPreviewWindow _window;
		private static void Update()
		{
			if( _lastHoverObject == null && _hoverObject == null )
				return;

			if( _lastHoverObject != _hoverObject )
			{
				// don't currently support plain old gameobjects
				if( _hoverObject is GameObject )
					if( PrefabUtility.GetPrefabParent( _hoverObject ) == null )
						_hoverObject = null;
			}

			if( _lastHoverObject != _hoverObject )
			{
				_lastHoverObject = _hoverObject;

				TeneEnhPreviewWindow.Update(
					Common.HierarchyWindow.position,
					_mousePosition,
					pAsset: _hoverObject
				);
			}
			else
			{
				_updateThrottle++;
				if( _updateThrottle > 20 )
				{
					_hoverObject = null;
					Common.HierarchyWindow.Repaint();
					_updateThrottle = 0;
				}
			}
		}

		static void Updated( SceneView pScene )
		{
			// triggered when the user changes anything
			// e.g. manually enables/disables components, etc

			if( Event.current.type == EventType.Repaint )
				Common.HierarchyWindow.Repaint();
		}

		private static void Draw( int pInstanceID, Rect pDrawingRect )
		{
			// called per-line in the hierarchy window

			GameObject gameObject = EditorUtility.InstanceIDToObject( pInstanceID ) as GameObject;
			if( gameObject == null )
				return;

			Texture tex;

			bool doPreview = _setting_showHoverPreview
						  && ( !_setting_showHoverPreviewShift || ( Event.current.modifiers & EventModifiers.Shift ) != 0 )
						  && ( !_setting_showHoverPreviewCtrl || ( Event.current.modifiers & EventModifiers.Control ) != 0 )
						  && ( !_setting_showHoverPreviewAlt || ( Event.current.modifiers & EventModifiers.Alt ) != 0 );

			bool doTooltip = _setting_showHoverTooltip && (!_setting_showHoverTooltipShift || Event.current.modifiers == EventModifiers.Shift);
			string tooltip = "";

			Color originalColor = GUI.color;

			if( (( 1 << gameObject.layer ) & Tools.visibleLayers) == 0 )
			{
				Rect labelRect = pDrawingRect;
				labelRect.width = EditorStyles.label.CalcSize( new GUIContent( gameObject.name ) ).x;
				labelRect.x-=2;
				labelRect.y-=4;
				GUI.Label( labelRect, "".PadRight( gameObject.name.Length, '_' ) );
			}

			if( doTooltip )
			{
				tooltip = GetTooltip(gameObject);
				if (tooltip.Length > 0)
					GUI.Label(pDrawingRect, new GUIContent(" ", tooltip));
			}

			Rect iconRect = pDrawingRect;
			iconRect.x = pDrawingRect.x + pDrawingRect.width - 16;
			iconRect.y--;
			iconRect.width = 16;
			iconRect.height = 16;

			_mousePosition = new Vector2( Event.current.mousePosition.x + Common.HierarchyWindow.position.x, Event.current.mousePosition.y + Common.HierarchyWindow.position.y );

			if( doPreview )
				if( pDrawingRect.Contains( Event.current.mousePosition ) )
					_hoverObject = gameObject;

			foreach( Component c in gameObject.GetComponents<Component>() )
			{
				if( c is Transform )
					continue;

				GUI.color = c.GetEnabled() ? Color.white : new Color( 0.5f, 0.5f, 0.5f, 0.5f );

				tex = null;

				if( c == null )
				{
					Rect rectX = new Rect( iconRect.x + 4, iconRect.y + 1, 14, iconRect.height );
					GUI.color = new Color( 1.0f, 0.35f, 0.35f, 1.0f );
					GUI.Label( rectX, new GUIContent( "X", "Missing Script" ), Common.ColorLabel( new Color( 1.0f, 0.35f, 0.35f, 1.0f ) ) );
					iconRect.x -= 9;

					if( rectX.Contains( Event.current.mousePosition ) )
						_hoverObject = null;

					continue;
				}

				if( doTooltip )
 					tooltip = GetTooltip(c);

				if( c is MonoBehaviour )
				{
					MonoScript ms = MonoScript.FromMonoBehaviour( c as MonoBehaviour );
					tex = AssetDatabase.GetCachedIcon( AssetDatabase.GetAssetPath( ms ) );
				}

				if( tex == null )
					tex = Common.GetMiniThumbnail( c );

				if( tex != null )
				{
					if( doPreview )
						if( iconRect.Contains( Event.current.mousePosition ) )
							_hoverObject = c;

					GUI.DrawTexture( iconRect, tex, ScaleMode.ScaleToFit );
					if( GUI.Button( iconRect, new GUIContent( "", tooltip ), EditorStyles.label ) )
					{
						c.SetEnabled( !c.GetEnabled() );
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

		private static string GetTooltip( UnityEngine.Object pObject )
		{
			string tooltip;

			if( _tooltips.TryGetValue( pObject, out tooltip ) )
				return ( tooltip );

			tooltip = pObject.GetPreviewInfo();

			_tooltips[pObject] = tooltip;

			return tooltip;
		}
		private static void ClearTooltipCache()
		{
			_tooltips.Clear();
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
			_setting_showHoverPreview = EditorGUILayout.Toggle( "Show asset preview on hover", _setting_showHoverPreview );
			if( _setting_showHoverPreview )
			{
				_setting_showHoverPreviewShift = EditorGUILayout.Toggle( "         only when holding shift", _setting_showHoverPreviewShift );
				_setting_showHoverPreviewCtrl  = EditorGUILayout.Toggle( "         only when holding ctrl", _setting_showHoverPreviewCtrl );
				_setting_showHoverPreviewAlt   = EditorGUILayout.Toggle( "         only when holding alt", _setting_showHoverPreviewAlt );
			}

			_setting_showHoverTooltip = EditorGUILayout.Toggle( "Show asset tooltip on hover", _setting_showHoverTooltip );
			if( _setting_showHoverTooltip )
				_setting_showHoverTooltipShift = EditorGUILayout.Toggle( "         only when holding shift", _setting_showHoverTooltipShift );

			if( GUI.changed )
			{
				SaveSettings();
				Common.HierarchyWindow.Repaint();
			}
		}

		private static void ReadSettings()
		{
			_setting_showHoverPreview = EditorPrefs.GetBool( "TeneHierarchyWindow_PreviewOnHover", true );
			_setting_showHoverPreviewShift = EditorPrefs.GetBool( "TeneHierarchyWindow_PreviewOnHoverShift", false );
			_setting_showHoverPreviewCtrl = EditorPrefs.GetBool( "TeneHierarchyWindow_PreviewOnHoverCtrl", false );
			_setting_showHoverPreviewAlt = EditorPrefs.GetBool( "TeneHierarchyWindow_PreviewOnHoverAlt", false );
			_setting_showHoverTooltip = EditorPrefs.GetBool( "TeneHierarchyWindow_HoverTooltip", true );
			_setting_showHoverTooltipShift = EditorPrefs.GetBool( "TeneHierarchyWindow_HoverTooltipShift", false );
		}

		private static void SaveSettings()
		{
			EditorPrefs.SetBool( "TeneHierarchyWindow_PreviewOnHover", _setting_showHoverPreview );
			EditorPrefs.SetBool( "TeneHierarchyWindow_PreviewOnHoverShift", _setting_showHoverPreviewShift );
			EditorPrefs.SetBool( "TeneHierarchyWindow_PreviewOnHoverCtrl", _setting_showHoverPreviewCtrl );
			EditorPrefs.SetBool( "TeneHierarchyWindow_PreviewOnHoverAlt", _setting_showHoverPreviewAlt );
			EditorPrefs.SetBool( "TeneHierarchyWindow_HoverTooltip", _setting_showHoverTooltip );
			EditorPrefs.SetBool( "TeneHierarchyWindow_HoverTooltipShift", _setting_showHoverTooltipShift );
		}
	}
}