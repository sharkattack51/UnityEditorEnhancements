using System;
using UnityEditor;
using UnityEngine;
using System.Collections;
using Object = UnityEngine.Object;

static class AssetInfo
{
	public static string GetPreviewInfo( this object pObject )
	{
		string info = "";
		string typename = pObject.GetType().ToString().Replace( "UnityEngine.", "" );

		if( pObject is AudioClip )
			info = ( (AudioClip)pObject ).GetPreviewInfo();
		else if( pObject is Texture2D )
			info = ( (Texture2D)pObject ).GetPreviewInfo();
		else if( pObject is Material )
			info = ( (Material)pObject ).GetPreviewInfo();
		else if( pObject is Mesh )
			info = ( (Mesh)pObject ).GetPreviewInfo();
		else if( pObject is MeshFilter )
			info = ( (MeshFilter)pObject ).GetPreviewInfo();
		else if( pObject is MeshRenderer )
			info = ( (MeshRenderer)pObject ).GetPreviewInfo();
		else if( pObject is MonoScript )
		{
			typename = "";
			info = ((MonoScript) pObject).GetPreviewInfo();
		}
		else if( pObject is Shader )
			info = ( (Shader)pObject ).GetPreviewInfo();
		else if( pObject is MonoBehaviour )
		{
			typename = pObject.GetType().BaseType.ToString().Replace( "UnityEngine.", "" );
			info = ( (MonoBehaviour)pObject ).GetPreviewInfo();
		}
		else if( pObject is Behaviour )
			info = ( (Behaviour)pObject ).GetPreviewInfo();

		if( typename != "" )
			if( info == "" )
				info += typename;
			else
				info += " (" + typename + ")";

		return( info );
	}

	public static string GetPreviewInfo( this AudioClip pObject )
	{
		string info = "";

		TimeSpan clipTimespan = TimeSpan.FromSeconds( pObject.length );
		if( clipTimespan.Hours > 0 )
			info += clipTimespan.Hours + ":";

		info += clipTimespan.Minutes.ToString( "00" ) + ":" + clipTimespan.Seconds.ToString( "00" ) + "." + clipTimespan.Milliseconds.ToString( "000" ) + " ";

		if( pObject.channels == 1 )
			info += "Mono\n";
		else if( pObject.channels == 2 )
			info += "Stereo\n";
		else
			info += pObject.channels + " channels\n";

		return ( info );
	}

	public static string GetPreviewInfo( this Texture2D pObject )
	{
		string info = "";

		info = pObject.format.ToString() + "\n"
			   + pObject.width + "x" + pObject.height;

		return ( info );
	}

	public static string GetPreviewInfo( this Material pObject )
	{
		string info = "";

		info = pObject.shader.name;

		foreach( Object obj in EditorUtility.CollectDependencies( new Object[] { pObject } ) )
			if( obj is Texture )
				info += "\n - " + obj.name;

		return ( info );
	}


	public static string GetPreviewInfo( this MeshFilter pObject )
	{
		string info = "";

		if( pObject.sharedMesh != null )
			info += pObject.sharedMesh.GetPreviewInfo();

		return ( info );
	}

	public static string GetPreviewInfo( this Mesh pObject )
	{
		string info = "";

		info += pObject.vertexCount + " verts " + pObject.triangles.Length + " tris"
			 + "\n" + pObject.name;

		return ( info );
	}

	public static string GetPreviewInfo( this MeshRenderer pObject )
	{
		string info = "";

		//	info += "blah";

		return ( info );
	}

	public static string GetPreviewInfo( this MonoScript pObject )
	{
		string info = "";

		Type assetclass = pObject.GetClass();
		if( assetclass != null )
			info += assetclass.ToString() + "\n(" + assetclass.BaseType.ToString().Replace( "UnityEngine.", "" ) + ")";
		else
			info += "(multiple classes)";

		return ( info );
	}

	public static string GetPreviewInfo( this Shader pObject )
	{
		string info = "";

		info += pObject.renderQueue;

		return ( info );
	}

	public static string GetPreviewInfo( this MonoBehaviour pObject )
	{
		string info = "";

		MonoScript ms = MonoScript.FromMonoBehaviour( pObject );
		
		info += ms.name;

		return ( info );
	}


	// components

	public static string GetPreviewInfo( this Behaviour pObject )
	{
		string info = "";

		if( pObject is Light )
			info += ( pObject as Light ).GetPreviewInfo();
		else if( pObject is Camera )
			info += ( pObject as Camera ).GetPreviewInfo();
		else if( pObject is AudioSource )
			info += ( pObject as AudioSource ).GetPreviewInfo();
		else if( pObject is AudioReverbFilter )
			info += ( pObject as AudioReverbFilter ).GetPreviewInfo();
		
		return ( info );
	}

	public static string GetPreviewInfo( this Light pObject )
	{
		string info = "";

		info += pObject.type;

		return ( info );
	}

	public static string GetPreviewInfo( this Camera pObject )
	{
		string info = "";

		if( pObject.orthographic )
			info += "Orthographic";
		else
			info += "Perspective";

		return ( info );
	}

	public static string GetPreviewInfo( this AudioSource pObject )
	{
		string info = "";

		info += pObject.clip.name;

		return ( info );
	}

	public static string GetPreviewInfo( this AudioReverbFilter pObject )
	{
		string info = "";

		info += pObject.reverbPreset;

		return ( info );
	}
}
