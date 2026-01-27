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
    
    // Logic (Linked Objects - use these for Post Processing Volumes!)
    public GameObject[] atmoRoots; 

    // --- 2. LOGIC DATA ---
    public GameObject[] cleanupProps;
    
    // --- 3. TOGGLE PERSISTENCE ---
    private string[] _toggleIDs;
    private bool[] _toggleValues;
    
    public void SaveToggleState(string id, bool state)
    {
        if (_toggleIDs == null)
        {
            _toggleIDs = new string[0];
            _toggleValues = new bool[0];
        }

        int index = -1;
        for (int i = 0; i < _toggleIDs.Length; i++)
        {
            if (_toggleIDs[i] == id)
            {
                index = i;
                break;
            }
        }

        if (index != -1)
        {
            _toggleValues[index] = state;
        }
        else
        {
            string[] newIDs = new string[_toggleIDs.Length + 1];
            bool[] newVals = new bool[_toggleValues.Length + 1];
            
            for(int i=0; i<_toggleIDs.Length; i++)
            {
                newIDs[i] = _toggleIDs[i];
                newVals[i] = _toggleValues[i];
            }
            
            newIDs[newIDs.Length - 1] = id;
            newVals[newVals.Length - 1] = state;
            
            _toggleIDs = newIDs;
            _toggleValues = newVals;
        }
    }

    public bool LoadToggleState(string id, bool defaultState)
    {
        if (_toggleIDs == null) return defaultState;

        for (int i = 0; i < _toggleIDs.Length; i++)
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