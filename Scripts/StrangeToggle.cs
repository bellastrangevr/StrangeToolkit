using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class StrangeToggle : UdonSharpBehaviour
{
    [Header("--- Configuration ---")]
    public StrangeHub hub; // Reference to the Brain
    [Tooltip("Unique ID for saving state. MUST be unique per toggle.")]
    public string persistenceID;
    public bool usePersistence = true;
    [Tooltip("Sync toggle state to all players (mutually exclusive with persistence)")]
    public bool useGlobalSync = false;
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

    // Synced state for global mode
    [UdonSynced]
    private bool _syncedState;

    private bool _isOn;
    private MaterialPropertyBlock _mpb;

    void Start()
    {
        _mpb = new MaterialPropertyBlock();

        if (useGlobalSync)
        {
            // Global sync mode: use synced state (default starts as defaultOn)
            _isOn = defaultOn;
            _syncedState = defaultOn;
        }
        else if (hub != null && usePersistence)
        {
            // Persistence mode: load from player data
            _isOn = hub.LoadToggleState(persistenceID, defaultOn);
        }
        else
        {
            _isOn = defaultOn;
        }

        UpdateVisuals();
    }

    public override void OnDeserialization()
    {
        if (!useGlobalSync) return;

        // Remote update received - apply the synced state
        if (_isOn != _syncedState)
        {
            _isOn = _syncedState;
            UpdateVisuals();
            PlaySound();
        }
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (!useGlobalSync) return;

        // If local player joining, apply current state
        if (player.isLocal)
        {
            _isOn = _syncedState;
            UpdateVisuals();
        }
        // If we're the owner, resend state for late joiners
        else if (Networking.IsOwner(gameObject))
        {
            RequestSerialization();
        }
    }

    public override void Interact()
    {
        Toggle();
    }

    public void Toggle()
    {
        if (useGlobalSync)
        {
            // Take ownership and sync to all players
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            _isOn = !_isOn;
            _syncedState = _isOn;
            RequestSerialization();
            UpdateVisuals();
            PlaySound();
        }
        else
        {
            // Local toggle (with optional persistence)
            _isOn = !_isOn;
            UpdateVisuals();
            PlaySound();

            if (hub != null && usePersistence)
            {
                hub.SaveToggleState(persistenceID, _isOn);
            }
        }
    }

    private void PlaySound()
    {
        if (soundSource != null)
        {
            AudioClip clip = _isOn ? onSound : offSound;
            if (clip != null) soundSource.PlayOneShot(clip);
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
        // Only applies to persistence mode
        if (useGlobalSync) return;

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
