/*
 * Copyright (c) 2013 Tenebrous
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
 * Latest version: http://hg.tenebrous.co.uk/unityeditorenhancements/wiki/Home
*/

using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Tenebrous.EditorEnhancements
{
    public class TeneHierarchyWindow : EditorEnhancement
	{
		private Dictionary<string, Color> _colorMap;
		private string _basePath;

		private bool _showAll;
		private int _shownLayers;

		private bool _setting_showHoverPreview;
		private bool _setting_showHoverPreviewShift;
		private bool _setting_showHoverPreviewCtrl;
		private bool _setting_showHoverPreviewAlt;
		private bool _setting_showHoverTooltip;
        private bool _setting_showHoverTooltipShift;
        private bool _setting_showHoverTooltipCtrl;
        private bool _setting_showHoverTooltipAlt;
        private bool _setting_showHoverDropWindow;

		private Object _hoverObject;
		private Object _lastHoverObject;

		private int _updateThrottle;
		private Vector2 _mousePosition;

		private GameObject _draggingHeldOver;
		private System.DateTime _draggingHeldStart;
		private bool _draggingShownQuickInspector;

		private Dictionary<Object, string> _tooltips = new Dictionary<Object, string>();

		private bool _wasDragging = false;

		public TeneHierarchyWindow()
		{
			ReadSettings();
		}

        public override void OnEnable()
        {
            EditorApplication.hierarchyWindowItemOnGUI += Draw;
            EditorApplication.update += Update;
            SceneView.onSceneGUIDelegate += Updated;
            EditorApplication.hierarchyWindowChanged += ClearTooltipCache;
            if( Common.HierarchyWindow != null ) Common.HierarchyWindow.Repaint();
        }

        public override void OnDisable()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= Draw;
            EditorApplication.update -= Update;
            SceneView.onSceneGUIDelegate -= Updated;
            EditorApplication.hierarchyWindowChanged -= ClearTooltipCache;
            Common.HierarchyWindow.Repaint();
        }

        public override string Name
        {
            get
            {
                return "Hierarchy Window";
            }
        }

        public override string Prefix
        {
            get
            {
                return "TeneHierarchyWindow";
            }
        }


		private void Update()
		{
			bool nowDragging = DragAndDrop.objectReferences.Length == 1;
			if( _wasDragging != nowDragging )
			{
				// detect when dragging state changes, so we force a refresh of the hierarchy
				_wasDragging = nowDragging;

				if( !nowDragging )
				{
					_draggingHeldOver = null;
					_draggingShownQuickInspector = false;
				}

				// doesn't appear to refresh when starting to drag stuff
				// but works ok when finishing
				Common.HierarchyWindow.Repaint();
			}

			if( _lastHoverObject != null || _hoverObject != null )
			{

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
						pAsset : _hoverObject
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

			if( _draggingHeldOver != null )
				if( !_draggingShownQuickInspector )
					if( ( System.DateTime.Now - _draggingHeldStart ).TotalSeconds >= 0.5f )
					{
						// user held mouse over a game object, so we can show the
						// quick-drop popup window

                        TeneDropTarget.Update(
                            Common.HierarchyWindow.position,
                            _mousePosition,
                            _draggingHeldOver
                        );

						_draggingShownQuickInspector = true;
					}
		}

		void Updated( SceneView pScene )
		{
			// triggered when the user changes anything
			// e.g. manually enables/disables components, etc

			if( Event.current.type == EventType.Repaint )
				Common.HierarchyWindow.Repaint();
		}

		private void Draw( int pInstanceID, Rect pDrawingRect )
		{
			// called per-line in the hierarchy window

			GameObject gameObject = EditorUtility.InstanceIDToObject( pInstanceID ) as GameObject;
			if( gameObject == null )
				return;

            bool doPreview = _setting_showHoverPreview
                          && ( !_setting_showHoverPreviewShift || ( Event.current.modifiers & EventModifiers.Shift   ) != 0 )
                          && ( !_setting_showHoverPreviewCtrl  || ( Event.current.modifiers & EventModifiers.Control ) != 0 )
                          && ( !_setting_showHoverPreviewAlt   || ( Event.current.modifiers & EventModifiers.Alt     ) != 0 );

            bool doTooltip = _setting_showHoverTooltip
                          && ( !_setting_showHoverTooltipShift || ( Event.current.modifiers & EventModifiers.Shift   ) != 0 )
                          && ( !_setting_showHoverTooltipCtrl  || ( Event.current.modifiers & EventModifiers.Control ) != 0 )
                          && ( !_setting_showHoverTooltipAlt   || ( Event.current.modifiers & EventModifiers.Alt     ) != 0 );

            string tooltip = "";

			Color originalColor = GUI.color;

			float width = EditorStyles.label.CalcSize( new GUIContent( gameObject.name ) ).x;

			if( (( 1 << gameObject.layer ) & Tools.visibleLayers) == 0 )
			{
				Rect labelRect = pDrawingRect;
				labelRect.width = width;
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

			bool mouseIn = pDrawingRect.Contains( Event.current.mousePosition );

			if( doPreview && mouseIn )
				_hoverObject = gameObject;

			Object dragging = DragAndDrop.objectReferences.Length == 1 ? DragAndDrop.objectReferences[0] : null;

			if( DragAndDrop.objectReferences.Length == 1 && _setting_showHoverDropWindow && mouseIn )
			{
				if (_draggingHeldOver == null)
				    _draggingHeldStart = System.DateTime.Now;

				if (_draggingHeldOver != gameObject)
				    _draggingShownQuickInspector = false;

				_draggingHeldOver = gameObject;
			}

			bool suitableDrop = false;
			bool drawnEtc = false;

			foreach( Component c in gameObject.GetComponents<Component>() )
			{
				if( c is Transform )
					continue;

				if( c != null && !suitableDrop && dragging != null )
				{
					Type type = c.GetType();
					foreach( FieldInfo f in TeneDropTarget.FieldsFor( type ) )
						if( TeneDropTarget.IsCompatibleField( f, dragging ) )
						{
							suitableDrop = true;
							break;
						}
				}

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

				GUI.color = c.GetEnabled() ? Color.white : new Color( 0.5f, 0.5f, 0.5f, 0.5f );

				if( iconRect.x < pDrawingRect.x + width )
				{
					if( !drawnEtc )
					{
						GUI.Label( iconRect, " .." );
						drawnEtc = true;
					}
					continue;
				}

				if( doTooltip )
 					tooltip = GetTooltip(c);

				Texture iconTexture = null;

				if( c is MonoBehaviour )
				{
					MonoScript ms = MonoScript.FromMonoBehaviour( c as MonoBehaviour );
					iconTexture = AssetDatabase.GetCachedIcon( AssetDatabase.GetAssetPath( ms ) );
				}

				if( iconTexture == null )
					iconTexture = Common.GetMiniThumbnail( c );

				if( iconTexture != null )
				{
					if( doPreview )
						if( iconRect.Contains( Event.current.mousePosition ) )
							_hoverObject = c;

					GUI.DrawTexture( iconRect, iconTexture, ScaleMode.ScaleToFit );
					if( GUI.Button( iconRect, new GUIContent( "", tooltip ), EditorStyles.label ) )
					{
						c.SetEnabled( !c.GetEnabled() );
						Common.HierarchyWindow.Repaint();
						return;
					}
					iconRect.x -= iconRect.width;
				}
			}

			if( suitableDrop )
			{
				Rect labelRect = pDrawingRect;
				
				labelRect.width = width;
				labelRect.x -= 2;
				labelRect.y += 1;

				GUI.color = Color.white;
				GUI.DrawTexture( new Rect( labelRect.x, labelRect.y, labelRect.width, 1 ), EditorGUIUtility.whiteTexture );
				GUI.DrawTexture( new Rect( labelRect.x, labelRect.y, 1, labelRect.height ), EditorGUIUtility.whiteTexture );
				GUI.DrawTexture( new Rect( labelRect.x, labelRect.yMax, labelRect.width, 1 ), EditorGUIUtility.whiteTexture );
				GUI.DrawTexture( new Rect( labelRect.xMax, labelRect.y, 1, labelRect.height ), EditorGUIUtility.whiteTexture );

				if( mouseIn )
					DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}

			GUI.color = originalColor;
		}

		private string GetTooltip( UnityEngine.Object pObject )
		{
			string tooltip;

			if( _tooltips.TryGetValue( pObject, out tooltip ) )
				return ( tooltip );

			tooltip = pObject.GetPreviewInfo();

			_tooltips[pObject] = tooltip;

			return tooltip;
		}
		private void ClearTooltipCache()
		{
			_tooltips.Clear();
		}

		//////////////////////////////////////////////////////////////////////

		public override void DrawPreferences()
		{
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label( "Asset preview on hover", GUILayout.Width( 176 ) );

            _setting_showHoverPreview = GUILayout.Toggle( _setting_showHoverPreview, "" );

            if( _setting_showHoverPreview )
            {
                EditorGUILayout.Space();
                _setting_showHoverPreviewShift = GUILayout.Toggle( _setting_showHoverPreviewShift, "shift" );
                EditorGUILayout.Space();
                _setting_showHoverPreviewCtrl = GUILayout.Toggle( _setting_showHoverPreviewCtrl, "ctrl" );
                EditorGUILayout.Space();
                _setting_showHoverPreviewAlt = GUILayout.Toggle( _setting_showHoverPreviewAlt, "alt" );
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();

            GUILayout.Label( "Asset tooltip on hover", GUILayout.Width( 176 ) );

            _setting_showHoverTooltip = GUILayout.Toggle( _setting_showHoverTooltip, "" );

            if( _setting_showHoverTooltip )
            {
                EditorGUILayout.Space();
                _setting_showHoverTooltipShift = GUILayout.Toggle( _setting_showHoverTooltipShift, "shift" );
                EditorGUILayout.Space();
                _setting_showHoverTooltipCtrl = GUILayout.Toggle( _setting_showHoverTooltipCtrl, "ctrl" );
                EditorGUILayout.Space();
                _setting_showHoverTooltipAlt = GUILayout.Toggle( _setting_showHoverTooltipAlt, "alt" );
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

		    _setting_showHoverDropWindow = EditorGUILayout.Toggle( "Quick-drop window", _setting_showHoverDropWindow );

		    if( GUI.changed )
		    {
		        SaveSettings();
		        Common.HierarchyWindow.Repaint();
		    }
		}

		public void ReadSettings()
		{
            _setting_showHoverPreview = EditorPrefs.GetBool( "TeneHierarchyWindow_PreviewOnHover", Defaults.HierarchyWindowHoverPreview );
            _setting_showHoverPreviewShift = EditorPrefs.GetBool( "TeneHierarchyWindow_PreviewOnHoverShift", Defaults.HierarchyWindowHoverPreviewShift );
            _setting_showHoverPreviewCtrl = EditorPrefs.GetBool( "TeneHierarchyWindow_PreviewOnHoverCtrl", Defaults.HierarchyWindowHoverPreviewCtrl );
            _setting_showHoverPreviewAlt = EditorPrefs.GetBool( "TeneHierarchyWindow_PreviewOnHoverAlt", Defaults.HierarchyWindowHoverPreviewAlt );

            _setting_showHoverTooltip = EditorPrefs.GetBool( "TeneHierarchyWindow_HoverTooltip", Defaults.HierarchyWindowHoverTooltip );
            _setting_showHoverTooltipShift = EditorPrefs.GetBool( "TeneHierarchyWindow_HoverTooltipShift", Defaults.HierarchyWindowHoverTooltipShift );
            _setting_showHoverTooltipCtrl = EditorPrefs.GetBool( "TeneHierarchyWindow_HoverTooltipCtrl", Defaults.HierarchyWindowHoverTooltipCtrl );
            _setting_showHoverTooltipAlt = EditorPrefs.GetBool( "TeneHierarchyWindow_HoverTooltipAlt", Defaults.HierarchyWindowHoverTooltipAlt );

            _setting_showHoverDropWindow = EditorPrefs.GetBool( "TeneHierarchyWindow_HoverDropWindow", Defaults.HierarchyWindowHoverDropWindow );
		}

		private void SaveSettings()
		{
			EditorPrefs.SetBool( "TeneHierarchyWindow_PreviewOnHover", _setting_showHoverPreview );
			EditorPrefs.SetBool( "TeneHierarchyWindow_PreviewOnHoverShift", _setting_showHoverPreviewShift );
			EditorPrefs.SetBool( "TeneHierarchyWindow_PreviewOnHoverCtrl", _setting_showHoverPreviewCtrl );
			EditorPrefs.SetBool( "TeneHierarchyWindow_PreviewOnHoverAlt", _setting_showHoverPreviewAlt );

			EditorPrefs.SetBool( "TeneHierarchyWindow_HoverTooltip", _setting_showHoverTooltip );
            EditorPrefs.SetBool( "TeneHierarchyWindow_HoverTooltipShift", _setting_showHoverTooltipShift );
            EditorPrefs.SetBool( "TeneHierarchyWindow_HoverTooltipCtrl", _setting_showHoverTooltipCtrl );
            EditorPrefs.SetBool( "TeneHierarchyWindow_HoverTooltipAlt", _setting_showHoverTooltipAlt );

            EditorPrefs.SetBool( "TeneHierarchyWindow_HoverDropWindow", _setting_showHoverDropWindow );
		}
	}

    public static class ComponentExtensions
    {
        public static bool GetEnabled( this Component pComponent )
        {
            if( pComponent == null )
                return ( true );

            PropertyInfo p = pComponent.GetType().GetProperty( "enabled", typeof( bool ) );

            if( p != null )
                return ( (bool)p.GetValue( pComponent, null ) );

            return ( true );
        }
        public static void SetEnabled( this Component pComponent, bool pNewValue )
        {
            if( pComponent == null )
                return;

            Undo.RegisterUndo( pComponent, pNewValue ? "Enable Component" : "Disable Component" );

            PropertyInfo p = pComponent.GetType().GetProperty( "enabled", typeof( bool ) );

            if( p != null )
            {
                p.SetValue( pComponent, pNewValue, null );
                EditorUtility.SetDirty( pComponent.gameObject );
            }
        }        
    }
}