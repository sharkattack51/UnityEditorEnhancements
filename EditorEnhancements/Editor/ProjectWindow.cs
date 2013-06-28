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
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Tenebrous.EditorEnhancements
{
	public class TeneProjectWindow : EditorEnhancement
	{
		private Dictionary<string, Color> _colorMap;

		// caches for various things
		private Dictionary<string, int> _fileCount = new Dictionary<string, int>();
		private Dictionary<string, int> _folderFileCount = new Dictionary<string, int>();
		private Dictionary<string, string> _tooltips = new Dictionary<string, string>();
		//private Dictionary<string, string> _specialPaths = new Dictionary<string, string>();
		private Dictionary<string, FileAttributes> _fileAttrs = new Dictionary<string, FileAttributes>();

        public enum ShowExtensions
        {
            Never,
            Always,
            OnlyWhenConflicts
        }
        
		// settings
        private ShowExtensions _setting_showExtensionsWhen;
		private bool _setting_showFileCount;

		private bool _setting_showHoverPreview;
		private bool _setting_showHoverPreviewShift;
		private bool _setting_showHoverPreviewCtrl;
		private bool _setting_showHoverPreviewAlt;
		private bool _setting_showHoverTooltip;
        private bool _setting_showHoverTooltipShift;
        private bool _setting_showHoverTooltipCtrl;
        private bool _setting_showHoverTooltipAlt;

		private string _lastGUID;
		private string _currentGUID;
		private int _updateThrottle;

		// mouse position recorded during the projectWindowItemOnGUI
		private Vector2 _mousePosition;

		// any version-specific hacks
		private bool _needHackScrollbarWidthForDrawing;

        public override void OnEnable()
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

            if( Common.ProjectWindow != null ) Common.ProjectWindow.Repaint();
        }

        public override void OnDisable()
        {
            EditorApplication.projectWindowItemOnGUI -= Draw;
            EditorApplication.update -= Update;
            Common.ProjectWindow.Repaint();
        }

        public override string Name
        {
            get
            {
                return "Project Window";
            }
        }

        public override string Prefix
        {
            get
            {
                return "TeneProjectWindow";
            }
        }

		private void CheckScriptInfo( string pLines )
		{
			//string[] scripts = sLines.Split(new char[] {'\n'} ,StringSplitOptions.RemoveEmptyEntries);
			//foreach( string script in scripts )
			//{
			//	//Debug.Log(script);
			//}
		}

		private TeneEnhPreviewWindow _window;
		private void Update()
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

		private void Draw( string pGUID, Rect pDrawingRect )
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
                          && ( !_setting_showHoverPreviewShift || ( Event.current.modifiers & EventModifiers.Shift   ) != 0 )
                          && ( !_setting_showHoverPreviewCtrl  || ( Event.current.modifiers & EventModifiers.Control ) != 0 )
                          && ( !_setting_showHoverPreviewAlt   || ( Event.current.modifiers & EventModifiers.Alt     ) != 0 );

            bool doTooltip = _setting_showHoverTooltip
                          && ( !_setting_showHoverTooltipShift || ( Event.current.modifiers & EventModifiers.Shift   ) != 0 )
                          && ( !_setting_showHoverTooltipCtrl  || ( Event.current.modifiers & EventModifiers.Control ) != 0 )
                          && ( !_setting_showHoverTooltipAlt   || ( Event.current.modifiers & EventModifiers.Alt     ) != 0 );

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

            if( !isFolder )
                if (_setting_showExtensionsWhen == ShowExtensions.Never)
                    return;
                else if( _setting_showExtensionsWhen == ShowExtensions.OnlyWhenConflicts )
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

		private FileAttributes GetFileAttr( string assetpath )
		{
			FileAttributes attrs;

			if( _fileAttrs.TryGetValue( assetpath, out attrs ) )
				return ( attrs );

			string searchpath = Common.FullPath( assetpath );

			attrs = File.GetAttributes( searchpath );

			_fileAttrs[ assetpath ] = attrs;

			return ( attrs );
		}

		private string GetTooltip( string assetpath )
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

		private int GetExtensionsCount( string extension, string filename, string path )
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

		private int GetFolderFilesCount( string assetpath )
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

		public void ClearCache( string sAsset )
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

		public override void DrawPreferences()
		{
			_setting_showExtensionsWhen = (ShowExtensions)EditorGUILayout.EnumPopup( "Show extensions", (Enum)_setting_showExtensionsWhen );

			EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				_setting_showFileCount = GUILayout.Toggle( _setting_showFileCount, "" );
				GUILayout.Label( "Show folder file count", GUILayout.Width( 176 ) );
				GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();
				EditorGUILayout.Space();
				_setting_showHoverPreview = GUILayout.Toggle( _setting_showHoverPreview, "" );
				GUILayout.Label( "Asset preview on hover", GUILayout.Width( 176 ) );

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
				EditorGUILayout.Space();
				_setting_showHoverTooltip = GUILayout.Toggle( _setting_showHoverTooltip, "" );
				GUILayout.Label( "Asset tooltip on hover", GUILayout.Width( 176 ) );

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

		private void ReadSettings()
		{
			//string colourinfo;

            if( EditorPrefs.HasKey( "TeneProjectWindow_All" ) )
            {
                _setting_showExtensionsWhen = EditorPrefs.GetBool( "TeneProjectWindow_All", true ) ? ShowExtensions.Always : ShowExtensions.OnlyWhenConflicts;
                EditorPrefs.DeleteKey( "TeneProjectWindow_All" );
            }
            else
            {
                _setting_showExtensionsWhen = (ShowExtensions)EditorPrefs.GetInt( "TeneProjectWindow_WhenExtensions", (int)Defaults.ProjectWindowExtensionsWhen );
            }
            _setting_showFileCount = EditorPrefs.GetBool( "TeneProjectWindow_FileCount", Defaults.ProjectWindowFileCount );

			//string colormap = Common.GetLongPref("TeneProjectWindow_ColorMap");

            _setting_showHoverPreview = EditorPrefs.GetBool( "TeneProjectWindow_PreviewOnHover", Defaults.ProjectWindowHoverPreview );
            _setting_showHoverPreviewShift = EditorPrefs.GetBool( "TeneProjectWindow_PreviewOnHoverShift", Defaults.ProjectWindowHoverPreviewShift );
            _setting_showHoverPreviewCtrl = EditorPrefs.GetBool( "TeneProjectWindow_PreviewOnHoverCtrl", Defaults.ProjectWindowHoverPreviewCtrl );
            _setting_showHoverPreviewAlt = EditorPrefs.GetBool( "TeneProjectWindow_PreviewOnHoverAlt", Defaults.ProjectWindowHoverPreviewAlt );

            _setting_showHoverTooltip = EditorPrefs.GetBool( "TeneProjectWindow_HoverTooltip", Defaults.ProjectWindowHoverTooltip );
            _setting_showHoverTooltipShift = EditorPrefs.GetBool( "TeneProjectWindow_HoverTooltipShift", Defaults.ProjectWindowHoverTooltipShift );
            _setting_showHoverTooltipCtrl = EditorPrefs.GetBool( "TeneProjectWindow_HoverTooltipCtrl", Defaults.ProjectWindowHoverTooltipCtrl );
            _setting_showHoverTooltipAlt = EditorPrefs.GetBool( "TeneProjectWindow_HoverTooltipAlt", Defaults.ProjectWindowHoverTooltipAlt );
        }

		private void SaveSettings()
		{
			EditorPrefs.SetInt( "TeneProjectWindow_WhenExtensions", (int)_setting_showExtensionsWhen );
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
            EditorPrefs.SetBool( "TeneProjectWindow_HoverTooltipCtrl", _setting_showHoverTooltipCtrl );
            EditorPrefs.SetBool( "TeneProjectWindow_HoverTooltipAlt", _setting_showHoverTooltipAlt );
        }
	}

    public class ProjectWindowExtensionsClass : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets( string[] pImported, string[] pDeleted, string[] pMoved, string[] pMoveFrom )
        {
            TeneProjectWindow proj = Main.Enhancement<TeneProjectWindow>();
            if (proj == null )
                return;

            string compilationList = "";
            foreach( string file in pImported )
            {
                string lower = file.ToLower();
                if( lower.EndsWith( ".cs" ) || lower.EndsWith( ".boo" ) || lower.EndsWith( ".js" ) )
                    compilationList += file + "\n";

                proj.ClearCache( file );
            }

            if( compilationList.Length > 0 )
                File.WriteAllText( Common.TempRecompilationList, compilationList );

            foreach( string file in pDeleted )
                proj.ClearCache( file );

            foreach( string file in pMoved )
                proj.ClearCache( file );

            foreach( string file in pMoveFrom )
                proj.ClearCache( file );
        }
    }
}