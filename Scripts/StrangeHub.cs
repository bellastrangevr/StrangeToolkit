using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Rendering;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class StrangeHub : UdonSharpBehaviour
{
    [UdonSynced]
    public float videoTimestamp = 0f;

    // Atmosphere data (parallel arrays for Udon)
    public string[] atmoNames;
    public bool[] atmoIsDefault;
    public Material[] atmoSkyboxes;
    
    public bool[] atmoControlFog;
    public Color[] atmoFogColors;
    public float[] atmoFogDensities;
    // FogMode as int (1 Linear, 2 Exponential, 3 ExponentialSquared); 0 or missing keeps the scene's mode
    public int[] atmoFogModes;
    public float[] atmoFogStarts;
    public float[] atmoFogEnds;
    // Camera clipping per atmosphere, applied through VRCCameraSettings.ScreenCamera
    public bool[] atmoControlClip;
    public float[] atmoNearClips;
    public float[] atmoFarClips;
    
    public GameObject[] atmoRoots;

    // Toggle persistence
    private string[] _toggleIDs;
    private bool[] _toggleValues;
    private int _toggleCount = 0;
    private const int CHUNK_SIZE = 32;
    
    public void SaveToggleState(string id, bool state)
    {
        if (_toggleIDs == null)
        {
            _toggleIDs = new string[CHUNK_SIZE];
            _toggleValues = new bool[CHUNK_SIZE];
            _toggleCount = 0;
        }

        for (int i = 0; i < _toggleCount; i++)
        {
            if (_toggleIDs[i] == id)
            {
                _toggleValues[i] = state;
                return;
            }
        }

        // Expand arrays if needed
        if (_toggleCount >= _toggleIDs.Length)
        {
            string[] newIDs = new string[_toggleIDs.Length + CHUNK_SIZE];
            bool[] newVals = new bool[_toggleValues.Length + CHUNK_SIZE];
            for(int i = 0; i < _toggleIDs.Length; i++)
            {
                newIDs[i] = _toggleIDs[i];
                newVals[i] = _toggleValues[i];
            }
            
            _toggleIDs = newIDs;
            _toggleValues = newVals;
        }

        _toggleIDs[_toggleCount] = id;
        _toggleValues[_toggleCount] = state;
        _toggleCount++;
    }

    public bool LoadToggleState(string id, bool defaultState)
    {
        if (_toggleIDs == null) return defaultState;
        for (int i = 0; i < _toggleCount; i++)
        {
            if (_toggleIDs[i] == id) return _toggleValues[i];
        }
        return defaultState;
    }

    private int _currentIndex = 0;

    // The scene's own RenderSettings, captured before any atmosphere is applied.
    // Every atmosphere is applied on top of this baseline, so values never carry over between presets.
    private bool _baselineCaptured;
    private Material _baseSkybox;
    private bool _baseFog;
    private FogMode _baseFogMode;
    private Color _baseFogColor;
    private float _baseFogDensity;
    private float _baseFogStart;
    private float _baseFogEnd;
    private bool _hasClipBaseline;
    private float _baseNearClip;
    private float _baseFarClip;

    private void CaptureBaseline()
    {
        _baseSkybox = RenderSettings.skybox;
        _baseFog = RenderSettings.fog;
        _baseFogMode = RenderSettings.fogMode;
        _baseFogColor = RenderSettings.fogColor;
        _baseFogDensity = RenderSettings.fogDensity;
        _baseFogStart = RenderSettings.fogStartDistance;
        _baseFogEnd = RenderSettings.fogEndDistance;

        // The player camera already carries the Reference Camera's clipping by the time Start runs.
        // ScreenCamera is null outside a client (e.g. the dashboard preview in edit mode).
        VRCCameraSettings screenCamera = VRCCameraSettings.ScreenCamera;
        _hasClipBaseline = screenCamera != null;
        if (_hasClipBaseline)
        {
            _baseNearClip = screenCamera.NearClipPlane;
            _baseFarClip = screenCamera.FarClipPlane;
        }
        _baselineCaptured = true;
    }

    // Used by the dashboard preview so the baseline is re-read from the scene
    public void ResetBaseline()
    {
        _baselineCaptured = false;
    }

    private void Start()
    {
        if (!_baselineCaptured) CaptureBaseline();

        if (atmoNames != null && atmoIsDefault != null)
        {
            for (int i = 0; i < atmoNames.Length && i < atmoIsDefault.Length; i++)
            {
                if (atmoIsDefault[i])
                {
                    _currentIndex = i;
                    ApplyAtmosphere(_currentIndex);
                    break;
                }
            }
        }
    }

    private void ApplyClipping(int index)
    {
        if (!_hasClipBaseline) return;
        VRCCameraSettings screenCamera = VRCCameraSettings.ScreenCamera;
        if (screenCamera == null) return;

        float nearClip = _baseNearClip;
        float farClip = _baseFarClip;
        if (atmoControlClip != null && index < atmoControlClip.Length && atmoControlClip[index]
            && atmoNearClips != null && index < atmoNearClips.Length
            && atmoFarClips != null && index < atmoFarClips.Length
            && atmoFarClips[index] > atmoNearClips[index])
        {
            nearClip = atmoNearClips[index];
            farClip = atmoFarClips[index];
        }

        // Near first: VRChat clamps far to at least near + 0.1, and clamps near to 0.001-0.05
        // (the user's "Forced Camera Near Distance" setting can still override near)
        screenCamera.NearClipPlane = nearClip;
        screenCamera.FarClipPlane = farClip;
    }

    public void NextAtmosphere()
    {
        if (atmoNames == null || atmoNames.Length == 0) return;
        _currentIndex = (_currentIndex + 1) % atmoNames.Length;
        ApplyAtmosphere(_currentIndex);
    }

    public void ApplyAtmosphere(int index)
    {
        if (atmoNames == null || index < 0 || index >= atmoNames.Length) return;
        if (!_baselineCaptured) CaptureBaseline();

        // Start from the scene baseline so a preset only changes what it controls
        Material skybox = _baseSkybox;
        bool fog = _baseFog;
        FogMode fogMode = _baseFogMode;
        Color fogColor = _baseFogColor;
        float fogDensity = _baseFogDensity;
        float fogStart = _baseFogStart;
        float fogEnd = _baseFogEnd;

        if (atmoSkyboxes != null && index < atmoSkyboxes.Length && atmoSkyboxes[index] != null)
            skybox = atmoSkyboxes[index];

        if (atmoControlFog != null && index < atmoControlFog.Length && atmoControlFog[index])
        {
            fog = true;
            if (atmoFogColors != null && index < atmoFogColors.Length)
                fogColor = atmoFogColors[index];
            if (atmoFogDensities != null && index < atmoFogDensities.Length)
                fogDensity = atmoFogDensities[index];
            if (atmoFogModes != null && index < atmoFogModes.Length && atmoFogModes[index] > 0)
                fogMode = (FogMode)atmoFogModes[index];
            if (atmoFogStarts != null && index < atmoFogStarts.Length && atmoFogEnds != null && index < atmoFogEnds.Length
                && atmoFogEnds[index] > atmoFogStarts[index])
            {
                fogStart = atmoFogStarts[index];
                fogEnd = atmoFogEnds[index];
            }
        }

        RenderSettings.skybox = skybox;
        RenderSettings.fog = fog;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogStartDistance = fogStart;
        RenderSettings.fogEndDistance = fogEnd;

        ApplyClipping(index);

        if (atmoRoots != null)
        {
            for (int i = 0; i < atmoRoots.Length; i++)
            {
                GameObject root = atmoRoots[i];
                if (root != null)
                {
                    bool shouldActive = (i == index);
                    if (root.activeSelf != shouldActive) root.SetActive(shouldActive);
                }
            }
        }
    }
}