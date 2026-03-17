using UnityEngine;

public enum CurrencyType
{
    Coins,
    Gems
}

public class PlayerItem : MonoBehaviour
{
    private PlayerInventory playerInventory;
    private InventoryManager inventory;

    public int Coins => inventory != null ? inventory.Coins : 0;
    public int Gems => inventory != null ? inventory.Gems : 0;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
        ResolveInventoryManager();
    }

    public bool CanReceiveItem(ItemInstance itemInstance, int amount = 1)
    {
        if (itemInstance == null || itemInstance.Data == null || amount <= 0)
        {
            return false;
        }

        return TryGetInventoryManager(false, out InventoryManager inventoryManager)
            && inventoryManager.CanAddItem(itemInstance, amount);
    }

    public bool CanReceiveItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0)
        {
            return false;
        }

        return TryGetInventoryManager(false, out InventoryManager inventoryManager)
            && inventoryManager.CanAddItem(itemData, amount);
    }

    public void AddBalance(int amount)
    {
        AddBalance(CurrencyType.Coins, amount);
    }

    public void AddBalance(CurrencyType currencyType, int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        if (!TryGetInventoryManager(true, out InventoryManager inventoryManager))
        {
            return;
        }

        switch (currencyType)
        {
            case CurrencyType.Coins:
                inventoryManager.AddBalance(CurrencyType.Coins, amount);
                break;
            case CurrencyType.Gems:
                inventoryManager.AddBalance(CurrencyType.Gems, amount);
                break;
            default:
                Debug.LogWarning($"Unsupported currency type: {currencyType}");
                break;
        }
    }

    public bool ReceiveItem(ItemInstance itemInstance, int amount = 1)
    {
        if (itemInstance == null || itemInstance.Data == null || amount <= 0)
        {
            return false;
        }

        if (!TryGetInventoryManager(true, out InventoryManager inventoryManager))
        {
            return false;
        }

        return inventoryManager.AddItem(itemInstance, amount);
    }

    public bool ReceiveItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null)
        {
            return false;
        }

        return ReceiveItem(ItemInstance.Create(itemData), amount);
    }

    private bool TryGetInventoryManager(bool logIfMissing, out InventoryManager inventoryManager)
    {
        ResolveInventoryManager();

        inventoryManager = inventory;
        if (inventoryManager != null)
        {
            return true;
        }

        if (logIfMissing)
        {
            Debug.LogWarning("PlayerItem requires a global InventoryManager to receive items.");
        }

        return false;
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
            inventory = playerInventory.InventoryManager;
        }

        if (inventory == null && GameController.Instance != null)
        {
            inventory = GameController.Instance.GetInventory();
        }
    }
}
