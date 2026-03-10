using UnityEngine;
using System.Collections.Generic;

public class EnemyDrop : MonoBehaviour
{
    [System.Serializable]
    public class DropRate
    {
        public GameObject itemPrefab; // Prefab chứa script WorldItem
        [Range(0, 100)] public float dropChance; // Tỷ lệ rơi (0-100%)
    }

    public List<DropRate> dropTable;

    public void DropItems()
    {
        foreach (var drop in dropTable)
        {
            float roll = Random.Range(0f, 100f);
            if (roll <= drop.dropChance)
            {
                Instantiate(drop.itemPrefab, transform.position, Quaternion.identity);
            }
        }
    }
}