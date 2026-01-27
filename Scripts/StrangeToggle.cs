using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class StrangeToggle : UdonSharpBehaviour
{
    [Header("--- Configuration ---")]
    public StrangeHub hub; // Reference to the Brain
    [Tooltip("Unique ID for saving state. MUST be unique per toggle.")]
    public string persistenceID;
    public bool usePersistence = true;
    public bool defaultOn = true; 

    [Header("--- Targets ---")]
    public GameObject[] toggleObjects;
    public Renderer[] emissionRenderers;
    [ColorUsage(false, true)] public Color emissionOnColor = Color.white;
    public Color emissionOffColor = Color.black;
    public Animator[] animators;
    public string animatorBoolParam = "IsOn";

    [Header("--- Audio ---")]
    public AudioSource soundSource;
    public AudioClip onSound;
    public AudioClip offSound;

    private bool _isOn;
    private MaterialPropertyBlock _mpb;

    void Start()
    {
        _mpb = new MaterialPropertyBlock();
        
        // If persistence is on, try to load. Otherwise, use default.
        if (hub != null && usePersistence)
        {
            _isOn = hub.LoadToggleState(persistenceID, defaultOn);
        }
        else
        {
            _isOn = defaultOn;
        }
        
        UpdateVisuals();
    }

    public override void Interact()
    {
        Toggle();
    }

    public void Toggle()
    {
        _isOn = !_isOn;
        
        // 1. Update Visuals locally
        UpdateVisuals();
        
        // 2. Play Sound
        if (soundSource != null)
        {
            AudioClip clip = _isOn ? onSound : offSound;
            if (clip != null) soundSource.PlayOneShot(clip);
        }

        // 3. Save State (Local Persistence)
        if (hub != null && usePersistence)
        {
            hub.SaveToggleState(persistenceID, _isOn);
        }
    }

    public void UpdateVisuals()
    {
        // Toggle GameObjects (Mirrors/Blockers)
        if (toggleObjects != null)
        {
            foreach (GameObject obj in toggleObjects)
            {
                if (obj != null) obj.SetActive(_isOn);
            }
        }

        // Toggle Animators
        if (animators != null)
        {
            foreach (Animator anim in animators)
            {
                if (anim != null) anim.SetBool(animatorBoolParam, _isOn);
            }
        }

        // Toggle Emission (GPU Optimized via MPB)
        if (emissionRenderers != null)
        {
            foreach (Renderer rend in emissionRenderers)
            {
                if (rend == null) continue;

                rend.GetPropertyBlock(_mpb);
                _mpb.SetColor("_EmissionColor", _isOn ? emissionOnColor : emissionOffColor);
                rend.SetPropertyBlock(_mpb);
            }
        }
    }

    // Called by Hub when OnPlayerRestored fires
    public void RefreshPersistence()
    {
        if (hub != null && usePersistence)
        {
            bool savedState = hub.LoadToggleState(persistenceID, defaultOn);
            if (savedState != _isOn)
            {
                _isOn = savedState;
                UpdateVisuals();
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Visual Gizmo: Dashed line connecting this toggle to the Hub
        if (hub != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, hub.transform.position);
        }
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawIcon(transform.position, "d_Toggle Icon", true);
        
        // Draw lines to targets
        if (toggleObjects != null)
        {
            foreach(GameObject obj in toggleObjects) {
                if(obj != null) Gizmos.DrawLine(transform.position, obj.transform.position);
            }
        }
    }
#endif
}