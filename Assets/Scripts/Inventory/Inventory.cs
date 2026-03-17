using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    private Dictionary<string, int> items = new Dictionary<string, int>();
    private PlayerInventory playerInventory;
    private InventoryManager inventoryManager;

    private void Awake()
    {
        Instance = this;
        playerInventory = GetComponent<PlayerInventory>();
        ResolveInventoryManager();
    }

    public void AddItem(ItemInstance item, int amount)
    {
        if (item == null || item.Data == null)
        {
            return;
        }

        if (inventoryManager != null)
        {
            inventoryManager.AddItem(item, amount);
        }

        items[item.Data.itemName] = items.TryGetValue(item.Data.itemName, out int existingAmount)
            ? existingAmount + amount
            : amount;

        Debug.Log("Picked up " + item.Data.name + " x" + amount);
    }

    public ItemInstance GetItemInSlot(int index)
    {
        ResolveInventoryManager();

        if (inventoryManager == null)
        {
            return null;
        }

        return inventoryManager.GetItemInSlot(index);
    }

    private void ResolveInventoryManager()
    {
        if (playerInventory == null)
        {
            playerInventory = GetComponent<PlayerInventory>();
        }

        if (playerInventory != null)
        {
            playerInventory.RefreshBinding();
            inventoryManager = playerInventory.InventoryManager;
        }

        if (inventoryManager == null && GameController.Instance != null)
        {
            inventoryManager = GameController.Instance.GetInventory();
        }
    }
}
