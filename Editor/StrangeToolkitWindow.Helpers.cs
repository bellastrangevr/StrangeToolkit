using UnityEngine;
using UnityEditor;
using System;
using System.IO;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        private void SimpleScan()
        {
            _tVRC_LPPV = FindScriptType("VRCLightProbeProxyVolume");
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
                    Debug.LogError($"[StrangeToolkit] Failed to move file: {result}");
                else
                    Debug.Log("[StrangeToolkit] Installation fixed successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[StrangeToolkit] Error fixing installation: {e.Message}");
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
        }

        private void DrawHorizontalLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 1));
        }
    }
}
