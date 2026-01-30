using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void SimpleScan()
        {
            _tRedSim_LPPV = FindScriptType("LightVolume");
            _tBakery = FindScriptType("ftRenderLightmap");
            _scanComplete = true;
        }

        private void RefreshSystem()
        {
            SimpleScan();
            _cachedHub = null;
            _realtimeLights.Clear();
            _nonStaticObjects.Clear();
            _brokenStaticObjects.Clear();
            _auditorHasRun = false;
            _auditorClean = false;
            _heaviestMeshes.Clear();
            _heaviestTextures.Clear();
            _registry = new SceneRegistry();
            _weightScanRun = false;
            _totalVRAMBytes = 0;
            _usingBuildData = false;
            _buildDataSize = "";
            _occlusionSize = 0;
            _lastSnapshot = null;
            _shadersLoaded = false;
            ScanForExpansions();
            Repaint();
        }

        private Type FindScriptType(string exactName)
        {
            string[] guids = AssetDatabase.FindAssets(exactName + " t:MonoScript");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(exactName + ".cs"))
                {
                    MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (script != null)
                        return script.GetClass();
                }
            }
            return null;
        }

        private bool CheckForInstallationError()
        {
            string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            return !path.Contains("/Editor/") && !path.Contains("\\Editor\\");
        }

        private void DrawInstallationFixer()
        {
            EditorGUILayout.HelpBox("StrangeToolkitWindow.cs must be in an Editor folder!", MessageType.Error);
            if (GUILayout.Button("FIX INSTALLATION"))
                FixInstallation();
        }

        private void FixInstallation()
        {
            try
            {
                string path = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
                string parentDir = Path.GetDirectoryName(Path.GetDirectoryName(path));
                string editorFolder = Path.Combine(parentDir, "Editor");

                if (!Directory.Exists(editorFolder))
                    AssetDatabase.CreateFolder(parentDir, "Editor");

                string newPath = Path.Combine(editorFolder, "StrangeToolkitWindow.cs");
                string result = AssetDatabase.MoveAsset(path, newPath);

                if (!string.IsNullOrEmpty(result))
                    StrangeToolkitLogger.LogError($"Failed to move file: {result}");
                else
                    StrangeToolkitLogger.LogSuccess("Installation fixed successfully.");
            }
            catch (Exception e)
            {
                StrangeToolkitLogger.LogError($"Error fixing installation: {e.Message}");
            }
        }

        private void InitStyles()
        {
            if (_headerStyle == null)
            {
                _headerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 18,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (_subHeaderStyle == null)
            {
                _subHeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 16,
                    alignment = TextAnchor.MiddleCenter,
                    margin = new RectOffset(0, 0, 10, 10)
                };
            }

            if (_warningStyle == null)
            {
                _warningStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.yellow }
                };
            }

            if (_listItemStyle == null)
            {
                _listItemStyle = new GUIStyle(EditorStyles.helpBox);
                _listItemStyle.padding = new RectOffset(5, 5, 5, 5);
            }

            if (_successStyle == null)
            {
                _successStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (_bigDropStyle == null)
            {
                _bigDropStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    fontStyle = FontStyle.Bold
                };
                _bigDropStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            }

            if (_cardStyle == null)
            {
                _cardStyle = new GUIStyle(EditorStyles.helpBox)
                {
                    padding = new RectOffset(10, 10, 10, 10),
                    margin = new RectOffset(5, 5, 5, 5)
                };
            }

            if (_questSafeStyle == null)
            {
                _questSafeStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.2f, 0.8f, 0.2f) }
                };
            }

            if (_questWarnStyle == null)
            {
                _questWarnStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.yellow }
                };
            }

            if (_questDangerStyle == null)
            {
                _questDangerStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 14,
                    normal = { textColor = new Color(1f, 0.3f, 0.3f) }
                };
            }

            if (_infoStyle == null)
            {
                _infoStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = new Color(0.6f, 0.8f, 1f) }
                };
            }

            if (_ignoredStyle == null)
            {
                _ignoredStyle = new GUIStyle(EditorStyles.label)
                {
                    fontStyle = FontStyle.Italic,
                    normal = { textColor = Color.gray }
                };
            }

            if (_whitelistButtonStyle == null)
            {
                _whitelistButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold
                };
                _whitelistButtonStyle.normal.textColor = Color.green;
            }

            if (_blacklistButtonStyle == null)
            {
                _blacklistButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontStyle = FontStyle.Bold
                };
                _blacklistButtonStyle.normal.textColor = new Color(1f, 0.5f, 0.5f);
            }

            if (_foldoutStyle == null)
            {
                _foldoutStyle = new GUIStyle(EditorStyles.foldout)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 12
                };
            }
        }

        private void DrawHorizontalLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }

        private void AutoGenerateSceneVolumes(float maxVolumeSize)
        {
            var renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None)
                .Where(r => (GameObjectUtility.GetStaticEditorFlags(r.gameObject) & StaticEditorFlags.ContributeGI) != 0)
                .ToList();

            if (renderers.Count == 0)
            {
                EditorUtility.DisplayDialog("Scene Volume Generation", "No lightmap-static MeshRenderers found in the scene. There's nothing to build the volume around.", "OK");
                return;
            }

            var bounds = new Bounds(renderers[0].bounds.center, Vector3.zero);
            foreach (var rend in renderers)
            {
                bounds.Encapsulate(rend.bounds);
            }

            if (bounds.size.x > maxVolumeSize || bounds.size.y > maxVolumeSize || bounds.size.z > maxVolumeSize)
            {
                bool proceed = EditorUtility.DisplayDialog("Large Scene Volume",
                    $"The calculated bounding volume is very large ({bounds.size.x:F1} x {bounds.size.y:F1} x {bounds.size.z:F1}m). " +
                    $"This may lead to long bake times and high memory usage.\n\n" +
                    $"It's recommended to keep volumes under {maxVolumeSize}m on any given axis. Consider splitting your scene into smaller, more manageable areas.\n\n" +
                    $"Do you want to proceed anyway?",
                    "Proceed Anyway", "Cancel");
                if (!proceed) return;
            }

            Type lppvType = _tRedSim_LPPV;
            if (lppvType == null)
            {
                EditorUtility.DisplayDialog("LPPV Component Not Found", "Could not find RedSim Light Volume component. Make sure you have RedSim Light Volumes installed.", "OK");
                return;
            }

            GameObject lppvGo = GameObject.Find("Scene_Global_LPPV");
            if (lppvGo == null)
            {
                lppvGo = new GameObject("Scene_Global_LPPV");
                Undo.RegisterCreatedObjectUndo(lppvGo, "Create Scene LPPV");
            }
            else
            {
                Undo.RecordObject(lppvGo.transform, "Update Scene LPPV Transform");
            }

            Component lppvComponent = lppvGo.GetComponent(lppvType);
            if (lppvComponent == null)
            {
                lppvComponent = Undo.AddComponent(lppvGo, lppvType);
            }

            Undo.RecordObject(lppvComponent, "Update LPPV Settings");

            // Configure RedSim Light Volume
            PropertyInfo centerProp = lppvType.GetProperty("center");
            if (centerProp != null && centerProp.CanWrite) centerProp.SetValue(lppvComponent, bounds.center);

            PropertyInfo extentsProp = lppvType.GetProperty("extents");
            if (extentsProp != null && extentsProp.CanWrite) extentsProp.SetValue(lppvComponent, bounds.size);

            if (FindFirstObjectByType<LightProbeGroup>() == null)
            {
                Undo.AddComponent<LightProbeGroup>(lppvGo);
                StrangeToolkitLogger.Log("No LightProbeGroup found in scene. Added one to LPPV object.");
            }

            Selection.activeGameObject = lppvGo;
            StrangeToolkitLogger.LogSuccess($"Configured 'Scene_Global_LPPV' with {lppvType.Name} to encompass all {renderers.Count} static renderers.");
        }
    }
}
