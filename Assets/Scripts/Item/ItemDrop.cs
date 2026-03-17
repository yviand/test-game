using UnityEngine;

public class ItemDrop : BaseDrop
{
    [SerializeField] private ItemData itemData;
    [SerializeField] private int amount = 1;

    public ItemData ItemData => itemData;
    public int Amount => amount;

    private void Awake()
    {
        ResolveItemData();
        ConfigurePickupState();
    }

    public void SetDrop(ItemData newItemData, int newAmount)
    {
        itemData = newItemData;
        amount = Mathf.Max(1, newAmount);
        ResolveItemData();
        ConfigurePickupState();
    }

    protected override bool Collect(PlayerItem playerItem)
    {
        ResolveItemData();

        if (itemData == null)
        {
            Debug.LogWarning("ItemDrop is missing item data.");
            return false;
        }

        ItemInstance itemInstance = CreateItemInstance();
        if (itemInstance == null)
        {
            Debug.LogWarning($"Failed to create item instance for drop {name}.");
            return false;
        }

        bool addedToInventory = playerItem.ReceiveItem(itemInstance, amount);
        if (!addedToInventory)
        {
            return false;
        }

        return addedToInventory;
    }

    protected override bool CanMagnetize(PlayerItem playerItem)
    {
        ResolveItemData();
        return itemData != null && playerItem != null && playerItem.CanReceiveItem(itemData, amount);
    }

    private void OnValidate()
    {
        ResolveItemData();
    }

    protected override void LaunchOnSpawn()
    {
    }

    private void ResolveItemData()
    {
        Weapon weapon = GetComponent<Weapon>();

        if (itemData != null)
        {
            if (weapon != null && weapon.ItemData == null)
            {
                weapon.SetItemData(itemData);
            }

            return;
        }

        if (weapon != null && weapon.ItemData != null)
        {
            itemData = weapon.ItemData;
        }
    }

    private void ConfigurePickupState()
    {
        Weapon weapon = GetComponent<Weapon>();
        if (weapon != null)
        {
            weapon.SetWorldPickupState(true);
        }

        Rigidbody2D rigidbody2D = GetComponent<Rigidbody2D>();
        if (rigidbody2D == null)
        {
            return;
        }

        rigidbody2D.linearVelocity = Vector2.zero;
        rigidbody2D.angularVelocity = 0f;
        rigidbody2D.simulated = false;
    }

    private ItemInstance CreateItemInstance()
    {
        if (itemData == null)
        {
            return null;
        }

        if (itemData.itemType != ItemData.ItemType.Weapon)
        {
            return ItemInstance.Create(itemData);
        }

        Weapon weapon = GetComponent<Weapon>();
        if (weapon != null)
        {
            if (weapon.ItemData == null && itemData != null)
            {
                weapon.SetItemData(itemData);
            }

            return ItemInstance.Create(itemData, weapon);
        }

        Debug.LogWarning($"Weapon drop {name} is missing a Weapon component for stat rolls.");
        return ItemInstance.Create(itemData);
    }
}
