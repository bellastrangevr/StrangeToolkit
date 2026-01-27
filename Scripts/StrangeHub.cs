using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class StrangeHub : UdonSharpBehaviour
{
    // Atmosphere data (parallel arrays for Udon)
    public string[] atmoNames;
    public bool[] atmoIsDefault;
    public Material[] atmoSkyboxes;
    
    public bool[] atmoControlFog;
    public Color[] atmoFogColors;
    public float[] atmoFogDensities;
    
    public GameObject[] atmoRoots;
    public GameObject[] cleanupProps;

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

    private void Start()
    {
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

    public void NextAtmosphere()
    {
        if (atmoNames == null || atmoNames.Length == 0) return;
        _currentIndex = (_currentIndex + 1) % atmoNames.Length;
        ApplyAtmosphere(_currentIndex);
    }

    public void ApplyAtmosphere(int index)
    {
        if (atmoNames == null || index < 0 || index >= atmoNames.Length) return;

        if (atmoSkyboxes != null && index < atmoSkyboxes.Length && atmoSkyboxes[index] != null)
            RenderSettings.skybox = atmoSkyboxes[index];

        if (atmoControlFog != null && index < atmoControlFog.Length && atmoControlFog[index])
        {
            if (atmoFogColors != null && index < atmoFogColors.Length)
                RenderSettings.fogColor = atmoFogColors[index];
            if (atmoFogDensities != null && index < atmoFogDensities.Length)
                RenderSettings.fogDensity = atmoFogDensities[index];
            RenderSettings.fog = true;
        }

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