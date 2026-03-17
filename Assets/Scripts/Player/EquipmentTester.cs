using UnityEngine;

public class EquipmentTester : MonoBehaviour
{
    [SerializeField] private EquipmentManager equipmentManager;
    [SerializeField] private Inventory inventory;

    private void Awake()
    {
        if (equipmentManager == null)
        {
            equipmentManager = GetComponent<EquipmentManager>();
        }

        if (inventory == null)
        {
            inventory = GetComponent<Inventory>();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipSlot(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipSlot(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            EquipSlot(2);
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            UnequipCurrent();
        }
    }

    private void EquipSlot(int slotIndex)
    {
        if (equipmentManager == null || inventory == null)
        {
            return;
        }

        ItemInstance itemInstance = inventory.GetItemInSlot(slotIndex);
        if (itemInstance == null || itemInstance.Data == null)
        {
            Debug.Log($"No item found in inventory slot {slotIndex}.");
            return;
        }

        bool equipped = equipmentManager.Equip(itemInstance);
        if (!equipped)
        {
            Debug.Log($"Failed to equip {itemInstance.Data.itemName} from slot {slotIndex}.");
            return;
        }

        Debug.Log($"Equipped {itemInstance.Data.itemName} | New Final Attack: {equipmentManager.PlayerStats.FinalAttack}");
    }

    private void UnequipCurrent()
    {
        if (equipmentManager == null)
        {
            return;
        }

        equipmentManager.Unequip();

        if (equipmentManager.PlayerStats != null)
        {
            Debug.Log($"Equipped Barehand | New Final Attack: {equipmentManager.PlayerStats.FinalAttack}");
        }
    }
}
