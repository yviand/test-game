using UnityEngine;

public class PlayerCoin : MonoBehaviour
{
    public int currentCoin = 0;
    private InventoryManager inventory;

    void Start()
    {
        inventory = GetComponent<InventoryManager>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Collectible"))
        {
            WorldItem worldItem = other.GetComponent<WorldItem>();
            if (inventory.AddItem(worldItem.itemData, worldItem.amount))
            {
                Destroy(other.gameObject);
            }
        }
    }

    public void AddCoin(int amount)
    {
        currentCoin += amount;
        Debug.Log("Current coins: " + currentCoin);
    }
}