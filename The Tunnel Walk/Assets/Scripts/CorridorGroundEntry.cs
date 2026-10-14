using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CorridorGroundEntry : MonoBehaviour
{
    private CorridorEncounter corridor;

    private void Awake()
    {
        corridor = GetComponentInParent<CorridorEncounter>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (corridor != null)
            corridor.Enter(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (corridor != null)
            corridor.Enter(other);
    }
}
