using UnityEngine;
using UnityEditor;

namespace StrangeToolkit
{
    public partial class StrangeToolkitWindow
    {
        // VRChat's limits for the player camera (VRCCameraSettings docs / Reference Camera)
        private const float VrcMinNearClip = 0.001f;
        private const float VrcMaxNearClip = 0.05f;
        private const float VrcMinClipGap = 0.1f;

        // Fog this dense (99%) hides geometry completely, so far clip beyond it renders nothing visible
        private const float FogHiddenLn = 4.60517f; // ln(100)

        // The camera on the scene descriptor's Reference Camera. VRChat copies its clipping to every player on load.
        private Camera GetReferenceCamera(out SerializedObject descriptorSO, out SerializedProperty referenceProp)
        {
            descriptorSO = null;
            referenceProp = null;

            var descriptorType = FindVRCSceneDescriptorType();
            if (descriptorType == null) return null;

            var descriptor = FindFirstObjectByType(descriptorType) as Component;
            if (descriptor == null) return null;

            descriptorSO = new SerializedObject(descriptor);
            referenceProp = descriptorSO.FindProperty("ReferenceCamera");
            var go = referenceProp != null ? referenceProp.objectReferenceValue as GameObject : null;
            return go != null ? go.GetComponent<Camera>() : null;
        }

        // Distance at which the given fog fully hides geometry, or -1 if fog doesn't hide anything
        private static float FogHiddenDistance(bool fogOn, FogMode mode, float density, float start, float end)
        {
            if (!fogOn) return -1f;
            switch (mode)
            {
                case FogMode.Linear: return end > start ? end : -1f;
                case FogMode.Exponential: return density > 0f ? FogHiddenLn / density : -1f;
                case FogMode.ExponentialSquared: return density > 0f ? Mathf.Sqrt(FogHiddenLn) / density : -1f;
            }
            return -1f;
        }

        private void DrawClippingSection()
        {
            EditorGUILayout.BeginVertical(_listItemStyle);
            GUILayout.Label(new GUIContent("Clipping Distance (Reference Camera)",
                "VRChat copies the Reference Camera's near/far clip to every player when the world loads"), EditorStyles.boldLabel);

            Camera cam = GetReferenceCamera(out var descriptorSO, out var referenceProp);

            if (descriptorSO == null)
            {
                EditorGUILayout.HelpBox("No VRC Scene Descriptor in this scene. Add one (VRCWorld prefab) to set clipping.", MessageType.Info);
            }
            else if (referenceProp == null)
            {
                EditorGUILayout.HelpBox("This VRChat SDK's scene descriptor has no Reference Camera field the toolkit recognizes. Set clipping on your Reference Camera directly.", MessageType.Warning);
            }
            else if (cam == null)
            {
                EditorGUILayout.HelpBox("No Reference Camera is assigned, so players get VRChat's default clipping. Assign one to control it.", MessageType.Info);
                EditorGUILayout.BeginHorizontal();
                Camera main = Camera.main;
                GUI.enabled = main != null;
                if (GUILayout.Button(new GUIContent("Use Main Camera", main != null ? main.name : "No camera tagged MainCamera in the scene"), EditorStyles.miniButton))
                    AssignReferenceCamera(descriptorSO, referenceProp, main.gameObject);
                GUI.enabled = true;
                if (GUILayout.Button("Create Reference Camera", EditorStyles.miniButton))
                    CreateReferenceCamera(descriptorSO, referenceProp);
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                float near = EditorGUILayout.FloatField(new GUIContent("Near", $"VRChat clamps this to {VrcMinNearClip}-{VrcMaxNearClip}"), cam.nearClipPlane);
                float far = EditorGUILayout.FloatField("Far", cam.farClipPlane);
                if (EditorGUI.EndChangeCheck())
                {
                    SetCameraClipping(cam, near, far);
                }

                if (cam.nearClipPlane > VrcMaxNearClip || cam.nearClipPlane < VrcMinNearClip)
                    EditorGUILayout.HelpBox($"VRChat will clamp near to {Mathf.Clamp(cam.nearClipPlane, VrcMinNearClip, VrcMaxNearClip)} in-game.", MessageType.Warning);

                float hidden = FogHiddenDistance(RenderSettings.fog, RenderSettings.fogMode, RenderSettings.fogDensity,
                    RenderSettings.fogStartDistance, RenderSettings.fogEndDistance);
                DrawFogClipCoupling(hidden, cam.farClipPlane, "the scene's fog", newFar => SetCameraClipping(cam, cam.nearClipPlane, newFar));

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Select Camera", EditorStyles.miniButton, GUILayout.Width(100)))
                {
                    Selection.activeObject = cam.gameObject;
                    EditorGUIUtility.PingObject(cam.gameObject);
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
            GUILayout.Space(3);
        }

        // Shows how far clip and fog line up. Far clip short of the fog leaves a hard edge where geometry
        // pops out while it is still visible; far clip well past it renders geometry nobody can see.
        private void DrawFogClipCoupling(float fogHidden, float far, string fogLabel, System.Action<float> setFar)
        {
            if (fogHidden <= 0f) return;

            if (far < fogHidden * 0.98f)
            {
                EditorGUILayout.HelpBox($"Far clip ({far:0.#} m) cuts geometry before {fogLabel} hides it ({fogHidden:0.#} m), so distant objects will pop out with a hard edge.", MessageType.Warning);
                if (GUILayout.Button($"Set Far Clip to {fogHidden:0.#} m", EditorStyles.miniButton)) setFar(fogHidden);
            }
            else if (far > fogHidden * 1.5f)
            {
                EditorGUILayout.HelpBox($"Everything past {fogHidden:0.#} m is fully hidden by {fogLabel}. Lowering far clip to match saves rendering with no visible change.", MessageType.Info);
                if (GUILayout.Button($"Set Far Clip to {fogHidden:0.#} m", EditorStyles.miniButton)) setFar(fogHidden);
            }
        }

        private static void SetCameraClipping(Camera cam, float near, float far)
        {
            Undo.RecordObject(cam, "Set Clipping Distance");
            near = Mathf.Clamp(near, VrcMinNearClip, VrcMaxNearClip);
            cam.nearClipPlane = near;
            cam.farClipPlane = Mathf.Max(far, near + VrcMinClipGap);
            EditorUtility.SetDirty(cam);
        }

        private static void AssignReferenceCamera(SerializedObject descriptorSO, SerializedProperty referenceProp, GameObject cameraObject)
        {
            descriptorSO.Update();
            referenceProp.objectReferenceValue = cameraObject;
            descriptorSO.ApplyModifiedProperties();
            StrangeToolkitLogger.LogSuccess($"Reference Camera set to '{cameraObject.name}'");
        }

        private static void CreateReferenceCamera(SerializedObject descriptorSO, SerializedProperty referenceProp)
        {
            var go = new GameObject("Reference Camera");
            Undo.RegisterCreatedObjectUndo(go, "Create Reference Camera");
            var cam = go.AddComponent<Camera>();
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 1000f;
            // Only its settings are read; keep it from rendering in the editor
            cam.enabled = false;
            AssignReferenceCamera(descriptorSO, referenceProp, go);
        }
    }
}
