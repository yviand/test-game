using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    private Dictionary<string, int> items = new Dictionary<string, int>();

    void Awake()
    {
        Instance = this;
    }

    public void AddItem(string itemId, int amount)
    {
        if (items.ContainsKey(itemId))
        {
            items[itemId] += amount;
        }
        else
        {
            items[itemId] = amount;
        }

        Debug.Log(itemId + ": " + items[itemId]);
    }
}