using UnityEngine;

public class PlayerInventoryHandler : MonoBehaviour
{
    private void Awake()
    {
        if (GetComponent<PlayerItem>() == null)
        {
            Debug.LogWarning("PlayerInventoryHandler expects PlayerItem on the same GameObject.");
        }

        if (GetComponent<PlayerInventory>() == null)
        {
            Debug.LogWarning("PlayerInventoryHandler expects PlayerInventory on the same GameObject.");
        }
    }
}
