using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class StrangeCleanup : UdonSharpBehaviour
{
    [Header("--- Configuration ---")]
    [Tooltip("Objects to reset when cleanup is triggered")]
    public GameObject[] cleanupProps;
    [Tooltip("Sync reset to all players globally")]
    public bool useGlobalSync = false;

    [Header("--- Auto Respawn ---")]
    [Tooltip("Automatically reset objects after they've been idle (not held) for a while")]
    public bool useAutoRespawn = false;
    [Tooltip("Minutes after dropping before auto-respawn (per object)")]
    [Range(1, 60)]
    public float autoRespawnMinutes = 5f;

    [Header("--- Audio ---")]
    public AudioSource soundSource;
    public AudioClip resetSound;

    // Synced reset counter - increments each time reset is triggered
    [UdonSynced]
    private int _syncedResetCount;
    private int _localResetCount;
    private bool _hasReceivedInitialSync = false;

    // Stored original transforms (parallel arrays for Udon)
    private Vector3[] _originalPositions;
    private Quaternion[] _originalRotations;
    // Props with VRCObjectSync are moved only by their new owner; everyone else just receives the sync
    private VRCObjectSync[] _objectSyncs;
    private bool _initialized = false;

    // Auto-respawn tracking (per object)
    private float[] _lastDroppedTimes;
    private bool[] _wasHeldLastFrame;
    private VRC_Pickup[] _pickupCache;
    private const float POSITION_THRESHOLD = 0.01f; // Min distance to consider "at spawn"
    private const float CHECK_INTERVAL = 0.5f; // Seconds between auto-respawn checks

    void Start()
    {
        CaptureOriginalTransforms();

        // If we're the master (first in instance), we already have correct state
        if (Networking.IsMaster)
        {
            _localResetCount = _syncedResetCount;
            _hasReceivedInitialSync = true;
        }

        // Initialize auto-respawn tracking
        if (useAutoRespawn && _initialized)
        {
            InitializeAutoRespawn();
            SendCustomEventDelayedSeconds(nameof(_CheckAutoRespawn), CHECK_INTERVAL);
        }
    }

    private void InitializeAutoRespawn()
    {
        if (cleanupProps == null) return;
        int count = cleanupProps.Length;
        _lastDroppedTimes = new float[count];
        _wasHeldLastFrame = new bool[count];
        _pickupCache = new VRC_Pickup[count];

        float currentTime = Time.time;
        for (int i = 0; i < count; i++)
        {
            _lastDroppedTimes[i] = currentTime;
            _wasHeldLastFrame[i] = false;

            // Cache pickup component reference
            GameObject prop = cleanupProps[i];
            if (prop != null)
            {
                _pickupCache[i] = (VRC_Pickup)prop.GetComponent(typeof(VRC_Pickup));
            }
        }
    }

    private void CaptureOriginalTransforms()
    {
        if (cleanupProps == null || cleanupProps.Length == 0)
        {
            _initialized = false;
            return;
        }

        int count = cleanupProps.Length;
        _originalPositions = new Vector3[count];
        _originalRotations = new Quaternion[count];
        _objectSyncs = new VRCObjectSync[count];

        for (int i = 0; i < count; i++)
        {
            GameObject prop = cleanupProps[i];
            if (prop != null)
            {
                _originalPositions[i] = prop.transform.position;
                _originalRotations[i] = prop.transform.rotation;
                _objectSyncs[i] = (VRCObjectSync)prop.GetComponent(typeof(VRCObjectSync));
            }
        }

        _initialized = true;
    }

    public void _CheckAutoRespawn()
    {
        if (!useAutoRespawn || !_initialized || _lastDroppedTimes == null) return;

        // Synced props (VRCObjectSync): only the master respawns them and the sync carries it to everyone.
        // Unsynced props: every player respawns their own local copy.
        bool isMaster = Networking.IsMaster;

        float currentTime = Time.time;
        float timeoutSeconds = autoRespawnMinutes * 60f;

        for (int i = 0; i < cleanupProps.Length; i++)
        {
            GameObject prop = cleanupProps[i];
            if (prop == null || i >= _lastDroppedTimes.Length) continue;
            if (IsSynced(i) && !isMaster) continue;

            // Check if pickup is currently held (using cached reference)
            bool isHeld = false;
            if (_pickupCache != null && i < _pickupCache.Length && _pickupCache[i] != null)
            {
                isHeld = _pickupCache[i].IsHeld;
            }

            // Detect drop event (was held, now not held) - reset timer
            if (_wasHeldLastFrame[i] && !isHeld)
            {
                _lastDroppedTimes[i] = currentTime;
            }
            _wasHeldLastFrame[i] = isHeld;

            // Skip respawn check if currently held
            if (isHeld) continue;

            Vector3 currentPos = prop.transform.position;

            // Check if object is not at spawn and has timed out since last drop
            bool isAtSpawn = Vector3.Distance(currentPos, _originalPositions[i]) < POSITION_THRESHOLD;
            bool hasTimedOut = (currentTime - _lastDroppedTimes[i]) >= timeoutSeconds;

            if (!isAtSpawn && hasTimedOut)
            {
                // Auto-respawn this object (even if still moving)
                RespawnSingleProp(i);
                _lastDroppedTimes[i] = currentTime;
            }
        }

        // Schedule next check
        SendCustomEventDelayedSeconds(nameof(_CheckAutoRespawn), CHECK_INTERVAL);
    }

    private void RespawnSingleProp(int index)
    {
        if (index < 0 || index >= cleanupProps.Length) return;
        if (cleanupProps[index] == null || index >= _originalPositions.Length) return;

        MoveProp(index);

        // Reset drop time after respawn
        if (_lastDroppedTimes != null && index < _lastDroppedTimes.Length)
        {
            _lastDroppedTimes[index] = Time.time;
        }
    }

    private bool IsSynced(int index)
    {
        return _objectSyncs != null && index < _objectSyncs.Length && _objectSyncs[index] != null;
    }

    // Returns a prop to its start. For synced props the caller must be the one player doing the reset:
    // it takes ownership and VRCObjectSync sends the move to everyone.
    private void MoveProp(int index)
    {
        GameObject prop = cleanupProps[index];
        bool synced = IsSynced(index);

        if (synced && !Networking.IsOwner(prop))
        {
            Networking.SetOwner(Networking.LocalPlayer, prop);
        }

        var pickup = (VRC_Pickup)prop.GetComponent(typeof(VRC_Pickup));
        if (pickup != null)
        {
            pickup.Drop();
        }

        prop.transform.position = _originalPositions[index];
        prop.transform.rotation = _originalRotations[index];

        var rb = prop.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Teleport without smoothing for everyone else
        if (synced) _objectSyncs[index].FlagDiscontinuity();
    }

    public override void OnDeserialization()
    {
        if (!useGlobalSync) return;

        // First sync for late joiners - just store the value, don't reset
        if (!_hasReceivedInitialSync)
        {
            _localResetCount = _syncedResetCount;
            _hasReceivedInitialSync = true;
            return;
        }

        // Check if reset count changed (remote player triggered reset)
        if (_syncedResetCount != _localResetCount)
        {
            _localResetCount = _syncedResetCount;
            ApplyReset(false);
        }
    }

    public override void Interact()
    {
        ResetAllProps();
    }

    public void ResetAllProps()
    {
        if (useGlobalSync)
        {
            // Take ownership and sync to all players
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }

            _syncedResetCount++;
            _localResetCount = _syncedResetCount;
            RequestSerialization();
            ApplyReset(true);
        }
        else
        {
            // Local-only reset: this player moves everything, including synced props
            ApplyReset(true);
        }
    }

    // isInitiator: true for the player who pressed reset. Everyone else only moves unsynced props;
    // synced props are moved once by the initiator and arrive through VRCObjectSync, so players
    // don't all fight over ownership of the same objects.
    private void ApplyReset(bool isInitiator)
    {
        if (!_initialized || cleanupProps == null) return;

        int resetCount = 0;
        for (int i = 0; i < cleanupProps.Length; i++)
        {
            if (cleanupProps[i] == null || i >= _originalPositions.Length) continue;
            if (IsSynced(i) && !isInitiator) continue;

            MoveProp(i);
            resetCount++;
        }

        // Play sound
        if (soundSource != null && resetSound != null && (resetCount > 0 || !isInitiator))
        {
            soundSource.PlayOneShot(resetSound);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawIcon(transform.position, "d_Refresh", true);

        // Draw lines to cleanup props
        if (cleanupProps != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 1f, 0.5f);
            foreach (GameObject prop in cleanupProps)
            {
                if (prop != null)
                    Gizmos.DrawLine(transform.position, prop.transform.position);
            }
        }
    }
#endif
}
