using UnityEngine;
using UdonSharp;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class StrangeAtmosphereSwitch : UdonSharpBehaviour
{
    public StrangeHub hub;
    public bool cycleOnInteract = true;

    public override void Interact()
    {
        if (hub != null && cycleOnInteract)
        {
            hub.NextAtmosphere();
        }
    }
}