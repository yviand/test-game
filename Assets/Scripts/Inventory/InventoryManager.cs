using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    [System.Serializable]
    public class InventoryItem
    {
        public ItemInstance instance;
        public int count;

        public InventoryItem(ItemInstance itemInstance, int amount)
        {
            instance = itemInstance;
            count = amount;
        }
    }

    [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();
    [SerializeField] private int maxSlots = 20;
    [SerializeField] private int coins;
    [SerializeField] private int gems;

    public IReadOnlyList<InventoryItem> Items => items;
    public int MaxSlots => maxSlots;
    public int Coins => coins;
    public int Gems => gems;

    public event Action<InventoryManager> InventoryChanged;

    private void Start()
    {
        NotifyInventoryChanged();
    }

    public bool IsFull()
    {
        return items.Count >= maxSlots;
    }

    public bool CanAddItem(ItemInstance itemInstance, int amount = 1)
    {
        if (itemInstance == null || itemInstance.Data == null || amount <= 0)
        {
            return false;
        }

        return CanAddItem(itemInstance.Data, amount);
    }

    public bool CanAddItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0)
        {
            return false;
        }

        if (itemData.isStackable)
        {
            foreach (var invItem in items)
            {
                if (invItem.instance != null && invItem.instance.Data == itemData)
                {
                    return true;
                }
            }

            return !IsFull();
        }

        return maxSlots - items.Count >= amount;
    }

    public bool AddItem(ItemInstance itemInstance, int amount = 1)
    {
        if (itemInstance == null || itemInstance.Data == null || amount <= 0)
        {
            return false;
        }

        if (!CanAddItem(itemInstance, amount))
        {
            Debug.Log("Inventory Full!");
            return false;
        }

        if (itemInstance.Data.isStackable)
        {
            foreach (var invItem in items)
            {
                if (invItem.instance != null && invItem.instance.Data == itemInstance.Data)
                {
                    invItem.count += amount;
                    // Debug.Log($"Added {amount} to {itemInstance.Data.itemName}. Total: {invItem.count}");
                    NotifyInventoryChanged();
                    return true;
                }
            }

            items.Add(new InventoryItem(itemInstance, amount));
            // Debug.Log($"Added new item: {itemInstance.Data.itemName}");
            NotifyInventoryChanged();
            return true;
        }

        for (int i = 0; i < amount; i++)
        {
            ItemInstance instanceToStore = i == 0
                ? itemInstance
                : itemInstance.Clone();

            items.Add(new InventoryItem(instanceToStore, 1));
        }

        // Debug.Log($"Added new item: {itemInstance.Data.itemName}");
        NotifyInventoryChanged();
        return true;
    }

    public void AddBalance(CurrencyType currencyType, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        switch (currencyType)
        {
            case CurrencyType.Coins:
                coins += amount;
                Debug.Log($"Coins: {coins}");
                break;
            case CurrencyType.Gems:
                gems += amount;
                Debug.Log($"Gems: {gems}");
                break;
            default:
                // Debug.LogWarning($"Unsupported currency type: {currencyType}");
                return;
        }

        NotifyInventoryChanged();
    }

    public ItemInstance GetItemInSlot(int index)
    {
        if (index < 0 || index >= items.Count)
        {
            return null;
        }

        InventoryItem inventoryItem = items[index];
        return inventoryItem != null ? inventoryItem.instance : null;
    }

    public void Refresh()
    {
        NotifyInventoryChanged();
    }

    private void NotifyInventoryChanged()
    {
        InventoryChanged?.Invoke(this);
    }
}
