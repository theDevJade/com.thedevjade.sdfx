using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal static class SdfxEditorScroll
    {
        public static Vector2 Begin(Vector2 scroll, params GUILayoutOption[] options)
        {
            return EditorGUILayout.BeginScrollView(scroll, false, true, options);
        }

        public static Vector2 BeginFlexible(Vector2 scroll, float minHeight, float maxHeight)
        {
            return Begin(
                scroll,
                GUILayout.ExpandWidth(true),
                GUILayout.MinHeight(minHeight),
                GUILayout.MaxHeight(maxHeight));
        }

        public static Vector2 BeginFill(Vector2 scroll)
        {
            return Begin(scroll, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        }

        public static void End()
        {
            EditorGUILayout.EndScrollView();
        }

        public static Vector2 GetPersisted(string key)
        {
            return new Vector2(
                SessionState.GetFloat(key + ".x", 0f),
                SessionState.GetFloat(key + ".y", 0f));
        }

        public static void SetPersisted(string key, Vector2 scroll)
        {
            SessionState.SetFloat(key + ".x", scroll.x);
            SessionState.SetFloat(key + ".y", scroll.y);
        }
    }
}
