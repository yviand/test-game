using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public bool isStackable; // Vật phẩm có thể cộng dồn không (vật liệu thì có, trang bị thì không)
    
    public enum ItemType { MobDrop, Tool, Armor, Consumable }
    public ItemType itemType;

    [TextArea]
    public string description;
}