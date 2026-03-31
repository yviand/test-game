using UnityEngine;
using System.Collections.Generic;

public class EnemyDrop : MonoBehaviour
{
    [SerializeField] private float scatterRadius = 0.5f;
    [SerializeField] private float spawnHeightOffset = 0.1f;
    
    [System.Serializable]
    public class DropRate
    {
        public GameObject itemPrefab;
        [Range(0, 100)] public float dropChance;
        public int minAmount = 1;
        public int maxAmount = 1;
    }

    [System.Serializable]
    public class CurrencyDropConfig
    {
        public GameObject currencyPrefab;
        public CurrencyType currencyType = CurrencyType.Coins;
        public int minAmount = 1;
        public int maxAmount = 5;
    }

    [SerializeField] private CurrencyDropConfig currencyDrop;
    public List<DropRate> itemDrops = new List<DropRate>();

    public void Drop()
    {
        DropCurrency();
        DropItems();
    }

    public void DropItems()
    {
        if (itemDrops == null || itemDrops.Count == 0)
        {
            return;
        }

        foreach (var drop in itemDrops)
        {
            float roll = Random.Range(0f, 100f);

            if (roll <= drop.dropChance)
            {
                int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
                SpawnItemDrop(drop.itemPrefab, amount);
            }
        }
    }

    public void DropCurrency()
    {
        if (currencyDrop == null || currencyDrop.currencyPrefab == null)
        {
            return;
        }

        int amount = Random.Range(currencyDrop.minAmount, currencyDrop.maxAmount + 1);
        for (int i = 0; i < amount; i++)
        {
            GameObject dropObject = Instantiate(currencyDrop.currencyPrefab, GetRandomScatterPosition(), Quaternion.identity);
            CurrencyDrop worldCurrency = dropObject.GetComponent<CurrencyDrop>();
            if (worldCurrency != null)
            {
                worldCurrency.SetCurrencyType(currencyDrop.currencyType);
                worldCurrency.SetAmount(1);
                continue;
            }

            Debug.LogWarning("Currency prefab is missing a CurrencyDrop component.");
        }
    }

    private void SpawnItemDrop(GameObject itemPrefab, int amount)
    {
        if (itemPrefab == null)
        {
            return;
        }

        for (int i = 0; i < amount; i++)
        {
            GameObject itemObject = Instantiate(itemPrefab, GetRandomScatterPosition(), Quaternion.identity);
            ItemDrop itemDrop = itemObject.GetComponent<ItemDrop>();
            if (itemDrop != null)
            {
                itemDrop.SetDrop(itemDrop.ItemData, 1);
                continue;
            }

            Debug.LogWarning("Item prefab is missing an ItemDrop or WorldItem component.");
        }
    }

    private Vector3 GetRandomScatterPosition()
    {
        float horizontalOffset = Random.Range(-scatterRadius, scatterRadius);
        return transform.position + new Vector3(horizontalOffset, spawnHeightOffset, 0f);
    }
}
