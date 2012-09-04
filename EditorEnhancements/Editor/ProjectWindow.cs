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

using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Tenebrous.EditorEnhancements
{
	[InitializeOnLoad]
	public static class TeneProjectWindow
	{
		private static Dictionary<string, Color> _colorMap;
		private static Dictionary<string, int> _fileCount = new Dictionary<string, int>();
		private static Dictionary<string, string> _tooltips = new Dictionary<string, string>();
		//private static Dictionary<string, string> _specialPaths = new Dictionary<string, string>();

		private static bool _setting_showAllExtensions;
		private static bool _setting_showHoverPreview;

		private static string _lastGUID;
		private static string _currentGUID;
		private static int _updateThrottle;

		private static Vector2 _mousePosition;

		static TeneProjectWindow()
		{
			EditorApplication.projectWindowItemOnGUI += Draw;
			EditorApplication.update += Update;

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

			//_specialPaths = new Dictionary<string, string>()
			//{
			//    {"WebPlayerTemplates", "WebPlayerTemplates - excluded from build"}
			//};

			ReadSettings();
		}

		private static ProjectWindowPreview _previewWindow;
		private static void Update()
		{
			if( !_setting_showHoverPreview )
				return;

			if( _lastGUID == null && _currentGUID == null )
				return;

			if( _lastGUID != _currentGUID )
			{
				_lastGUID = _currentGUID;

				if( _lastGUID != null )
				{
					string path = AssetDatabase.GUIDToAssetPath( _lastGUID );
					object _asset = AssetDatabase.LoadAssetAtPath( path, typeof( object ) );

					if( _asset is MonoScript || _asset is Shader || _asset.GetType().ToString() == "UnityEngine.Object" )
						return;

					if( _previewWindow == null )
						_previewWindow = EditorWindow.GetWindow<ProjectWindowPreview>( true );

					_previewWindow.Repaint();

					Rect projectWindowPos = Common.ProjectWindow.position;
					Rect newPos = new Rect( projectWindowPos.x - 210, _mousePosition.y - 90, 200, 200 );

					_previewWindow.GUID = _lastGUID;

					if( newPos.x < 0 )
						newPos.x = projectWindowPos.x + projectWindowPos.width;

					newPos.y = Mathf.Clamp( newPos.y, 0, Screen.currentResolution.height - 250 );

					_previewWindow.position = newPos;

					Common.ProjectWindow.Focus();
				}
				else
				{
					if( _previewWindow != null )
					{
						_previewWindow.Close();
						_previewWindow = null;
					}
				}
			}
			else
			{
				_updateThrottle++;
				if( _updateThrottle > 20 )
				{
					_currentGUID = null;
					Common.ProjectWindow.Repaint();
					_updateThrottle = 0;
				}
			}
		}

		private static void Draw( string pGUID, Rect pDrawingRect )
		{
			string assetpath = AssetDatabase.GUIDToAssetPath( pGUID );
			string extension = Path.GetExtension( assetpath );
			string filename = Path.GetFileNameWithoutExtension( assetpath );

			bool icons = pDrawingRect.height > 20;
			GUIStyle labelstyle = icons ? EditorStyles.miniLabel : EditorStyles.label;

			if( assetpath.Length == 0 )
				return;

			string path = Path.GetDirectoryName( assetpath );

			string tooltip = GetTooltip( assetpath );
			if( tooltip.Length > 0 )
				GUI.Label( pDrawingRect, new GUIContent( " ", tooltip ) );

			if( extension.Length == 0 || filename.Length == 0 )
				return;

			_mousePosition = new Vector2(Event.current.mousePosition.x + Common.ProjectWindow.position.x, Event.current.mousePosition.y + Common.ProjectWindow.position.y );

#if UNITY_4_0
			// ignore scrollbar width in Unity 4b7
			if( Event.current.mousePosition.x < pDrawingRect.width - 16 )
#endif
			if( pDrawingRect.Contains( Event.current.mousePosition ) )
				_currentGUID = pGUID;

			if( !_setting_showAllExtensions )
				if( GetFileCount( extension, filename, path ) <= 1 )
					return;

			extension = extension.Substring( 1 );
			string drawextension = extension;

			Rect newRect = pDrawingRect;
			Vector2 labelSize = labelstyle.CalcSize( new GUIContent( drawextension ) );

			if( icons )
			{
				labelSize = labelstyle.CalcSize( new GUIContent( drawextension ) );
				newRect.x += newRect.width - labelSize.x;
				newRect.width = labelSize.x;
				newRect.height = labelSize.y;
			}
			else
			{
#if UNITY_4_0
				newRect.width += pDrawingRect.x - 16;
				newRect.x = 0;
#else
				newRect.width += pDrawingRect.x;
				newRect.x = 0;
#endif

				newRect.x = newRect.width - labelSize.x - 4;

				drawextension = "." + drawextension;
				labelSize = labelstyle.CalcSize( new GUIContent( drawextension ) );

				newRect.width = labelSize.x + 1;
			}

			Color color = GUI.color;
			Color newColor;

			// fill background
			newColor = Common.DefaultBackgroundColor;
			newColor.a = 1;
			GUI.color = newColor;
			GUI.DrawTexture( newRect, EditorGUIUtility.whiteTexture );

			if( !_colorMap.TryGetValue( extension.ToLower(), out newColor ) )
				newColor = Color.grey;

			GUI.color = newColor;
			GUI.Label( newRect, drawextension, labelstyle );
			GUI.color = color;
		}

		private static string GetTooltip( string assetpath )
		{
			string tooltip;

			if( _tooltips.TryGetValue( assetpath, out tooltip ) )
				return( tooltip );

			Object asset = AssetDatabase.LoadAssetAtPath( assetpath, typeof( Object ) );

			tooltip = asset.GetPreviewInfo();
			while( tooltip.StartsWith( "\n" ) )
				tooltip = tooltip.Substring("\n".Length);

			//foreach( KeyValuePair<string,string> kvp in _specialPaths )
			//    if( System.Text.RegularExpressions.Regex.IsMatch(assetpath,kvp.Key) )
			//        tooltip += "\n" + kvp.Value;

			_tooltips[assetpath] = tooltip;
			
			return tooltip;
		}

		private static int GetFileCount( string extension, string filename, string path )
		{
			string searchpath = Common.BasePath +
								path.Substring( 6 ).Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );

			int files = 0;
			string pathnoext = path + Path.AltDirectorySeparatorChar + filename;

			if( !_fileCount.TryGetValue( pathnoext, out files ) )
			{
				files = 1;
				string[] otherFilenames = Directory.GetFiles( searchpath, filename + ".*" );
				foreach( string otherFilename in otherFilenames )
				{
					if( otherFilename.EndsWith( filename + extension ) )
						continue;

					if( otherFilename.EndsWith( ".meta" ) )
						continue;

					files++;
					break;
				}

				_fileCount[pathnoext] = files;
			}
			return files;
		}

		public static void ClearCache( string sAsset )
		{
			_fileCount.Remove( Path.GetDirectoryName( sAsset ) + Path.AltDirectorySeparatorChar +
							   Path.GetFileNameWithoutExtension( sAsset ) );

			_tooltips.Remove( sAsset );
		}


		//////////////////////////////////////////////////////////////////////

		//		private static Vector2 _scroll;
		//		private static string _editingName = "";
		//		private static Color _editingColor;

		[PreferenceItem( "Project Pane" )]
		public static void DrawPrefs()
		{
			_setting_showAllExtensions = EditorGUILayout.Toggle( "Show all", _setting_showAllExtensions );
			_setting_showHoverPreview = EditorGUILayout.Toggle( "Show asset preview on hover", _setting_showHoverPreview );

			/*
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
			*/

			if( GUI.changed )
			{
				SaveSettings();
				Common.ProjectWindow.Repaint();
			}
		}

		private static void ReadSettings()
		{
			//string colourinfo;

			_setting_showAllExtensions = EditorPrefs.GetBool( "TeneProjectWindow_All", true );
			_setting_showHoverPreview = EditorPrefs.GetBool( "TeneProjectWindow_PreviewOnHover", true );

			//string colormap = Common.GetLongPref("TeneProjectWindow_ColorMap");
		}

		private static void SaveSettings()
		{
			EditorPrefs.SetBool( "TeneProjectWindow_All", _setting_showAllExtensions );
			EditorPrefs.GetBool( "TeneProjectWindow_PreviewOnHover", _setting_showHoverPreview );

			string colormap = "";
			foreach( KeyValuePair<string, Color> entry in _colorMap )
				colormap += entry.Key + ":" + Common.ColorToString( entry.Value ) + "|";

			Common.SetLongPref( "TeneProjectWindow_ColorMap", colormap );
		}
	}

	public class ProjectWindowExtensionsClass : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets( string[] pImported, string[] pDeleted, string[] pMoved, string[] pMoveFrom )
		{
			foreach( string file in pImported )
				TeneProjectWindow.ClearCache( file );

			foreach( string file in pDeleted )
				TeneProjectWindow.ClearCache( file );

			foreach( string file in pMoved )
				TeneProjectWindow.ClearCache( file );

			foreach( string file in pMoveFrom )
				TeneProjectWindow.ClearCache( file );
		}
	}
}