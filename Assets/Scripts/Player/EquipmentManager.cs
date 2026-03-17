using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private Transform weaponVisualParent;
    [SerializeField] private ItemInstance currentWeaponInstance;
    [SerializeField] private GameObject currentWeaponVisual;

    private readonly object weaponModifierSource = new object();

    public ItemInstance CurrentWeaponInstance => currentWeaponInstance;
    public GameObject CurrentWeaponVisual => currentWeaponVisual;
    public PlayerStats PlayerStats => playerStats;

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }
    }

    private void Start()
    {
        if (currentWeaponInstance != null)
        {
            Equip(currentWeaponInstance);
        }
    }

    public bool Equip(ItemInstance weaponInstance)
    {
        if (weaponInstance == null)
        {
            Unequip();
            return true;
        }

        if (weaponInstance.Data == null)
        {
            return false;
        }

        if (weaponInstance.Data.itemType != ItemData.ItemType.Weapon)
        {
            return false;
        }

        if (playerStats == null)
        {
            return false;
        }

        ClearCurrentWeaponState();

        currentWeaponInstance = weaponInstance;
        currentWeaponVisual = InstantiateWeaponVisual(currentWeaponInstance);
        playerStats.ApplyItemInstance(currentWeaponInstance, weaponModifierSource);
        playerStats.RecalculateAllFinalStats();
        return true;
    }

    public void Unequip()
    {
        ClearCurrentWeaponState();
        if (playerStats != null)
        {
            playerStats.RecalculateAllFinalStats();
        }
    }

    private void ClearCurrentWeaponState()
    {
        if (currentWeaponVisual != null)
        {
            Destroy(currentWeaponVisual);
            currentWeaponVisual = null;
        }

        if (playerStats != null)
        {
            playerStats.RemoveModifiersFromSource(weaponModifierSource);
        }

        currentWeaponInstance = null;
    }

    private GameObject InstantiateWeaponVisual(ItemInstance weaponInstance)
    {
        if (weaponInstance.Data.prefab == null)
        {
            return null;
        }

        Transform parent = weaponVisualParent != null ? weaponVisualParent : transform;
        GameObject visualObject = Instantiate(weaponInstance.Data.prefab, parent);
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localRotation = Quaternion.identity;

        Weapon weaponComponent = visualObject.GetComponent<Weapon>();
        if (weaponComponent != null)
        {
            weaponComponent.SetWorldPickupState(false);
            weaponComponent.ApplyItemInstance(weaponInstance);
        }

        BaseDrop baseDrop = visualObject.GetComponent<BaseDrop>();
        if (baseDrop != null)
        {
            baseDrop.enabled = false;
        }

        Collider2D collider2D = visualObject.GetComponent<Collider2D>();
        if (collider2D != null)
        {
            collider2D.enabled = false;
        }

        Rigidbody2D rigidbody2D = visualObject.GetComponent<Rigidbody2D>();
        if (rigidbody2D != null)
        {
            rigidbody2D.simulated = false;
        }

        return visualObject;
    }
}
