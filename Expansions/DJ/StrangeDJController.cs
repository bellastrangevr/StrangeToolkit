using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class StrangeDJController : UdonSharpBehaviour
{
    [Header("--- The Stage Brain ---")]
    [Tooltip("The Master Animator that controls all lights.")]
    public Animator unifiedAnimator;
    
    [Header("--- DMX Patch Bay ---")]
    [Tooltip("List of Light Groups (e.g., 'Spotlights', 'Lasers')")]
    public string[] lightGroups;
    
    [UdonSynced] private int _currentPatternIndex;

    public void SetPattern(int groupIndex, int patternID)
    {
        if (unifiedAnimator == null) return;
        
        // This simulates sending a DMX signal to the Unified Animator
        string triggerName = $"Group_{groupIndex}_Pattern_{patternID}";
        unifiedAnimator.SetTrigger(triggerName);
        
        Debug.Log($"[Strange DJ] DMX Signal Sent: {triggerName}");
    }
}