using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class StrangeHub : UdonSharpBehaviour
{
    // --- 1. PARALLEL ARRAYS (Udon-Safe Data Structure) ---
    public string[] atmoNames;
    public bool[] atmoIsDefault;
    public Material[] atmoSkyboxes;
    
    // Visuals
    public bool[] atmoControlFog;
    public Color[] atmoFogColors;
    public float[] atmoFogDensities;
    
    // Logic (Linked Objects)
    public GameObject[] atmoRoots; 

    // --- 2. LOGIC DATA ---
    public GameObject[] cleanupProps;
    
    // --- 3. OPTIMIZED TOGGLE PERSISTENCE ---
    private string[] _toggleIDs;
    private bool[] _toggleValues;
    private int _toggleCount = 0; // Tracks how many slots are actually used
    private const int CHUNK_SIZE = 32; // How many slots to add at once
    
    public void SaveToggleState(string id, bool state)
    {
        // 1. Initialization: Create the first chunk if array is missing
        if (_toggleIDs == null)
        {
            _toggleIDs = new string[CHUNK_SIZE];
            _toggleValues = new bool[CHUNK_SIZE];
            _toggleCount = 0;
        }

        // 2. Search: Check if this ID already exists in the used slots
        for (int i = 0; i < _toggleCount; i++)
        {
            if (_toggleIDs[i] == id)
            {
                _toggleValues[i] = state;
                return; // Found and updated, exit early
            }
        }

        // 3. Expansion: If we are out of empty slots, Resize!
        if (_toggleCount >= _toggleIDs.Length)
        {
            string[] newIDs = new string[_toggleIDs.Length + CHUNK_SIZE];
            bool[] newVals = new bool[_toggleValues.Length + CHUNK_SIZE];
            
            // Copy existing data to new arrays
            for(int i = 0; i < _toggleIDs.Length; i++)
            {
                newIDs[i] = _toggleIDs[i];
                newVals[i] = _toggleValues[i];
            }
            
            _toggleIDs = newIDs;
            _toggleValues = newVals;
        }

        // 4. Insertion: Add the new data into the next open slot
        _toggleIDs[_toggleCount] = id;
        _toggleValues[_toggleCount] = state;
        _toggleCount++; // Increment the counter
    }

    public bool LoadToggleState(string id, bool defaultState)
    {
        if (_toggleIDs == null) return defaultState;

        // Optimization: Only loop through the *used* slots (_toggleCount), not the empty ones.
        for (int i = 0; i < _toggleCount; i++)
        {
            if (_toggleIDs[i] == id) return _toggleValues[i];
        }
        return defaultState;
    }

    // --- 4. RUNTIME LOGIC ---
    private int _currentIndex = 0;

    private void Start()
    {
        if (atmoNames != null)
        {
            for (int i = 0; i < atmoNames.Length; i++)
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
        
        // 1. Skybox
        if (atmoSkyboxes[index] != null) 
            RenderSettings.skybox = atmoSkyboxes[index];

        // 2. Fog
        if (atmoControlFog[index])
        {
            RenderSettings.fogColor = atmoFogColors[index];
            RenderSettings.fogDensity = atmoFogDensities[index];
            RenderSettings.fog = true;
        }

        // 3. Object Toggle
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