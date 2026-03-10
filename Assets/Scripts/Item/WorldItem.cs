using UnityEngine;

public class WorldItem : MonoBehaviour
{
    public ItemData itemData; // ScriptableObject bạn đã tạo ở bước trước
    public int amount = 1;

    // Có thể thêm hiệu ứng nảy nhẹ khi vừa rơi ra
    private void Start()
    {
        GetComponent<Rigidbody2D>()?.AddForce(Random.insideUnitCircle * 2f, ForceMode2D.Impulse);
    }
}