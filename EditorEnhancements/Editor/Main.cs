using UnityEditor;
using UnityEngine;
using System.Collections;

namespace Tenebrous.EditorEnhancements
{
    [InitializeOnLoad]
    public static class Main
    {
        private static EditorEnhancement[] list = new EditorEnhancement[]
        {
            new TeneProjectWindow(),
            new TeneHierarchyWindow()
        };

        static Main()
        {
            foreach (EditorEnhancement e in list)
                if( EditorPrefs.GetBool(e.Prefix + "_Enabled", true) )
                    e.OnEnable();
        }

        public static T Enhancement<T>() where T : EditorEnhancement
        {
            foreach( EditorEnhancement e in list )
                if (e is T)
                    return (T)e;

            return null;
        }

        [PreferenceItem("Enhancements")]
        public static void DrawPreferences()
        {
            foreach (EditorEnhancement e in list)
            {
                EditorGUILayout.BeginHorizontal();

                bool enabled = EditorPrefs.GetBool( e.Prefix + "_Enabled", true );
                bool newEnabled = EditorGUILayout.Toggle( enabled, GUILayout.Width( 32 ) );
                EditorPrefs.SetBool( e.Prefix + "_Enabled", newEnabled );

                if( newEnabled != enabled )
                {
                    if( newEnabled )
                        e.OnEnable();
                    else
                        e.OnDisable();
                }

                bool expanded = EditorPrefs.GetBool( e.Prefix + "_Expanded", false );
                bool newExpanded = EditorGUILayout.Foldout( expanded, "  " + e.Name );
                EditorPrefs.SetBool( e.Prefix + "_Expanded", newExpanded );

                if (newExpanded != expanded)
                {
                    if( newExpanded )
                        foreach( EditorEnhancement e2 in list )
                            if (e != e2 && EditorPrefs.GetBool(e2.Prefix + "_Expanded"))
                                EditorPrefs.SetBool(e2.Prefix + "_Expanded", false);
                }

                EditorGUILayout.EndHorizontal();

                if( expanded )
                {
                    EditorGUILayout.Separator();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginVertical();
                    e.DrawPreferences();
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Separator();
                }

                EditorGUILayout.Space();
            }
        }
    }

    public class EditorEnhancement
    {
        public virtual void OnEnable()
        {
        }

        public virtual void OnDisable()
        {
        }

        public virtual void DrawPreferences()
        {
            
        }

        public virtual string Name
        {
            get { return "Nothing"; }
        }

        public virtual string Prefix
        {
            get { return "nothing"; }
        }
    }
}