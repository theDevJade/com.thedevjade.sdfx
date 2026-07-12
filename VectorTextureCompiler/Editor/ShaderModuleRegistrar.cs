using System;
using System.Collections.Generic;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Modules;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    [InitializeOnLoad]
    internal static class ShaderModuleRegistrar
    {
        static ShaderModuleRegistrar()
        {
            Reload();
        }

        public static void Reload()
        {
            ShaderModuleRegistry.ResetBuiltIns();
            ShaderModuleRegistry.RegisterAttributedModulesFromAllAssemblies();
            ShaderModuleRegistry.RegisterAssetDefinitions(FindAssetDefinitions());
        }

        public static IReadOnlyList<SdfxModuleDefinition> FindAssetDefinitions()
        {
            var guids = AssetDatabase.FindAssets("t:SdfxModuleDefinition");
            var list = new List<SdfxModuleDefinition>(guids.Length);
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<SdfxModuleDefinition>(path);
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }

        public static SdfxModuleDefinition CreateAssetInProject()
        {
            var folder = "Assets";
            if (Selection.activeObject != null)
            {
                var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    folder = AssetDatabase.IsValidFolder(selectedPath)
                        ? selectedPath
                        : (System.IO.Path.GetDirectoryName(selectedPath) ?? "Assets").Replace("\\", "/");
                }
            }

            var asset = ScriptableObject.CreateInstance<SdfxModuleDefinition>();
            var path = AssetDatabase.GenerateUniqueAssetPath(
                folder + "/" + SdfxLanguage.ModuleDefinition.DefaultId + ".asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Reload();
            return asset;
        }
    }

    internal sealed class SdfxModuleDefinitionPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (!TouchesModuleDefinition(importedAssets)
                && !TouchesModuleDefinition(deletedAssets)
                && !TouchesModuleDefinition(movedAssets)
                && !TouchesModuleDefinition(movedFromAssetPaths))
            {
                return;
            }

            EditorApplication.delayCall -= ShaderModuleRegistrar.Reload;
            EditorApplication.delayCall += ShaderModuleRegistrar.Reload;
        }

        private static bool TouchesModuleDefinition(string[] paths)
        {
            if (paths == null)
            {
                return false;
            }

            for (var i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                if (string.IsNullOrEmpty(path) || !path.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var asset = AssetDatabase.LoadAssetAtPath<SdfxModuleDefinition>(path);
                if (asset != null)
                {
                    return true;
                }

                if (path.IndexOf("SdfxModule", StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("ModuleDefinition", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }

    [CustomEditor(typeof(SdfxModuleDefinition))]
    internal sealed class SdfxModuleDefinitionEditor : UnityEditor.Editor
    {
        private SerializedProperty id;
        private SerializedProperty displayName;
        private SerializedProperty description;
        private SerializedProperty category;
        private SerializedProperty order;
        private SerializedProperty lodTier;
        private SerializedProperty conflictIds;
        private SerializedProperty extraSamplerCountOverride;
        private SerializedProperty properties;
        private SerializedProperty functionsSnippet;
        private SerializedProperty vertexSnippet;
        private SerializedProperty uvSnippet;
        private SerializedProperty fragmentSnippet;
        private SerializedProperty extraPassesSnippet;
        private SerializedProperty modePropertyName;
        private SerializedProperty modeLabels;
        private SerializedProperty fragmentModeSnippets;

        private void OnEnable()
        {
            id = serializedObject.FindProperty("Id");
            displayName = serializedObject.FindProperty("DisplayName");
            description = serializedObject.FindProperty("Description");
            category = serializedObject.FindProperty("Category");
            order = serializedObject.FindProperty("Order");
            lodTier = serializedObject.FindProperty("LodTier");
            conflictIds = serializedObject.FindProperty("ConflictIds");
            extraSamplerCountOverride = serializedObject.FindProperty("ExtraSamplerCountOverride");
            properties = serializedObject.FindProperty("Properties");
            functionsSnippet = serializedObject.FindProperty("FunctionsSnippet");
            vertexSnippet = serializedObject.FindProperty("VertexSnippet");
            uvSnippet = serializedObject.FindProperty("UvSnippet");
            fragmentSnippet = serializedObject.FindProperty("FragmentSnippet");
            extraPassesSnippet = serializedObject.FindProperty("ExtraPassesSnippet");
            modePropertyName = serializedObject.FindProperty("ModePropertyName");
            modeLabels = serializedObject.FindProperty("ModeLabels");
            fragmentModeSnippets = serializedObject.FindProperty("FragmentModeSnippets");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(SdfxLanguage.ModuleDefinition.InspectorHelp, MessageType.Info);

            EditorGUILayout.LabelField(SdfxLanguage.ModuleDefinition.SectionIdentity, EditorStyles.boldLabel);
            PropertyField(id, SdfxLanguage.ModuleDefinition.Id, SdfxLanguage.ModuleDefinition.IdTooltip);
            PropertyField(displayName, SdfxLanguage.ModuleDefinition.DisplayName);
            PropertyField(description, SdfxLanguage.ModuleDefinition.Description);
            PropertyField(category, SdfxLanguage.ModuleDefinition.Category);
            PropertyField(order, SdfxLanguage.ModuleDefinition.Order, SdfxLanguage.ModuleDefinition.OrderTooltip);
            PropertyField(lodTier, SdfxLanguage.ModuleDefinition.LodTier, SdfxLanguage.ModuleDefinition.LodTierTooltip);
            PropertyField(conflictIds, SdfxLanguage.ModuleDefinition.ConflictIds);
            PropertyField(extraSamplerCountOverride, SdfxLanguage.ModuleDefinition.ExtraSamplerOverride, SdfxLanguage.ModuleDefinition.ExtraSamplerTooltip);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(SdfxLanguage.ModuleDefinition.SectionHooks, EditorStyles.boldLabel);
            PropertyField(functionsSnippet, SdfxLanguage.ModuleDefinition.FunctionsSnippet);
            PropertyField(vertexSnippet, SdfxLanguage.ModuleDefinition.VertexSnippet);
            PropertyField(uvSnippet, SdfxLanguage.ModuleDefinition.UvSnippet);
            PropertyField(fragmentSnippet, SdfxLanguage.ModuleDefinition.FragmentSnippet);
            PropertyField(extraPassesSnippet, SdfxLanguage.ModuleDefinition.ExtraPassesSnippet);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(SdfxLanguage.ModuleDefinition.SectionModes, EditorStyles.boldLabel);
            PropertyField(modePropertyName, SdfxLanguage.ModuleDefinition.ModePropertyName, SdfxLanguage.ModuleDefinition.ModePropertyTooltip);
            PropertyField(modeLabels, SdfxLanguage.ModuleDefinition.ModeLabels);
            PropertyField(fragmentModeSnippets, SdfxLanguage.ModuleDefinition.FragmentModeSnippets);

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(SdfxLanguage.ModuleDefinition.SectionProperties, EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(properties, new GUIContent(SdfxLanguage.ModuleDefinition.SectionProperties), true);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(6f);
            if (GUILayout.Button(SdfxLanguage.ModuleDefinition.ReloadButton))
            {
                ShaderModuleRegistrar.Reload();
            }
        }

        private static void PropertyField(SerializedProperty property, string label, string tooltip = null)
        {
            EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip ?? string.Empty));
        }
    }

    [CustomPropertyDrawer(typeof(SdfxModulePropertyDefinition))]
    internal sealed class SdfxModulePropertyDefinitionDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var kind = property.FindPropertyRelative("Kind");
            EditorGUI.BeginProperty(position, label, property);
            property.isExpanded = EditorGUI.Foldout(
                new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
                property.isExpanded,
                label,
                true);
            if (!property.isExpanded)
            {
                EditorGUI.EndProperty();
                return;
            }

            var y = position.y + EditorGUIUtility.singleLineHeight + 2f;
            var line = EditorGUIUtility.singleLineHeight;
            var width = position.width;
            var x = position.x;

            Draw(ref y, x, width, line, property.FindPropertyRelative("Name"), SdfxLanguage.ModuleDefinition.PropName);
            Draw(ref y, x, width, line, property.FindPropertyRelative("DisplayName"), SdfxLanguage.ModuleDefinition.PropDisplayName);
            Draw(ref y, x, width, line, kind, SdfxLanguage.ModuleDefinition.PropKind);

            var kindValue = (ModulePropertyKind)kind.enumValueIndex;
            switch (kindValue)
            {
                case ModulePropertyKind.Range:
                    Draw(ref y, x, width, line, property.FindPropertyRelative("RangeMin"), SdfxLanguage.ModuleDefinition.PropRangeMin);
                    Draw(ref y, x, width, line, property.FindPropertyRelative("RangeMax"), SdfxLanguage.ModuleDefinition.PropRangeMax);
                    Draw(ref y, x, width, line, property.FindPropertyRelative("DefaultFloat"), SdfxLanguage.ModuleDefinition.PropDefaultFloat);
                    break;
                case ModulePropertyKind.Color:
                    Draw(ref y, x, width, line, property.FindPropertyRelative("DefaultColor"), SdfxLanguage.ModuleDefinition.PropDefaultColor);
                    break;
                case ModulePropertyKind.Vector:
                    Draw(ref y, x, width, line, property.FindPropertyRelative("DefaultVector"), SdfxLanguage.ModuleDefinition.PropDefaultVector);
                    break;
                case ModulePropertyKind.Texture2D:
                    Draw(ref y, x, width, line, property.FindPropertyRelative("DefaultTexture"), SdfxLanguage.ModuleDefinition.PropDefaultTexture);
                    break;
                case ModulePropertyKind.Enum:
                    Draw(ref y, x, width, line, property.FindPropertyRelative("EnumLabels"), SdfxLanguage.ModuleDefinition.PropEnumLabels);
                    Draw(ref y, x, width, line, property.FindPropertyRelative("EnumDescriptions"), SdfxLanguage.ModuleDefinition.PropEnumDescriptions);
                    Draw(ref y, x, width, line, property.FindPropertyRelative("DefaultFloat"), SdfxLanguage.ModuleDefinition.PropDefaultFloat);
                    break;
                default:
                    Draw(ref y, x, width, line, property.FindPropertyRelative("DefaultFloat"), SdfxLanguage.ModuleDefinition.PropDefaultFloat);
                    break;
            }

            Draw(ref y, x, width, line, property.FindPropertyRelative("Attributes"), SdfxLanguage.ModuleDefinition.PropAttributes);
            Draw(ref y, x, width, line, property.FindPropertyRelative("SignalInput"), SdfxLanguage.ModuleDefinition.PropSignalInput);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (!property.isExpanded)
            {
                return EditorGUIUtility.singleLineHeight;
            }

            var kind = (ModulePropertyKind)property.FindPropertyRelative("Kind").enumValueIndex;
            var lines = 5; // name, display, kind, attributes, signal
            switch (kind)
            {
                case ModulePropertyKind.Range:
                    lines += 3;
                    break;
                case ModulePropertyKind.Enum:
                    lines += 3;
                    break;
                default:
                    lines += 1;
                    break;
            }

            return (EditorGUIUtility.singleLineHeight + 2f) * (lines + 1);
        }

        private static void Draw(ref float y, float x, float width, float line, SerializedProperty property, string label)
        {
            var height = EditorGUI.GetPropertyHeight(property, true);
            EditorGUI.PropertyField(new Rect(x, y, width, height), property, new GUIContent(label), true);
            y += height + 2f;
        }
    }
}
