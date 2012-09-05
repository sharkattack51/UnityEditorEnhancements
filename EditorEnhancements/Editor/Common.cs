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

using System.IO;
using UnityEngine;
using UnityEditor;

namespace Tenebrous.EditorEnhancements
{
	public static class Common
	{
		private static string _lastBackgroundColourString;
		private static Color _lastBackgroundColour;
		private static string _basePath;

		public static string BasePath
		{
			get
			{
				if( _basePath == null )
					_basePath = Application.dataPath.Replace( Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar );

				return ( _basePath );
			}
		}

		public static string ProjectPath
		{
			get
			{
				string basePath = BasePath;
				return ( basePath.Substring(0, basePath.Length-7) );
			}
		}

		public static string TempRecompilationList
		{
			get
			{
				return ProjectPath 
					+ Path.DirectorySeparatorChar 
					+ "Temp" 
					+ Path.DirectorySeparatorChar 
					+ "tene_recompile.txt";
			}
		}

		public static Color DefaultBackgroundColor
		{
			get
			{
				if (!EditorGUIUtility.isProSkin)
					return (new Color(0.75f, 0.75f, 0.75f, 1.0f));

				string value = EditorPrefs.GetString("Windows/Background");
				Color c;

				if (value == _lastBackgroundColourString)
					return (_lastBackgroundColour);

				string[] elements = value.Split(';');
				if (elements.Length == 5)
					if (elements[0] == "Windows/Background")
					{
						if (float.TryParse(elements[1], out c.r)
							&& float.TryParse(elements[2], out c.g)
							&& float.TryParse(elements[3], out c.b)
							&& float.TryParse(elements[4], out c.a))
						{
							c.a = 1;
							_lastBackgroundColour = c;
							_lastBackgroundColourString = value;
							return (c);
						}
					}

				return (Color.black);
			}
		}
		
		public static Color StringToColor(string pString)
		{
			Color c = Color.grey;

			string[] elements = pString.Split(',');

			if (elements.Length == 4)
				if (float.TryParse(elements[0], out c.r)
					&& float.TryParse(elements[1], out c.g)
					&& float.TryParse(elements[2], out c.b)
					&& float.TryParse(elements[3], out c.a))
					return (c);

			return (Color.grey);
		}

		public static string ColorToString(Color pColor)
		{
			return (pColor.r.ToString("0.00")
					+ ","
					+ pColor.g.ToString("0.00")
					+ ","
					+ pColor.b.ToString("0.00")
					+ ","
					+ pColor.a.ToString("0.00")
				   );
		}

		public static string GetLongPref(string pName)
		{
			string result = "";
			int index = 0;

			while (EditorPrefs.HasKey(pName + index))
				result += EditorPrefs.GetString(pName + index++);

			return (result);
		}

		public static void SetLongPref(string pName, string pValue)
		{
			string value = "";
			int index = 0;

			while (pValue.Length > 1000)
			{
				value = pValue.Substring(0, 1000);
				EditorPrefs.SetString(pName + index++, value);
				pValue = pValue.Substring(1000);
			}
			EditorPrefs.SetString(pName + index++, pValue);

			while (EditorPrefs.HasKey(pName + index))
				EditorPrefs.DeleteKey(pName + index++);
		}

		public static Texture GetMiniThumbnail( UnityEngine.Object obj )
		{
#if UNITY_4_0
			return ( AssetPreview.GetMiniThumbnail( obj ) );
#else
			return ( EditorUtility.GetMiniThumbnail( obj ) );
#endif
		}

		public static Texture2D GetAssetPreview( UnityEngine.Object obj )
		{
#if UNITY_4_0
			return ( AssetPreview.GetAssetPreview( obj ) );
#else
			return ( EditorUtility.GetAssetPreview( obj ) );
#endif
		}


		public static EditorWindow GetWindowByName( string pName )
		{
			UnityEngine.Object[] objectList = Resources.FindObjectsOfTypeAll( typeof( EditorWindow ) );

			foreach( UnityEngine.Object obj in objectList )
				if (obj.GetType().ToString() == pName)
					return ((EditorWindow) obj);

			return ( null );
		}

		private static EditorWindow _projectWindow = null;
		public static EditorWindow ProjectWindow
		{
			get
			{
				_projectWindow = _projectWindow ?? GetWindowByName( "UnityEditor.ProjectWindow" ) ?? GetWindowByName( "UnityEditor.ObjectBrowser" );
				return ( _projectWindow );
			}
		}


		private static EditorWindow _hierarchyWindow = null;
		public static EditorWindow HierarchyWindow
		{
			get
			{
				_hierarchyWindow = _hierarchyWindow ?? GetWindowByName( "UnityEditor.HierarchyWindow" );
				return ( _hierarchyWindow );
			}
		}
	}
}																	