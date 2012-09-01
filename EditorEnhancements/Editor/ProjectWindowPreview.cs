using System;
using System.IO;
using Tenebrous.EditorEnhancements;
using UnityEditor;
using UnityEngine;
using System.Collections;
using Object = UnityEngine.Object;

public class ProjectWindowPreview : EditorWindow
{
	private string _guid;
	private string _path;
	private Object _asset;
	private Texture2D _tex;
	private double _timeStart;
	private bool _noPreview;
	private string _info;

	private bool _hasAlpha;

	public string GUID
	{
		get { return _guid; }
		set
		{
			_guid = value;
			_path = AssetDatabase.GUIDToAssetPath( _guid );
			title = Path.GetFileName( _path );

			_asset = AssetDatabase.LoadAssetAtPath( _path, typeof( Object ) );

			_noPreview = false;
			_hasAlpha = false;
			_info = "";

			if( _asset is Texture2D )
			{
				_tex = (Texture2D)_asset;
				_hasAlpha = true;
				position = new Rect(position.x-200,position.y,position.width+200,position.height);
			}
			else
			{
				_tex = Common.GetAssetPreview( _asset );
			}

			_info += _asset.GetPreviewInfo() + "\n" + _asset.GetType().ToString();

			_timeStart = EditorApplication.timeSinceStartup;
		}
	}

	void Update()
	{
		if( _tex == null && _guid != null && _asset != null )
		{
			_tex = Common.GetAssetPreview( _asset );
			if( _tex != null )
				Repaint();
			else if( EditorApplication.timeSinceStartup - _timeStart > 3.0f )
			{
				_noPreview = true;
				Repaint();
			}
		}
	}

	void OnGUI()
	{
		Rect pos = position;
		pos.x = 0;
		pos.y = 0;

		if( _tex == null )
		{
			if( _noPreview )
				GUI.Label( pos, "No preview\n\n" + _info );
			else
				GUI.Label( pos, "Loading...\n\n" + _info );
		}
		else
		{
			GUI.color = Color.white;

			if( _hasAlpha )
			{
				Rect half = pos;
				half.width /= 2;
				EditorGUI.DrawTextureAlpha( half, _tex );
				
				half.x += half.width;
				EditorGUI.DrawPreviewTexture( half, _tex );
			}
			else
				GUI.DrawTexture( pos, _tex, ScaleMode.StretchToFill, false );

			pos.y = pos.height - EditorStyles.boldLabel.CalcHeight( new GUIContent( _info ), pos.width );
			pos.height -= pos.y;

			GUI.color = Color.white;
			EditorGUI.DropShadowLabel( pos, _info, EditorStyles.boldLabel );
		}
	}
}

static class ProjectWindowPreviewInfo
{
	public static string GetPreviewInfo( this Object pObject )
	{
		if( pObject is AudioClip )
			return ( (AudioClip)pObject ).GetPreviewInfo();
		else if( pObject is Texture2D )
			return ( (Texture2D)pObject ).GetPreviewInfo();
		return "";
	}

	public static string GetPreviewInfo( this AudioClip pObject )
	{
		string info = "";

		TimeSpan clipTimespan = TimeSpan.FromSeconds( pObject.length );
		if( clipTimespan.Hours > 0 )
			info += clipTimespan.Hours + ":";

		info += clipTimespan.Minutes.ToString( "00" ) + ":" + clipTimespan.Seconds.ToString( "00" ) + "." + clipTimespan.Milliseconds.ToString( "000" ) + "\n";

		if( pObject.channels == 1 )
			info += "Mono\n";
		else if( pObject.channels == 2 )
			info += "Stereo\n";
		else
			info += pObject.channels + " channels\n";

		return( info );
	}

	public static string GetPreviewInfo( this Texture2D pObject )
	{
		string info = "";

		info = pObject.format.ToString() + "\n"
		       + pObject.width + " x " + pObject.height;

		return( info );
	}	
}
