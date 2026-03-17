using System;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;

    public InventoryManager InventoryManager => inventoryManager;

    public event Action<InventoryManager> InventoryBound;

    private void Awake()
    {
        RefreshBinding();
    }

    private void Start()
    {
        inventoryManager?.Refresh();
    }

    public void RefreshBinding()
    {
        InventoryManager resolvedInventory = ResolveInventoryManager();
        if (inventoryManager == resolvedInventory)
        {
            InventoryBound?.Invoke(inventoryManager);
            return;
        }

        inventoryManager = resolvedInventory;
        InventoryBound?.Invoke(inventoryManager);
    }

    private InventoryManager ResolveInventoryManager()
    {
        if (GameController.Instance == null)
        {
            return inventoryManager;
        }

        return GameController.Instance.GetInventory();
    }
}
