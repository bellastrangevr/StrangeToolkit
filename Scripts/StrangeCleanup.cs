using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class StrangeCleanup : UdonSharpBehaviour
{
    [Header("--- Configuration ---")]
    public StrangeHub hub;

    [Header("--- Audio ---")]
    public AudioSource soundSource;
    public AudioClip resetSound;

    // Stored original transforms (parallel arrays for Udon)
    private Vector3[] _originalPositions;
    private Quaternion[] _originalRotations;
    private bool _initialized = false;

    void Start()
    {
        CaptureOriginalTransforms();
    }

    private void CaptureOriginalTransforms()
    {
        if (hub == null || hub.cleanupProps == null)
        {
            _initialized = false;
            return;
        }

        int count = hub.cleanupProps.Length;
        _originalPositions = new Vector3[count];
        _originalRotations = new Quaternion[count];

        for (int i = 0; i < count; i++)
        {
            GameObject prop = hub.cleanupProps[i];
            if (prop != null)
            {
                _originalPositions[i] = prop.transform.position;
                _originalRotations[i] = prop.transform.rotation;
            }
        }

        _initialized = true;
    }

    public override void Interact()
    {
        ResetAllProps();
    }

    public void ResetAllProps()
    {
        if (!_initialized || hub == null || hub.cleanupProps == null) return;

        int resetCount = 0;
        for (int i = 0; i < hub.cleanupProps.Length; i++)
        {
            GameObject prop = hub.cleanupProps[i];
            if (prop != null && i < _originalPositions.Length)
            {
                // Check if VRCPickup exists and drop it first
                var pickup = (VRC_Pickup)prop.GetComponent(typeof(VRC_Pickup));
                if (pickup != null)
                {
                    Networking.SetOwner(Networking.LocalPlayer, prop);
                    pickup.Drop();
                }

                // Reset transform
                prop.transform.position = _originalPositions[i];
                prop.transform.rotation = _originalRotations[i];

                // Reset Rigidbody velocity if present
                var rb = prop.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                resetCount++;
            }
        }

        // Play sound
        if (soundSource != null && resetSound != null && resetCount > 0)
        {
            soundSource.PlayOneShot(resetSound);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Visual Gizmo: Line connecting this to the Hub
        if (hub != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, hub.transform.position);
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawIcon(transform.position, "d_Refresh", true);

        // Draw lines to cleanup props
        if (hub != null && hub.cleanupProps != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 1f, 0.5f);
            foreach (GameObject prop in hub.cleanupProps)
            {
                if (prop != null)
                    Gizmos.DrawLine(transform.position, prop.transform.position);
            }
        }
    }
#endif
}
