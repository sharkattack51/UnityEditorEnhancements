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
using System.IO;
using System.Reflection;
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

		// caches for various things
		private static Dictionary<string, int> _fileCount = new Dictionary<string, int>();
		private static Dictionary<string, int> _folderFileCount = new Dictionary<string, int>();
		private static Dictionary<string, string> _tooltips = new Dictionary<string, string>();
		//private static Dictionary<string, string> _specialPaths = new Dictionary<string, string>();
		private static Dictionary<string, FileAttributes> _fileAttrs = new Dictionary<string, FileAttributes>();

		// settings
		private static bool _setting_showAllExtensions;
		private static bool _setting_showFileCount;

		private static bool _setting_showHoverPreview;
		private static bool _setting_showHoverPreviewShift;
		private static bool _setting_showHoverPreviewCtrl;
		private static bool _setting_showHoverPreviewAlt;
		private static bool _setting_showHoverTooltip;
		private static bool _setting_showHoverTooltipShift;

		private static string _lastGUID;
		private static string _currentGUID;
		private static int _updateThrottle;

		// mouse position recorded during the projectWindowItemOnGUI
		private static Vector2 _mousePosition;

		// any version-specific hacks
		private static bool _needHackScrollbarWidthForDrawing;

		static TeneProjectWindow()
		{
			EditorApplication.projectWindowItemOnGUI += Draw;
			EditorApplication.update += Update;
			
			if( EditorGUIUtility.isProSkin )
				_colorMap = new Dictionary<string, Color>()
				{
					{"png", new Color(0.8f, 0.8f, 1.0f)},
					{"psd", new Color(0.5f, 0.8f, 1.0f)},
					{"tga", new Color(0.8f, 0.5f, 1.0f)},

					{"cs",  new Color(0.5f, 1.0f, 0.5f)},
					{"js",  new Color(0.8f, 1.0f, 0.3f)},
					{"boo", new Color(0.3f, 1.0f, 0.8f)},

					{"mat", new Color(1.0f, 0.8f, 0.8f)},
					{"shader", new Color(1.0f, 0.5f, 0.5f)},

					{"wav", new Color(0.8f, 0.4f, 1.0f)},
					{"mp3", new Color(0.8f, 0.4f, 1.0f)},
					{"ogg", new Color(0.8f, 0.4f, 1.0f)},
				};
			else
				_colorMap = new Dictionary<string, Color>()
				{
					{"png", new Color(0.0f, 0.0f, 1.0f)},
					{"psd", new Color(0.7f, 0.2f, 1.0f)},
					{"tga", new Color(0.2f, 0.7f, 1.0f)},

					{"cs",  new Color(0.0f, 0.5f, 0.0f)},
					{"js",  new Color(0.5f, 0.5f, 0.0f)},
					{"boo", new Color(0.0f, 0.5f, 0.5f)},

					{"mat", new Color(0.2f, 0.8f, 0.8f)},
					{"shader", new Color(1.0f, 0.5f, 0.5f)},

					{"wav", new Color(0.8f, 0.4f, 1.0f)},
					{"mp3", new Color(0.8f, 0.4f, 1.0f)},
					{"ogg", new Color(0.8f, 0.4f, 1.0f)},
				};

			//_specialPaths = new Dictionary<string, string>()
			//{
			//    {"WebPlayerTemplates", "WebPlayerTemplates - excluded from build"}
			//};

			_needHackScrollbarWidthForDrawing = Common.UnityVersion() < Common.UnityVersion( "4.0.0b8" );

			ReadSettings();

			if( File.Exists( Common.TempRecompilationList ) )
			{
				CheckScriptInfo( File.ReadAllText( Common.TempRecompilationList ) );
				File.Delete( Common.TempRecompilationList );
			}
		}

		private static void CheckScriptInfo( string pLines )
		{
			//string[] scripts = sLines.Split(new char[] {'\n'} ,StringSplitOptions.RemoveEmptyEntries);
			//foreach( string script in scripts )
			//{
			//	//Debug.Log(script);
			//}
		}

		private static TeneEnhPreviewWindow _window;
		private static void Update()
		{
			if( !_setting_showHoverPreview )
				return;

			if( _lastGUID == null && _currentGUID == null )
				return;

			if( _lastGUID != _currentGUID )
			{
				_lastGUID = _currentGUID;

				TeneEnhPreviewWindow.Update(
					Common.ProjectWindow.position, 
					_mousePosition,
					pGUID : _currentGUID
				);
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
			// called per-line in the project window

			string assetpath = AssetDatabase.GUIDToAssetPath( pGUID );
			string extension = Path.GetExtension( assetpath );
			string filename = Path.GetFileNameWithoutExtension( assetpath );
			bool isFolder = false;

			bool icons = pDrawingRect.height > 20;

			if( assetpath.Length == 0 )
				return;

			string path = Path.GetDirectoryName( assetpath );

			bool doPreview = _setting_showHoverPreview
						  && ( !_setting_showHoverPreviewShift || ( Event.current.modifiers & EventModifiers.Shift ) != 0 )
						  && ( !_setting_showHoverPreviewCtrl || ( Event.current.modifiers & EventModifiers.Control ) != 0 )
						  && ( !_setting_showHoverPreviewAlt || ( Event.current.modifiers & EventModifiers.Alt ) != 0 );

			bool doTooltip = _setting_showHoverTooltip && ( !_setting_showHoverTooltipShift || Event.current.modifiers == EventModifiers.Shift );

			if( doTooltip )
			{
				string tooltip = GetTooltip(assetpath);
				if (tooltip.Length > 0)
					GUI.Label(pDrawingRect, new GUIContent(" ", tooltip));
			}

			isFolder = ( GetFileAttr( assetpath ) & FileAttributes.Directory ) != 0;

			if( !_setting_showFileCount && isFolder )
				return;

			_mousePosition = new Vector2(Event.current.mousePosition.x + Common.ProjectWindow.position.x, Event.current.mousePosition.y + Common.ProjectWindow.position.y );

#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3
			if( Event.current.mousePosition.x < pDrawingRect.width - 16 )
#endif
			if( doPreview )
				if( pDrawingRect.Contains( Event.current.mousePosition ) )
					_currentGUID = pGUID;

			if( !_setting_showAllExtensions && !isFolder )
				if( GetExtensionsCount( extension, filename, path ) <= 1 )
					return;

			Color labelColor = Color.grey;
			string drawextension = "";

			if( !isFolder )
			{
				extension = extension.Substring( 1 );
				drawextension = extension;

				if( !_colorMap.TryGetValue( extension.ToLower(), out labelColor ) )
					labelColor = Color.grey;
			}
			else
			{
				labelColor = new Color(0.75f,0.75f,0.75f,1.0f);
				int files = GetFolderFilesCount( assetpath );
				if( files == 0 )
					return;
				drawextension = "(" + files + ")";
			}

			GUIStyle labelstyle = icons ? Common.ColorMiniLabel(labelColor) : Common.ColorLabel(labelColor);

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
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3
				newRect.width += pDrawingRect.x - (_needHackScrollbarWidthForDrawing ? 16 : 0);
				newRect.x = 0;
#else
				newRect.width += pDrawingRect.x;
				newRect.x = 0;
#endif
				newRect.x = newRect.width - labelSize.x;
				if( !isFolder )
				{
					newRect.x -= 4;
					drawextension = "." + drawextension;
				}

				labelSize = labelstyle.CalcSize( new GUIContent( drawextension ) );

				newRect.width = labelSize.x + 1;

				if( isFolder )
				{
					newRect = pDrawingRect;
					newRect.x += labelstyle.CalcSize( new GUIContent( filename ) ).x + 20;
				}
			}
			
			Color color = GUI.color;

			if( !isFolder || icons )
			{
				// fill background
				Color bgColor = Common.DefaultBackgroundColor;
				bgColor.a = 1;
				GUI.color = bgColor;
				GUI.DrawTexture( newRect, EditorGUIUtility.whiteTexture );
			}

			GUI.color = labelColor;
			GUI.Label( newRect, drawextension, labelstyle );
			GUI.color = color;
		}

		private static FileAttributes GetFileAttr( string assetpath )
		{
			FileAttributes attrs;

			if( _fileAttrs.TryGetValue( assetpath, out attrs ) )
				return ( attrs );

			string searchpath = Common.FullPath( assetpath );

			attrs = File.GetAttributes( searchpath );

			_fileAttrs[ assetpath ] = attrs;

			return ( attrs );
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

		private static int GetExtensionsCount( string extension, string filename, string path )
		{
			string searchpath = Common.FullPath( path );

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

		private static int GetFolderFilesCount( string assetpath )
		{
			string searchpath = Common.FullPath( assetpath );
			int files;

			if( _folderFileCount.TryGetValue( assetpath, out files ) )
				return ( files );

			string[] otherFilenames = Directory.GetFiles( searchpath );
			files = 0;

			searchpath += Path.DirectorySeparatorChar + ".";

			foreach( string otherFilename in otherFilenames )
			{
				if( otherFilename.StartsWith( searchpath ) )
					continue;

				if( otherFilename.EndsWith( ".meta" ) )
					continue;

				files++;
			}

			_folderFileCount[ assetpath ] = files;

			return files;
		}

		public static void ClearCache( string sAsset )
		{
			// remove cached count of number of files with alternate extensions
			_fileCount.Remove( Path.GetDirectoryName( sAsset ) 
							   + Path.AltDirectorySeparatorChar
							   + Path.GetFileNameWithoutExtension( sAsset ) );

			// remove cached tooltips for this path
			_tooltips.Remove( sAsset );

			// remove cached file attributes for this path
			_fileAttrs.Remove( sAsset );

			// removed cached folder file count for this path's folder
			_fileAttrs.Remove( Path.GetDirectoryName(sAsset) );
		}


		//////////////////////////////////////////////////////////////////////

		//		private static Vector2 _scroll;
		//		private static string _editingName = "";
		//		private static Color _editingColor;

		[PreferenceItem( "Project Pane" )]
		public static void DrawPrefs()
		{
			_setting_showAllExtensions = EditorGUILayout.Toggle( "Show all", _setting_showAllExtensions );
			_setting_showFileCount = EditorGUILayout.Toggle( "Show folder file counts", _setting_showFileCount );

			_setting_showHoverPreview = EditorGUILayout.Toggle( "Show asset preview on hover", _setting_showHoverPreview );
			if( _setting_showHoverPreview )
			{
				_setting_showHoverPreviewShift = EditorGUILayout.Toggle( "         when holding shift", _setting_showHoverPreviewShift );
				_setting_showHoverPreviewCtrl  = EditorGUILayout.Toggle( "         when holding ctrl", _setting_showHoverPreviewCtrl );
				_setting_showHoverPreviewAlt   = EditorGUILayout.Toggle( "         when holding alt", _setting_showHoverPreviewAlt );
			}

			_setting_showHoverTooltip = EditorGUILayout.Toggle( "Show asset tooltip on hover", _setting_showHoverTooltip );
			if( _setting_showHoverTooltip )
				_setting_showHoverTooltipShift = EditorGUILayout.Toggle( "         when holding shift", _setting_showHoverTooltipShift );

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
			_setting_showFileCount = EditorPrefs.GetBool( "TeneProjectWindow_FileCount", true );

			//string colormap = Common.GetLongPref("TeneProjectWindow_ColorMap");
			
			_setting_showHoverPreview = EditorPrefs.GetBool( "TeneProjectWindow_PreviewOnHover", true );
			_setting_showHoverPreviewShift = EditorPrefs.GetBool( "TeneProjectWindow_PreviewOnHoverShift", false );
			_setting_showHoverPreviewCtrl = EditorPrefs.GetBool( "TeneProjectWindow_PreviewOnHoverCtrl", false );
			_setting_showHoverPreviewAlt = EditorPrefs.GetBool( "TeneProjectWindow_PreviewOnHoverAlt", false );
			_setting_showHoverTooltip = EditorPrefs.GetBool( "TeneProjectWindow_HoverTooltip", true );
			_setting_showHoverTooltipShift = EditorPrefs.GetBool( "TeneProjectWindow_HoverTooltipShift", false );
		}

		private static void SaveSettings()
		{
			EditorPrefs.SetBool( "TeneProjectWindow_All", _setting_showAllExtensions );
			EditorPrefs.SetBool( "TeneProjectWindow_FileCount", _setting_showFileCount );

			string colormap = "";
			foreach( KeyValuePair<string, Color> entry in _colorMap )
				colormap += entry.Key + ":" + Common.ColorToString( entry.Value ) + "|";

			Common.SetLongPref( "TeneProjectWindow_ColorMap", colormap );

			EditorPrefs.SetBool( "TeneProjectWindow_PreviewOnHover", _setting_showHoverPreview );
			EditorPrefs.SetBool( "TeneProjectWindow_PreviewOnHoverShift", _setting_showHoverPreviewShift );
			EditorPrefs.SetBool( "TeneProjectWindow_PreviewOnHoverCtrl", _setting_showHoverPreviewCtrl );
			EditorPrefs.SetBool( "TeneProjectWindow_PreviewOnHoverAlt", _setting_showHoverPreviewAlt );
			EditorPrefs.SetBool( "TeneProjectWindow_HoverTooltip", _setting_showHoverTooltip );
			EditorPrefs.SetBool( "TeneProjectWindow_HoverTooltipShift", _setting_showHoverTooltipShift );
		}
	}

	public class ProjectWindowExtensionsClass : AssetPostprocessor
	{
		private static void OnPostprocessAllAssets( string[] pImported, string[] pDeleted, string[] pMoved, string[] pMoveFrom )
		{
			string compilationList = "";
			foreach( string file in pImported )
			{
				string lower = file.ToLower();
				if( lower.EndsWith( ".cs" ) || lower.EndsWith( ".boo" ) || lower.EndsWith( ".js" ) )
					compilationList += file + "\n";
				
				TeneProjectWindow.ClearCache( file );
			}

			if( compilationList.Length > 0 )
				File.WriteAllText( Common.TempRecompilationList, compilationList );

			foreach( string file in pDeleted )
				TeneProjectWindow.ClearCache( file );

			foreach( string file in pMoved )
				TeneProjectWindow.ClearCache( file );

			foreach( string file in pMoveFrom )
				TeneProjectWindow.ClearCache( file );
		}
	}
}