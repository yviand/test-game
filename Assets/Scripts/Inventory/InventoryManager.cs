using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    // Cấu trúc một ô trong túi đồ
    [System.Serializable]
    public class InventoryItem
    {
        public ItemData data;
        public int count;

        public InventoryItem(ItemData item, int amount)
        {
            data = item;
            count = amount;
        }
    }

    public List<InventoryItem> items = new List<InventoryItem>();
    [SerializeField] private int maxSlots = 20;

    public bool AddItem(ItemData item, int amount = 1)
    {
        // Kiểm tra xem vật phẩm đã có trong túi và có thể cộng dồn không
        if (item.isStackable)
        {
            foreach (var invItem in items)
            {
                if (invItem.data == item)
                {
                    invItem.count += amount;
                    Debug.Log($"Added {amount} to {item.itemName}. Total: {invItem.count}");
                    return true;
                }
            }
        }

        // Nếu là vật phẩm mới hoặc không cộng dồn, kiểm tra slot trống
        if (items.Count < maxSlots)
        {
            items.Add(new InventoryItem(item, amount));
            Debug.Log($"Added new item: {item.itemName}");
            return true;
        }

        Debug.Log("Inventory Full!");
        return false;
    }
}