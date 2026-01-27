using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class StrangeGameManager : UdonSharpBehaviour
{
    [Header("--- RPG Vitals ---")]
    public bool enableVitals = true;
    [Range(100, 1000)] public int maxHealth = 100;
    [Range(0, 500)] public int maxMana = 100;

    [Header("--- Loot Tables ---")]
    [Tooltip("Define rarity tiers (Common, Rare, Legendary)")]
    public string[] lootTiers;
    
    // Simulate a simple damage event
    public void ApplyDamage(VRCPlayerApi target, int amount)
    {
        Debug.Log($"[Strange Game] {target.displayName} took {amount} damage!");
        // In the full version, this would sync with the player's local health manager
    }
}