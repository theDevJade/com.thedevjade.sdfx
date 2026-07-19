using System;
using System.Collections.Generic;
using System.Linq;
using SDFX.VectorTextureCompiler.Core;
using SDFX.VectorTextureCompiler.Core.Localization;
using SDFX.VectorTextureCompiler.Core.Modules;
using UnityEditor;
using UnityEngine;

namespace SDFX.VectorTextureCompiler.Editor
{
    internal sealed class SdfxModulePickerWindow : EditorWindow
    {
        private Material material;
        private CompiledVectorTextureAsset compiledAsset;
        private readonly Dictionary<string, bool> selected = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private Vector2 scroll;
        private string search = string.Empty;
        private bool receiveShadows;
        private bool forwardAddPass;

        public static void Open(Material material, CompiledVectorTextureAsset compiledAsset)
        {
            if (material == null)
            {
                EditorUtility.DisplayDialog(
                    SdfxLanguage.ShaderGui.RecompileShaderTitle,
                    SdfxLanguage.ShaderGui.NoCompiledAsset,
                    SdfxLanguage.ShaderGui.OkButton);
                return;
            }

            var window = GetWindow<SdfxModulePickerWindow>(true, SdfxLanguage.ShaderGui.RecompileShaderTitle, true);
            window.minSize = new Vector2(420f, 480f);
            window.material = material;
            window.compiledAsset = compiledAsset ?? SdfxMaterialInspectorUI.FindCompiledAsset(material);
            window.BuildSelection();
            window.ShowUtility();
        }

        private void BuildSelection()
        {
            selected.Clear();
            foreach (var module in ShaderModuleRegistry.All)
            {
                var inShader = material != null && material.HasProperty(module.ToggleProperty);
                var enabled = inShader
                              && material.GetFloat(module.ToggleProperty) > 0.5f;
                selected[module.Id] = inShader || enabled;
            }

            var shaderPath = material != null ? AssetDatabase.GetAssetPath(material.shader) : null;
            receiveShadows = SdfxCompilerActions.ReadReceivesShadows(shaderPath);
            forwardAddPass = SdfxCompilerActions.ReadForwardAddPass(shaderPath);
        }

        private void OnGUI()
        {
            if (material == null)
            {
                EditorGUILayout.HelpBox(SdfxLanguage.ShaderGui.NoCompiledAsset, MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField(material.name, EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(SdfxLanguage.ShaderGui.RecompileShaderHelp, MessageType.Info);

            receiveShadows = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    SdfxLanguage.ShaderGui.ReceiveShadowsField,
                    SdfxLanguage.ShaderGui.ReceiveShadowsTooltip),
                receiveShadows);

            forwardAddPass = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    SdfxLanguage.ShaderGui.ForwardAddPassField,
                    SdfxLanguage.ShaderGui.ForwardAddPassTooltip),
                forwardAddPass);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                search = SdfxEditorSearch.DrawInlineField(search, SdfxLanguage.ShaderGui.SearchPlaceholder);
                if (GUILayout.Button(SdfxLanguage.ShaderGui.ModulesSelectEnabled, EditorStyles.toolbarButton, GUILayout.Width(88f)))
                {
                    SelectEnabledOnly();
                }

                if (GUILayout.Button(SdfxLanguage.ShaderGui.ModulesEnableAll, EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    SetAll(true);
                }

                if (GUILayout.Button(SdfxLanguage.ShaderGui.ModulesDisableAll, EditorStyles.toolbarButton, GUILayout.Width(64f)))
                {
                    SetAll(false);
                }
            }

            var selectedCount = selected.Count(kv => kv.Value);
            EditorGUILayout.LabelField(
                SdfxLanguage.ShaderGui.ModulesSelectedCount(selectedCount, selected.Count),
                EditorStyles.miniLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var group in ShaderModuleRegistry.All.GroupBy(m => m.Category).OrderBy(g => (int)g.Key))
            {
                var modules = group
                    .Where(m => !SdfxEditorSearch.HasQuery(search)
                                || SdfxEditorSearch.MatchesQuery(search, m.Id, m.DisplayName, m.Description, group.Key.ToString()))
                    .ToList();
                if (modules.Count == 0)
                {
                    continue;
                }

                EditorGUILayout.LabelField(SdfxEditorLabels.Category(group.Key), EditorStyles.boldLabel);
                foreach (var module in modules)
                {
                    selected.TryGetValue(module.Id, out var isOn);
                    var inShader = material.HasProperty(module.ToggleProperty);
                    var label = inShader
                        ? module.DisplayName
                        : module.DisplayName + " " + SdfxLanguage.ShaderGui.ModuleNotInShaderSuffix;
                    var next = EditorGUILayout.ToggleLeft(
                        new GUIContent(label, module.Description),
                        isOn);
                    selected[module.Id] = next;
                }

                EditorGUILayout.Space(4f);
            }

            EditorGUILayout.EndScrollView();

            var conflicts = ShaderModuleRegistry.ValidateSelection(
                selected.Where(kv => kv.Value).Select(kv => kv.Key).ToList());
            if (conflicts.Count > 0)
            {
                EditorGUILayout.HelpBox(
                    SdfxLanguage.ShaderGui.ModulePickerConflicts(conflicts.Count),
                    MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(SdfxLanguage.ShaderGui.CancelButton, GUILayout.Width(90f), GUILayout.Height(26f)))
                {
                    Close();
                }

                using (new EditorGUI.DisabledScope(selectedCount == 0 || compiledAsset == null))
                {
                    if (GUILayout.Button(SdfxLanguage.ShaderGui.RecompileShaderConfirm, GUILayout.Width(140f), GUILayout.Height(26f)))
                    {
                        Confirm();
                    }
                }
            }

            if (compiledAsset == null)
            {
                EditorGUILayout.HelpBox(SdfxLanguage.ShaderGui.NoCompiledAsset, MessageType.Error);
            }
        }

        private void SelectEnabledOnly()
        {
            foreach (var module in ShaderModuleRegistry.All)
            {
                var enabled = material != null
                              && material.HasProperty(module.ToggleProperty)
                              && material.GetFloat(module.ToggleProperty) > 0.5f;
                selected[module.Id] = enabled;
            }
        }

        private void SetAll(bool value)
        {
            foreach (var module in ShaderModuleRegistry.All)
            {
                selected[module.Id] = value;
            }
        }

        private void Confirm()
        {
            var ids = selected.Where(kv => kv.Value).Select(kv => kv.Key).ToList();
            var mat = material;
            var asset = compiledAsset;
            var shadows = receiveShadows;
            var forwardAdd = forwardAddPass;
            Close();

            EditorApplication.delayCall += () =>
            {
                if (SdfxCompilerActions.TryRegenerateShaderOnly(mat, asset, ids, shadows, forwardAdd, out var message))
                {
                    Debug.Log(message);
                }
                else if (!string.IsNullOrWhiteSpace(message))
                {
                    EditorUtility.DisplayDialog(SdfxLanguage.ShaderGui.RecompileShaderTitle, message, SdfxLanguage.ShaderGui.OkButton);
                }
            };
        }
    }
}
